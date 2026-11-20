using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.Stage;
using Lair.Village;
using UnityEngine;

namespace Lair.Showcase
{
    //# 슬롯 하나 — 종족 고정 자리(기준 몬스터), 몬스터를 자식으로 받는 홀더·그림자·(로딩) 정적 자리표시자. 씬에 사전 배치(빌더).
    [Serializable]
    public class ShowcaseSlot
    {
        public EMonster Species;
        public Transform Holder;
        public SpriteRenderer Shadow;
        public GameObject Placeholder;
        public int SortingLayerId;
        public int DefaultOrder;
    }

    //# 마을·로딩 몬스터 쇼케이스 루트(Rule 02 §10, village-monster-showcase) — 유효 편성 6마리를 전투와 같은 외형(티어 시트 + 등급 연출)으로 보여 준다.
    //# 순수 비주얼: 전투 프리팹을 CHMPool 로 Pop → IMonsterShowcase 모드. 마을은 경유점 배회, 로딩은 마법진 위 고정(자리표시자 → 교체).
    public class MonsterShowcaseStage : MonoBehaviour
    {
        [SerializeField] private EScene _scene = EScene.Village;
        [SerializeField] private ShowcaseSlot[] _slots;

        private const float HpBarPad = 0.08f;
        private const float StartHoverDots = 10f;
        private const float IdlePhaseStepSec = 0.37f;

        private sealed class SlotState
        {
            public ShowcaseSlot Slot;
            public ShowcaseActorData Actor;
            public CHPoolable Instance;
            public IMonsterShowcase Showcase;
            public int Index;
            public int AppliedLevel = -1;
            public EMonster Key;
            public Vector3 PrefabScale = Vector3.one;
            public Vector3 ShadowBaseScale = Vector3.one;
            public Vector2 PosDots;
            public int WaypointIndex;
            public float Wait;
            public bool Moving;
            public bool FlipLeft;
            public float HoverPhase;
        }

        private readonly List<SlotState> _states = new List<SlotState>();
        private readonly HashSet<EMonster> _warmed = new HashSet<EMonster>();
        private MonsterTierSheetLoader _loader;
        private Transform _cam;
        private bool _busy;
        private bool _dirty;
        private bool _released;
        private bool _begun;
        //# 강화 Lv 계산용 메타 수치 — PrepareAsync 가 로더 결과를 1회 받아 둔다(직렬화하지 않는다). null 이면 강화 Lv 0 으로 본다.
        private MetaConfig _metaConfig;

        //# 로딩 컨트롤러가 5단계(진행 바)와 병렬로 시작하고 5.5 에서 기다린다. 실패해도 예외를 밖으로 내지 않는다(자리표시자 유지).
        public async Task PrepareAsync()
        {
            if (_begun)
                return;
            _begun = true;
            _metaConfig = MetaConfig.Current;
            await RefreshAsync();
        }

        //# 로딩 완료 통지 — 로딩 쇼케이스는 처음부터 군주 5등급 Lv5 고정이라 외형은 불변, 재갱신만 요청한다.
        public void RevealActual()
        {
            _dirty = true;
        }

        //# 프로필 변경(구매·승급·클라우드 복원) 통지 — 프레임당 1회로 합쳐 처리(LateUpdate).
        public void RequestRefresh()
        {
            _dirty = true;
        }

        //# 씬 정리 지점(출격·씬 전환 직전, OnDestroy) — 모든 인스턴스를 복구 후 풀에 반환하고 티어 시트를 해제. 멱등.
        public void ReleaseAll()
        {
            _released = true;
            foreach (SlotState state in _states)
            {
                ReturnInstance(state);
            }
            _loader?.Release();
            _loader = null;
        }

        //# 로딩은 군주 인스턴스가 적용되기 전에 자리표시자·그림자가 어색하게 보이지 않도록 시작부터 숨긴다(마을은 기존 동작).
        private void Awake()
        {
            if (_scene != EScene.Loading || _slots == null)
                return;
            foreach (ShowcaseSlot slot in _slots)
            {
                SetPlaceholderVisible(slot, false);
            }
        }

        private static void SetPlaceholderVisible(ShowcaseSlot slot, bool visible)
        {
            if (slot.Placeholder != null)
            {
                slot.Placeholder.SetActive(visible);
            }
            if (slot.Shadow != null)
            {
                slot.Shadow.enabled = visible;
            }
        }

        private void Start()
        {
            Camera mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }

