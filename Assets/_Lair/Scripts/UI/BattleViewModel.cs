using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Card;
using Lair.Data;

namespace Lair.UI
{
    //# 다음 액티브 카드까지의 카운트다운 — scene-2d-conversion §4.4. HasNext=false 면 표시 "—"·진행 0.
    public struct ActiveCountdown
    {
        public float RemainingSeconds;
        public float Progress;
        public bool HasNext;
    }

    //# Model 가공 + 이벤트 노출. View 를 모름.
    //# BattleResult 는 Lair.Data 의 공용 enum (Rule 09).
    public class BattleViewModel
    {
        //# 빌드 패널 1개 항목 — 카드 + 패시브 여부 + 중복 픽 횟수.
        public class BuildEntry
        {
            public CardData Card;
            public bool IsPassive;
            public int Count;
        }

        //# 스포너 셀 1개에 적용된 강화 카드 1픽 — 툴팁의 강화 줄 + 셀 상단 아이콘 row 의 source.
        //# Rule 10 의 동일 도메인 단일 파일 정신 + 기존 BuildEntry 와 같은 파일에 정의 (기획서 §4.3).
        public class AppliedBuff
        {
            public CardData Source;                  //# 어느 카드인지 (Wisp~Phantom 강화 6장 중 1)
            public int PickCount;                    //# 중첩 픽 횟수 (×N 배지 출처)
            public EMonsterStatKind Stat;            //# 어느 스탯
            public float AggregateMultiplier;        //# 곱연산 누적 결과 (툴팁 ×배율 표시 출처)
        }

        //# 스포너 1개의 표시용 스냅샷 — 이벤트 발행 시점에 재계산해 View 에 푸시.
        //# Progress 는 스냅샷에 안 들어감 (View 측 매 프레임 ISpawnerProgress.Progress 폴링).
        public class SpawnerSnapshot
        {
            public int Index;                                  //# 0~5 ring 인덱스
            public EMonster CurrentType;
            public int OutputCount;
            public IReadOnlyList<AppliedBuff> AppliedBuffs;    //# 현 출력 종에 적용된 강화 카드 픽 누적
        }

        private readonly BattleStateModel _model;
        private readonly List<BuildEntry> _build = new();
        private readonly List<SpawnerSnapshot> _spawnerSnapshots = new();

        //# 카드 리뉴얼 v0.6 — 4축 빌드 카운트. AddPick 시 card.Axis 로 증가, BuildSynergyPanel 이 구독·표시.
        //# OnBuildChanged 이벤트로 갱신 통지 (별도 이벤트 X — 카운트는 픽 시점에 함께 변동).
        private readonly Dictionary<EBuildAxis, int> _buildAxisCounts = new()
        {
            { EBuildAxis.Tank,   0 },
            { EBuildAxis.Dps,    0 },
            { EBuildAxis.Debuff, 0 },
            { EBuildAxis.Swarm,  0 },
        };

        //# AttachSpawners 가 보관 — Detach 시 동일 인스턴스로 unsubscribe.
        private IReadOnlyList<Spawner> _attachedSpawners;
        private BattleController _attachedController;
        //# Spawner 별로 등록한 핸들러 캐시 — Detach 시 정확히 해제 (대응 인덱스 클로저).
        private Action<EMonster>[] _outputTypeHandlers;
        private Action<int>[] _outputCountHandlers;
        private Action<EMonster> _typeModifierHandler;

        public event Action<float, float> OnTimerChanged;
        //# UpdateTimer 안에서 OnTimerChanged 직후 발행 — 다음 액티브 카드까지 남은 시간.
        public event Action<ActiveCountdown> OnActiveCountdownChanged;
        //# 패시브 트리거 i(0=90%) 발화 — 보스 바 눈금 획득 표시. 늦은 구독자는 IsPassiveTickAcquired 로 동기화.
        public event Action<int> OnPassiveTickAcquired;
        public event Action<float> OnHeroHpRatioChanged;
        //# 영웅 HP 정수값 (current, max) — HUD 의 "현재/최대" 텍스트 표기용. ratio 와 같은 지점에서 발행.
        public event Action<int, int> OnHeroHpValuesChanged;
        public event Action<BattleResult> OnBattleEnded;
        public event Action OnBuildChanged;

