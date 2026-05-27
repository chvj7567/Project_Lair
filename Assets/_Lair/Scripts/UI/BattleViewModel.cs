using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Card;
using Lair.Data;

namespace Lair.UI
{
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

        //# AttachSpawners 가 보관 — Detach 시 동일 인스턴스로 unsubscribe.
        private IReadOnlyList<Spawner> _attachedSpawners;
        private BattleController _attachedController;
        //# Spawner 별로 등록한 핸들러 캐시 — Detach 시 정확히 해제 (대응 인덱스 클로저).
        private Action<EMonster>[] _outputTypeHandlers;
        private Action<int>[] _outputCountHandlers;
        private Action<EMonster> _typeModifierHandler;

        public event Action<float, float> OnTimerChanged;
        public event Action<float> OnHeroHpRatioChanged;
        public event Action<BattleResult> OnBattleEnded;
        public event Action OnBuildChanged;

        //# 스포너 스냅샷 단독 갱신 — 6개 중 변경된 1개 인덱스만 알린다.
        public event Action<int> OnSpawnerSnapshotChanged;

        public BattleViewModel(BattleStateModel model)
        {
            _model = model;
        }

        public void UpdateTimer(float elapsed)
        {
            _model.ElapsedSeconds = elapsed;
            OnTimerChanged?.Invoke(elapsed, _model.TotalSeconds);
        }

        public void UpdateHeroHp(int current, int max)
        {
            _model.HeroHp = current;
            _model.HeroMaxHp = max;
            OnHeroHpRatioChanged?.Invoke(max > 0 ? (float)current / max : 0f);
        }

        public void EndBattle(BattleResult result)
        {
            _model.Result = result;
            OnBattleEnded?.Invoke(result);
        }

        //# 카드 픽 누적 — 같은 카드면 Count++, 아니면 신규 엔트리. 이후 OnBuildChanged.
        public void AddPick(CardData card, bool isPassive)
        {
            if (card == null) return;
            foreach (var e in _build)
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

        //# 늦은 구독자용 현재값
        public float ElapsedSeconds => _model.ElapsedSeconds;
        public float TotalSeconds   => _model.TotalSeconds;
        public float HeroHpRatio    => _model.HeroMaxHp > 0
            ? (float)_model.HeroHp / _model.HeroMaxHp : 0f;
        public BattleResult Result  => _model.Result;
        public IReadOnlyList<BuildEntry> Build => _build;

        //# 스포너 스냅샷 — 인덱스 0~5, AttachSpawners 이후에만 유효.
        public IReadOnlyList<SpawnerSnapshot> Spawners => _spawnerSnapshots;

        //# Spawner 6개 + BattleController 를 묶어 VM 이 종합 스냅샷을 제공.
        //# 초기 스냅샷은 직접 폴링으로 채우고, 이후 이벤트 구독으로 갱신.
        //# BattleController 와 lifecycle 이 동치라 unsubscribe leak 영향은 작지만,
        //# 안전을 위해 DetachSpawners 를 노출 (씬 재진입 / 테스트 재사용 대응).
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
                var sp = spawners[i];
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
                    var sp = _attachedSpawners[i];
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
                var sp = _attachedSpawners[i];
                if (sp == null) continue;
                if (sp.CurrentType == type) RecomputeAt(i);
            }
        }

        //# 인덱스 한 개의 스냅샷 재계산 + 이벤트 발행.
        private void RecomputeAt(int index)
        {
            if (_attachedSpawners == null || _attachedController == null) return;
            if (index < 0 || index >= _attachedSpawners.Count) return;
            var sp = _attachedSpawners[index];
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