        private void LateUpdate()
        {
            if (_dirty && _busy == false && _begun && _released == false)
            {
                _dirty = false;
                RunRefresh();
            }
        }

        private async void RunRefresh()
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (_busy)
            {
                _dirty = true;
                return;
            }
            ShowcaseDatabase db = ShowcaseDatabase.Current;
            if (db == null || _slots == null)
            {
                ShowLoadingFallbackForEmptySlots();
                return;
            }
            _busy = true;
            try
            {
                if (_states.Count == 0)
                {
                    //# 진입 시 이전 씬 잔여 이펙트 슬롯 제거(monster-grades §9.3 공유 카운터).
                    GradeFxBudget.Reset();
                    BuildStates(db);
                }
                //# 로딩 화면은 저장 데이터·공개 여부와 무관하게 처음부터 군주 5등급 + 최대 강화 티어로 고정.
                MonsterLook[] looks;
                if (_scene == EScene.Loading)
                {
                    looks = MonsterLooks.LoadingShowcase(_metaConfig);
                }
                else
                {
                    MetaProfile profile = MetaSession.GetOrLoad();
                    Dictionary<EMonster, int> levels = MonsterLooks.SpeciesLevels(profile, _metaConfig);
                    looks = MonsterLooks.Compute(MonsterLooks.ResolveFormation(profile), levels);
                }
                if (_loader == null)
                {
                    _loader = new MonsterTierSheetLoader(
                        key => CHMResource.Instance.LoadAsync<MonsterTierSheetSet>(key),
                        key => CHMResource.Instance.Unload(key),
                        Debug.LogWarning);
                }
                MonsterTierSheetLoader loader = _loader;
                await loader.LoadAsync(MonsterLooks.TierNeeds(looks));
                if (_released)
                {
                    //# 로드 중 씬이 정리됐다 — ReleaseAll 은 이 로더를 이미 놓았으므로 늦게 도착한 시트를 여기서 해제.
                    loader.Release();
                    return;
                }
                foreach (SlotState state in _states)
                {
                    if (_released)
                        return;
                    MonsterLook look = LookOf(looks, state.Slot.Species);
                    await ApplyLookAsync(state, look, db);
                }
                //# 모든 슬롯 적용이 끝난 시점 — 건너뛴 슬롯(SlotState 없음)·실패 슬롯이 숨은 채 남지 않게 복구.
                ShowLoadingFallbackForEmptySlots();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowLoadingFallbackForEmptySlots();
            }
            finally
            {
                _busy = false;
            }
        }

        private void BuildStates(ShowcaseDatabase db)
        {
            _states.Clear();
            foreach (ShowcaseSlot slot in _slots)
            {
                ShowcaseActorData actor = db.ActorOf(_scene, slot.Species);
                if (actor == null || slot.Holder == null)
                {
                    Debug.LogWarning($"[MonsterShowcase] {_scene}/{slot.Species} 슬롯·데이터 누락 — 건너뜀");
                    continue;
                }
                SlotState state = new SlotState
                {
                    Index = _states.Count,
                    Slot = slot,
                    Actor = actor,
                    PosDots = actor.Waypoints[0],
                    FlipLeft = actor.StartFacingLeft,
                    HoverPhase = UnityEngine.Random.value * 6.2832f,
                };
                SceneRowWait(db, state);
                if (slot.Shadow != null)
                {
                    state.ShadowBaseScale = slot.Shadow.transform.localScale;
                }
                _states.Add(state);
            }
        }

        private void SceneRowWait(ShowcaseDatabase db, SlotState state)
        {
            Showcase_SceneRow row = db.SceneRowOf(_scene);
            state.Wait = row != null ? UnityEngine.Random.value * row.startWaitMaxSec : 0f;
        }

        private static MonsterLook LookOf(MonsterLook[] looks, EMonster species)
        {
            foreach (MonsterLook look in looks)
            {
                if (look.Base == species)
                    return look;
            }
            return new MonsterLook { Monster = species, Base = species, Grade = 1 };
        }