        //# 스포너 스냅샷 단독 갱신 — 6개 중 변경된 1개 인덱스만 알린다.
        public event Action<int> OnSpawnerSnapshotChanged;

        //# 상태 아이콘 — key(aura 타입), 대표 ECardId. View(BattleHud)가 ECardId→Sprite 해석.
        public event Action<object, ECardId> OnStatusIconAdded;
        public event Action<object> OnStatusIconRemoved;

        public BattleViewModel(BattleStateModel model)
        {
            _model = model;
        }

        //# 타이머 경고 임계 — 남은 시간이 이 값 이하이면 붉은 글씨(30초 = 액티브 카드 1주기 근사, 기획서 §6.2-6).
        public const float TimerWarnRemainSeconds = 30f;

        //# 타이머 경고 여부 — total 이 0 이하(미설정)면 경고하지 않는다.
        public static bool IsTimerWarning(float elapsed, float total)
        {
            if (total <= 0f)
                return false;
            return total - elapsed <= TimerWarnRemainSeconds;
        }

        public void UpdateTimer(float elapsed)
        {
            _model.ElapsedSeconds = elapsed;
            OnTimerChanged?.Invoke(elapsed, _model.TotalSeconds);
            OnActiveCountdownChanged?.Invoke(ComputeActiveCountdown(_activeThresholds, elapsed));
        }

        //# === 트리거 페이싱 HUD (scene-2d-conversion §4.9) ===
        private float[] _activeThresholds = System.Array.Empty<float>();
        private float[] _passiveThresholds = System.Array.Empty<float>();
        private bool[] _passiveAcquired = System.Array.Empty<bool>();

        public IReadOnlyList<float> ActiveThresholds => _activeThresholds;
        public IReadOnlyList<float> PassiveThresholds => _passiveThresholds;
        public int ActiveTriggerTotal => _activeThresholds.Length;
        public int PassiveTriggerTotal => _passiveThresholds.Length;

        //# BattleController 가 두 트리거 서비스를 만들 때 같은 배열을 1회 주입.
        public void BindTriggerThresholds(float[] active, float[] passive)
        {
            _activeThresholds = active ?? System.Array.Empty<float>();
            _passiveThresholds = passive ?? System.Array.Empty<float>();
            _passiveAcquired = new bool[_passiveThresholds.Length];
        }

        //# 획득 판정은 HP 비율이 아니라 트리거 발화 기록 — 리젠으로 HP 가 올라가도 유지.
        public void MarkPassiveTriggered(int index)
        {
            if (index < 0 || index >= _passiveAcquired.Length)
                return;
            _passiveAcquired[index] = true;
            OnPassiveTickAcquired?.Invoke(index);
        }

        public bool IsPassiveTickAcquired(int index)
        {
            return index >= 0 && index < _passiveAcquired.Length && _passiveAcquired[index];
        }

        //# next = elapsed 보다 큰 첫 임계, prev = elapsed 이하의 마지막 임계(없으면 0).
        public static ActiveCountdown ComputeActiveCountdown(IReadOnlyList<float> thresholds, float elapsed)
        {
            ActiveCountdown c = default;
            if (thresholds == null)
                return c;
            float prev = 0f;
            for (int i = 0; i < thresholds.Count; i++)
            {
                float t = thresholds[i];
                if (t > elapsed)
                {
                    c.HasNext = true;
                    c.RemainingSeconds = t - elapsed;
                    float span = t - prev;
                    c.Progress = span > 0f ? Math.Clamp((elapsed - prev) / span, 0f, 1f) : 0f;
                    return c;
                }
                prev = t;
            }
            return c;
        }

        //# m:ss(올림, 타이머와 같은 규칙) / 다음 없음이면 "—"
        public static string FormatCountdown(ActiveCountdown c)
        {
            if (c.HasNext == false)
                return "—";
            int sec = (int)Math.Ceiling(c.RemainingSeconds);
            return $"{sec / 60}:{sec % 60:00}";
        }

        private string _heroTitle = string.Empty;
        public string HeroTitle => _heroTitle;
        public void SetHeroTitle(string title) => _heroTitle = title ?? string.Empty;

