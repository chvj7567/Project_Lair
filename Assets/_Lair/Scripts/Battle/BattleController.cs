using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using Lair.UI;
using UnityEngine;

namespace Lair.Battle
{
    //# 전투 씬 라이프사이클·스폰·VM 갱신·종료 처리.
    public class BattleController : MonoBehaviour
    {
        [SerializeField] private Transform _heroSpawn;
        [SerializeField] private MonsterSpawnEntry[] _monsterSpawns;

        private BattleClock _clock;
        private BattleStateModel _model;
        private BattleViewModel _vm;

        private CHPoolable _hero;
        private Health _heroHealth;
        private readonly List<CHPoolable> _monsters = new();

        async void Start()
        {
            //# 1. ChvjPackage 초기화
            if (await CHMResource.Instance.Init() == false)
            {
                Debug.LogError("[BattleController] CHMResource.Init 실패");
                return;
            }
            CHMUI.Instance.Init();
            CHMPool.Instance.Init();

            //# 2. MVVM
            _model = new BattleStateModel();
            _vm = new BattleViewModel(_model);

            //# 3. HUD 표시
            await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm });

            //# 4. 스폰
            await SpawnHero();
            await SpawnMonsters();

            //# 5. 시계
            _clock = new BattleClock(_model.TotalSeconds);
            _clock.OnTick   += _vm.UpdateTimer;
            _clock.OnTimeUp += () => EndBattle(BattleResult.Lose);
            _clock.Start();
        }

        private void Update()
        {
            _clock?.Tick(Time.deltaTime);
        }

        private async Task SpawnHero()
        {
            var prefab = await CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight);
            if (prefab == null)
            {
                Debug.LogError("[BattleController] Knight 프리팹 로드 실패");
                return;
            }

            var p = CHMPool.Instance.Pop(prefab, transform);
            if (p == null) return;
            p.transform.position = _heroSpawn != null ? _heroSpawn.position : Vector3.zero;

            _hero = p;
            _heroHealth = p.GetComponent<Health>();
            if (_heroHealth != null)
            {
                _heroHealth.OnChanged += _vm.UpdateHeroHp;
                _heroHealth.OnDied    += () => EndBattle(BattleResult.Win);
                _vm.UpdateHeroHp(_heroHealth.Current, _heroHealth.Max);
            }
        }

        private async Task SpawnMonsters()
        {
            if (_monsterSpawns == null) return;
            foreach (var sp in _monsterSpawns)
            {
                if (sp.Point == null) continue;
                var prefab = await CHMResource.Instance.LoadAsync<GameObject>(sp.Key);
                if (prefab == null) continue;
                var p = CHMPool.Instance.Pop(prefab, transform);
                if (p == null) continue;
                p.transform.position = sp.Point.position;
                _monsters.Add(p);
            }
        }

        private async void EndBattle(BattleResult result)
        {
            if (_model.Result != BattleResult.None) return;   //# 중복 방지
            _clock.Stop();

            //# 모든 AI 정지
            foreach (var ai in GetComponentsInChildren<AutoCombatAI>())
                ai.enabled = false;

            _vm.EndBattle(result);

            await CHMUI.Instance.ShowUIAsync(EUI.ResultPopup,
                new ResultPopupArg { Result = result });
        }
    }
}