        private async Task ApplyLookAsync(SlotState state, MonsterLook look, ShowcaseDatabase db)
        {
            //# 같은 (몬스터, 강화 Lv) 로 이미 적용돼 있으면 건너뜀 — OnChanged 마다 6슬롯 전부 재적용 방지.
            if (state.Instance != null && state.Key == look.Monster && state.AppliedLevel == look.Level)
                return;
            EMonster key = look.Monster;
            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            if (prefab == null && key != look.Base)
            {
                Debug.LogWarning($"[MonsterShowcase] {key} 프리팹 로드 실패 — 기준 몬스터로 폴백");
                key = look.Base;
                prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            }
            if (prefab == null)
            {
                Debug.LogWarning($"[MonsterShowcase] {key} 프리팹 로드 실패 — 자리표시자 유지");
                ShowLoadingFallback(state);
                return;
            }
            if (_released)
                return;

            bool sameInstance = state.Instance != null && state.Key == key;
            if (sameInstance == false)
            {
                //# 로딩은 자리표시자 대신 프리팹 인스턴스를 항상 띄운다(마법진 선·그림자가 자리표시자에선 어긋남). 그 외 씬은 같은 외형이면 교체 생략.
                bool samePlaceholder = _scene != EScene.Loading && state.Instance == null && state.Slot.Placeholder != null && look.Tier == 0 && MonsterGrades.GradeOf(key) == MonsterGrades.MinGrade;
                if (samePlaceholder)
                    return;
                ReturnInstance(state);
                if (SpawnInstance(state, prefab, key, db) == false)
                {
                    ShowLoadingFallback(state);
                    return;
                }
            }
            MonsterLookApplier.Apply(state.Instance.gameObject, key, look.Base, look.Level, sameInstance == false, _loader);
            state.AppliedLevel = look.Level;
            FitToHeight(state, key, db);
        }

        //# 로딩에서 군주 인스턴스를 못 띄웠을 때만 자리표시자+그림자를 되살려 빈 화면을 막는다.
        private void ShowLoadingFallback(SlotState state)
        {
            if (_scene == EScene.Loading && state.Instance == null)
            {
                SetPlaceholderVisible(state.Slot, true);
            }
        }

        private bool SpawnInstance(SlotState state, GameObject prefab, EMonster key, ShowcaseDatabase db)
        {
            if (_warmed.Add(key))
            {
                CHMPool.Instance.CreatePool(prefab, 1);
            }
            CHPoolable poolable = CHMPool.Instance.Pop(prefab, state.Slot.Holder);
            if (poolable == null)
                return false;
            poolable.transform.localPosition = Vector3.zero;
            state.PrefabScale = prefab.transform.localScale;
            state.Instance = poolable;
            state.Key = key;
            LairCharacter lair = poolable.GetComponent<LairCharacter>();
            state.Showcase = lair != null ? lair.Get<IMonsterShowcase>() : null;
            if (state.Showcase == null)
            {
                Debug.LogWarning($"[MonsterShowcase] {key} 프리팹에 MonsterShowcase 없음 — 전투 컴포넌트가 켜진 채로 표시됨");
            }
            else
            {
                state.Showcase.SetShowcaseMode(true);
                int order = _scene == EScene.Loading && state.Actor.OrderBase > 0 ? state.Actor.OrderBase : state.Slot.DefaultOrder;
                state.Showcase.SetSorting(state.Slot.SortingLayerId, order);
                if (_scene == EScene.Loading)
                {
                    state.Showcase.SetIdlePhase(state.Index * IdlePhaseStepSec);
                }
            }
            if (state.Slot.Placeholder != null)
            {
                state.Slot.Placeholder.SetActive(false);
            }
            if (_scene == EScene.Loading && state.Slot.Shadow != null)
            {
                state.Slot.Shadow.enabled = true;
            }
            ApplyFacing(state);
            return true;
        }

        //# 로딩에서 인스턴스가 없는 슬롯(데이터 누락·건너뜀·예외 포함)만 자리표시자+그림자를 되살린다 — 이미 뜬 슬롯은 건드리지 않는다.
        private void ShowLoadingFallbackForEmptySlots()
        {
            if (_scene != EScene.Loading || _slots == null)
                return;
            foreach (ShowcaseSlot slot in _slots)
            {
                SlotState state = _states.Find(x => x.Slot == slot);
                if (state == null || state.Instance == null)
                {
                    SetPlaceholderVisible(slot, true);
                }
            }
        }

        //# 표시 높이 상한 — H = (HP바 높이 − 0.08) x 48(도트), 상한 초과 시 루트 스케일·그림자에 fit 배율(village-monster-showcase §4.2).
        private void FitToHeight(SlotState state, EMonster key, ShowcaseDatabase db)
        {
            float fitMax = db.SceneRowOf(_scene).fitMaxHeightDots;
            float heightWorld = state.Showcase != null ? state.Showcase.DisplayHeightWorld : 0f;
            float dots = (heightWorld - HpBarPad) * DotStageMapping.Ppu;
            float fit = dots > fitMax && dots > 0f ? fitMax / dots : 1f;
            state.Instance.transform.localScale = state.PrefabScale * fit;
            if (state.Slot.Shadow != null)
            {
                float sizeMul = GradeFxDatabase.Current != null ? GradeFxDatabase.Current.SizeMulOf(MonsterGrades.GradeOf(key)) : 1f;
                state.Slot.Shadow.transform.localScale = state.ShadowBaseScale * fit * sizeMul;
            }
        }