        //# "기사 · 3단계"
        public static string ComposeHeroTitle(string heroName, int stage) => $"{heroName} · {stage}단계";

        //# 해당 종류 BuildEntry.Count 합 — 빌드 패널 라벨·접힌 탭.
        public int PassivePickCount => SumPicks(true);
        public int ActivePickCount => SumPicks(false);

        private int SumPicks(bool passive)
        {
            int sum = 0;
            foreach (BuildEntry e in _build)
            {
                if (e != null && e.IsPassive == passive)
                {
                    sum += e.Count;
                }
            }
            return sum;
        }

        public void UpdateHeroHp(int current, int max)
        {
            _model.HeroHp = current;
            _model.HeroMaxHp = max;
            OnHeroHpRatioChanged?.Invoke(max > 0 ? (float)current / max : 0f);
            //# ratio 와 같은 지점에서 정수값도 발행 — 늦은 구독자/표기 desync 방지.
            OnHeroHpValuesChanged?.Invoke(current, max);
        }

        public void EndBattle(BattleResult result)
        {
            _model.Result = result;
            OnBattleEnded?.Invoke(result);
        }

        //# 상태 아이콘 — HeroAuraRunner 이벤트를 BattleController 가 forward.
        public void AddStatusIcon(object key, ECardId iconId)
            => OnStatusIconAdded?.Invoke(key, iconId);

        public void RemoveStatusIcon(object key)
            => OnStatusIconRemoved?.Invoke(key);

        //# 카드 픽 누적 — 같은 카드면 Count++, 아니면 신규 엔트리. 이후 OnBuildChanged.
        //# 카드 리뉴얼 v0.6 — _buildAxisCounts 도 함께 증가 (BuildSynergyPanel 표시용).
        public void AddPick(CardData card, bool isPassive)
        {
            if (card == null) return;
            _buildAxisCounts[card.Axis] = _buildAxisCounts[card.Axis] + 1;
            foreach (BuildEntry e in _build)
            {
                if (e.Card == card)
                {
                    e.Count++;
                    OnBuildChanged?.Invoke();
                    return;
                }
            }
            _build.Add(new BuildEntry { Card = card, IsPassive = isPassive, Count = 1 });
            OnBuildChanged?.Invoke();
        }

        //# 카드 리뉴얼 v0.6 — 4축 빌드 카운트 조회. BuildSynergyPanel 이 OnBuildChanged 구독 후 호출.
        public int GetBuildCount(EBuildAxis axis)
        {
            return _buildAxisCounts.TryGetValue(axis, out int v) ? v : 0;
        }

        //# 시너지 모달 설명 조립용 — BattleController 가 부팅 시 바인딩한 서비스를 주입.
        private BuildSynergyService _synergy;

        public void BindSynergyService(BuildSynergyService synergy)
        {
            _synergy = synergy;
        }

        //# (축, 임계) → 바인딩된 Tier 인스턴스. 서비스 미주입/미바인딩이면 null (SynergyModalPopup 가드).
        public IBuildSynergyTier GetTier(EBuildAxis axis, int threshold)
        {
            return _synergy?.GetTier(axis, threshold);
        }

        //# 늦은 구독자용 현재값
        public float ElapsedSeconds => _model.ElapsedSeconds;
        public float TotalSeconds   => _model.TotalSeconds;
        public float HeroHpRatio    => _model.HeroMaxHp > 0
            ? (float)_model.HeroHp / _model.HeroMaxHp : 0f;
        //# 늦은 구독자용 현재 HP 정수값 (OnHeroHpValuesChanged 초기 동기화).
        public int HeroHp    => _model.HeroHp;
        public int HeroMaxHp => _model.HeroMaxHp;
        public BattleResult Result  => _model.Result;
        public IReadOnlyList<BuildEntry> Build => _build;

        //# 스포너 스냅샷 — 인덱스 0~5, AttachSpawners 이후에만 유효.
        public IReadOnlyList<SpawnerSnapshot> Spawners => _spawnerSnapshots;

