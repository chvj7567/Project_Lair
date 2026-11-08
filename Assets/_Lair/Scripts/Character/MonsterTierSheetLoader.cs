using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 전투 범위 티어 시트 로더·레지스트리(monster-tier-sheets §7) — 필요한 (종족, 티어)만 병렬 로드, 1회 재시도, 타임아웃, 실패 시 풋내기 폴백.
    //# 로드/해제는 델리게이트 주입(Rule 02 §5) — 기본은 CHMResource, 테스트는 가짜를 넣는다.
    public sealed class MonsterTierSheetLoader : IMonsterTierSheetProvider
    {
        public const int DefaultTimeoutMs = 5000;

        private readonly Func<EMonsterTierSheet, Task<MonsterTierSheetSet>> _load;
        private readonly Action<EMonsterTierSheet> _unload;
        private readonly Action<string> _warn;
        private readonly Dictionary<(EMonster, int), MonsterTierSheetSet> _sets = new Dictionary<(EMonster, int), MonsterTierSheetSet>();
        private readonly List<EMonsterTierSheet> _loadedKeys = new List<EMonsterTierSheet>();

        public MonsterTierSheetLoader(Func<EMonsterTierSheet, Task<MonsterTierSheetSet>> load, Action<EMonsterTierSheet> unload, Action<string> warn)
        {
            _load = load;
            _unload = unload;
            _warn = warn;
        }

        //# 키 규칙 — 값명 = <기준 몬스터 값명>T<티어>(CommonEnum EMonsterTierSheet). 없으면 false.
        public static bool TryKeyOf(EMonster baseSpecies, int tier, out EMonsterTierSheet key)
            => Enum.TryParse($"{baseSpecies}T{tier}", out key);

        public MonsterTierSheetSet Find(EMonster baseSpecies, int tier)
            => _sets.TryGetValue((baseSpecies, tier), out MonsterTierSheetSet set) ? set : null;

        //# needs = 티어 > 0 인 (기준 몬스터, 티어)만. 실패·시간 초과 종족은 등록하지 않아 풋내기로 진행(경고 로그 1줄).
        public async Task LoadAsync(IReadOnlyList<(EMonster, int)> needs, int timeoutMs = DefaultTimeoutMs)
        {
            List<EMonsterTierSheet> keys = new List<EMonsterTierSheet>();
            List<(EMonster, int)> targets = new List<(EMonster, int)>();
            List<Task<MonsterTierSheetSet>> tasks = new List<Task<MonsterTierSheetSet>>();
            foreach ((EMonster, int) need in needs)
            {
                //# 이미 로드된 (종족, 티어)는 다시 불러오지 않는다 — 마을·로딩 쇼케이스가 강화 구매 때마다 증분으로 호출.
                if (_sets.ContainsKey(need))
                    continue;
                if (TryKeyOf(need.Item1, need.Item2, out EMonsterTierSheet key) == false)
                {
                    _warn?.Invoke($"[MonsterTierSheet] 키 없음 — {need.Item1} T{need.Item2} 풋내기로 진행");
                    continue;
                }
                keys.Add(key);
                targets.Add(need);
                tasks.Add(LoadWithRetry(key));
            }
            if (tasks.Count == 0)
                return;

            Task all = Task.WhenAll(tasks);
            await Task.WhenAny(all, Task.Delay(timeoutMs));

            for (int i = 0; i < tasks.Count; i++)
            {
                Task<MonsterTierSheetSet> task = tasks[i];
                if (task.IsCompleted && task.IsFaulted == false && task.Result != null)
                {
                    _sets[targets[i]] = task.Result;
                    _loadedKeys.Add(keys[i]);
                    continue;
                }
                _warn?.Invoke($"[MonsterTierSheet] 로드 실패/시간 초과 — {targets[i].Item1} T{targets[i].Item2} 풋내기로 진행");
                if (task.IsCompleted == false)
                {
                    //# 늦게 도착한 결과는 해제(메인 스레드 컨텍스트로 되돌려 Addressables 호출).
                    EMonsterTierSheet lateKey = keys[i];
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted == false && t.Result != null)
                        {
                            _unload?.Invoke(lateKey);
                        }
                    }, SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default);
                }
            }
        }

        //# 전투 종료 → 몬스터 풀 정리 뒤 호출. 이번 판에 로드한 키 전부 해제.
        public void Release()
        {
            foreach (EMonsterTierSheet key in _loadedKeys)
            {
                _unload?.Invoke(key);
            }
            _loadedKeys.Clear();
            _sets.Clear();
        }

        private async Task<MonsterTierSheetSet> LoadWithRetry(EMonsterTierSheet key)
        {
            MonsterTierSheetSet set = await TryLoad(key);
            if (set == null)
            {
                set = await TryLoad(key);
            }
            return set;
        }

        private async Task<MonsterTierSheetSet> TryLoad(EMonsterTierSheet key)
        {
            try
            {
                return await _load(key);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