        //# 복구(SetShowcaseMode(false)) 후 풀 반환. 그림자 크기·자리표시자는 호출 맥락에 맞게 되돌린다.
        private void ReturnInstance(SlotState state)
        {
            if (state.Instance == null)
                return;
            if (state.Showcase != null)
            {
                state.Showcase.SetShowcaseMode(false);
            }
            CHMPool.Instance.Push(state.Instance);
            state.Instance = null;
            state.Showcase = null;
            state.AppliedLevel = -1;
            if (state.Slot.Shadow != null)
            {
                state.Slot.Shadow.transform.localScale = state.ShadowBaseScale;
            }
            if (state.Slot.Placeholder != null && _released && _scene != EScene.Loading)
            {
                state.Slot.Placeholder.SetActive(true);
            }
        }

        private void Update()
        {
            if (_scene != EScene.Village)
                return;
            ShowcaseDatabase db = ShowcaseDatabase.Current;
            Showcase_SceneRow row = db != null ? db.SceneRowOf(_scene) : null;
            if (row == null)
                return;
            foreach (SlotState state in _states)
            {
                if (state.Instance == null)
                    continue;
                if (state.Actor.IsWander)
                {
                    Tick(state, row, Time.deltaTime);
                }
                ApplyPose(state);
            }
        }

        //# VillageWanderer 와 같은 규칙(시안 stepWalker) — 정적 순수 함수 재사용.
        private static void Tick(SlotState state, Showcase_SceneRow row, float dt)
        {
            if (state.Wait > 0f)
            {
                state.Wait -= dt;
                state.Moving = false;
                return;
            }
            Vector2[] points = state.Actor.Waypoints;
            Vector2 target = points[state.WaypointIndex];
            (Vector2 next, bool arrived) = VillageWanderer.StepToward(state.PosDots, target, state.Actor.SpeedDots, dt);
            if (arrived)
            {
                int offset = points.Length > 1 ? UnityEngine.Random.Range(0, points.Length - 1) : 0;
                state.WaypointIndex = VillageWanderer.NextWaypointIndex(state.WaypointIndex, points.Length, offset);
                state.Wait = row.waitMinSec + UnityEngine.Random.value * (row.waitMaxSec - row.waitMinSec);
                state.Moving = false;
                return;
            }
            float dx = target.x - state.PosDots.x;
            if (Mathf.Abs(dx) > 0.5f)
            {
                state.FlipLeft = dx < 0f;
            }
            state.PosDots = next;
            state.Moving = true;
        }

        //# 위치는 매 프레임 도트 반올림 후 지면(XZ)에 — 영웅·기존 배회자와 같은 Z 깊이 정렬.
        private void ApplyPose(SlotState state)
        {
            Vector2 snapped = new Vector2(Mathf.Round(state.PosDots.x), Mathf.Round(state.PosDots.y));
            Vector3 ground = DotStageMapping.VillageDotToGround(snapped);
            state.Slot.Holder.position = ground;
            ApplyFacing(state);
            if (state.Showcase != null)
            {
                state.Showcase.SetMoving(state.Moving);
                Vector3 up = _cam != null ? _cam.up : Vector3.up;
                float hover = 0f;
                if (state.Actor.Flying)
                {
                    hover = StartHoverDots + Mathf.Round(Mathf.Sin(3f * Time.time + state.HoverPhase) * 2f);
                }
                state.Showcase.SetVisualOffset(up * (hover / DotStageMapping.Ppu));
            }
            if (state.Slot.Shadow != null && _cam != null)
            {
                state.Slot.Shadow.transform.rotation = _cam.rotation;
            }
        }

        //# 루트 yaw 로 좌우 결정 — MonsterVisual2D 의 forward.x 데드존 규칙을 그대로 쓴다(오른쪽 +x = yaw 90).
        private static void ApplyFacing(SlotState state)
        {
            if (state.Instance != null)
            {
                state.Instance.transform.rotation = Quaternion.Euler(0f, state.FlipLeft ? 270f : 90f, 0f);
            }
        }
    }
}