        //# Spawner 6개 + BattleController 를 묶어 VM 이 종합 스냅샷을 제공.
        //# 초기 스냅샷은 직접 폴링으로 채우고, 이후 이벤트 구독으로 갱신.
        public void AttachSpawners(IReadOnlyList<Spawner> spawners, BattleController controller)
        {
            if (spawners == null || controller == null) return;
            //# 멱등 보장 — 이미 attach 돼 있으면 detach 먼저.
            if (_attachedSpawners != null) DetachSpawners();

            _attachedSpawners = spawners;
            _attachedController = controller;

            //# 초기 스냅샷 직접 폴링 — Spawner.OnEnable broadcast 에 의존 안 함 (기획서 §4.5).
            _spawnerSnapshots.Clear();
            _outputTypeHandlers  = new Action<EMonster>[spawners.Count];
            _outputCountHandlers = new Action<int>[spawners.Count];
            for (int i = 0; i < spawners.Count; ++i)
            {
                Spawner sp = spawners[i];
                if (sp == null)
                {
                    _spawnerSnapshots.Add(null);
                    continue;
                }
                _spawnerSnapshots.Add(BuildSnapshot(i, sp, controller));

                //# 인덱스 캡처 — 람다 클로저에서 정확한 인덱스로 갱신 콜백 호출.
                int idx = i;
                _outputTypeHandlers[i]  = _ => RecomputeAt(idx);
                _outputCountHandlers[i] = _ => RecomputeAt(idx);
                sp.OnOutputTypeChanged  += _outputTypeHandlers[i];
                sp.OnOutputCountChanged += _outputCountHandlers[i];
            }

            //# 컨트롤러 — 종 단위 강화 픽이 바뀌면, 그 종을 출력 중인 모든 셀 갱신.
            _typeModifierHandler = HandleTypeModifierChanged;
            controller.OnTypeModifierChanged += _typeModifierHandler;
        }

        //# 구독 해제 + 캐시 정리. BattleController.OnDestroy 또는 씬 재진입 시 호출 가능.
        public void DetachSpawners()
        {
            if (_attachedSpawners != null)
            {
                for (int i = 0; i < _attachedSpawners.Count; ++i)
                {
                    Spawner sp = _attachedSpawners[i];
                    if (sp == null) continue;
                    if (_outputTypeHandlers != null && _outputTypeHandlers[i] != null)
                        sp.OnOutputTypeChanged -= _outputTypeHandlers[i];
                    if (_outputCountHandlers != null && _outputCountHandlers[i] != null)
                        sp.OnOutputCountChanged -= _outputCountHandlers[i];
                }
            }
            if (_attachedController != null && _typeModifierHandler != null)
                _attachedController.OnTypeModifierChanged -= _typeModifierHandler;

            _attachedSpawners = null;
            _attachedController = null;
            _outputTypeHandlers = null;
            _outputCountHandlers = null;
            _typeModifierHandler = null;
            _spawnerSnapshots.Clear();
        }

        //# 컨트롤러 OnTypeModifierChanged 핸들러 — 출력 종이 일치하는 모든 인덱스 갱신.
        private void HandleTypeModifierChanged(EMonster type)
        {
            if (_attachedSpawners == null || _attachedController == null) return;
            for (int i = 0; i < _attachedSpawners.Count; ++i)
            {
                Spawner sp = _attachedSpawners[i];
                if (sp == null) continue;
                if (sp.CurrentType == type) RecomputeAt(i);
            }
        }

        //# 인덱스 한 개의 스냅샷 재계산 + 이벤트 발행.
        private void RecomputeAt(int index)
        {
            if (_attachedSpawners == null || _attachedController == null) return;
            if (index < 0 || index >= _attachedSpawners.Count) return;
            Spawner sp = _attachedSpawners[index];
            if (sp == null) return;
            _spawnerSnapshots[index] = BuildSnapshot(index, sp, _attachedController);
            OnSpawnerSnapshotChanged?.Invoke(index);
        }

        //# 한 Spawner 의 스냅샷을 BattleController 데이터로 합성.
        private static SpawnerSnapshot BuildSnapshot(int index, Spawner sp, BattleController controller)
        {
            return new SpawnerSnapshot
            {
                Index = index,
                CurrentType = sp.CurrentType,
                OutputCount = sp.OutputCount,
                AppliedBuffs = controller.GetAppliedBuffs(sp.CurrentType),
            };
        }
    }
}
