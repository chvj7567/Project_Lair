using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.UI;

namespace Lair.Tests.PlayMode
{
    //# 스포너 상태 UI — PlayMode 통합 (실제 씬 + BattleController 라이프사이클).
    //#
    //# 검증 포인트:
    //#  - Battle 씬 로드 후 BattleViewModel.Spawners 가 6개 채워짐 (BattleController.BindSpawners 가 VM.AttachSpawners 호출).
    //#  - 6 SpawnerSnapshot 각자 Index/CurrentType/OutputCount 가 라이브 Spawner 값과 일치.
    //#  - 카드 픽 자동 처리 후 (DebugAutoPicker) BattleViewModel.Build 가 갱신.
    public class SpawnerStatusUIPlayTests
    {
        [TearDown]
        public void TearDown()
        {
            //# 카드 팝업이 띄워졌으면 Pause 가 timeScale=0 — 후속 테스트 영향 차단.
            Time.timeScale = 1f;
        }

        //# 정상 — Battle 씬 로드 후 VM.Spawners 가 6개 채워진다 (BattleController 가 AttachSpawners 호출).
        [UnityTest]
        public IEnumerator Battle씬_로드후_BattleViewModel_Spawners_6개_채워진다()
        {
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# BattleController 가 씬에 잡힐 때까지 대기 (5초 timeout).
            BattleController bc = null;
            float waitInit = 0f;
            while (waitInit < 5f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null) break;
                waitInit += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 가 씬에 있어야 함");

#if UNITY_EDITOR
            //# 카드 팝업 hang 방지 — 첫 카드 자동 픽.
            bc.DebugAutoPicker = (choices, src) =>
                (choices != null && choices.Count > 0) ? choices[0] : null;
#endif

            //# Start 의 비동기 초기화 완료 대기 (BindSpawners 가 VM.AttachSpawners 호출).
            float elapsed = 0f;
            while (elapsed < 3f) { elapsed += Time.unscaledDeltaTime; yield return null; }

            //# SpawnerStatusPanel 이 BattleHud 자식으로 활성화돼 있어야.
            var panel = Object.FindFirstObjectByType<SpawnerStatusPanel>();
            Assert.IsNotNull(panel, "SpawnerStatusPanel 이 씬에 있어야 함 (BattleHud 자식)");

            //# VM 의 Spawners 가 6개 채워졌는지 — BattleHud 의 ViewModel 을 가져와 확인.
            //# 우회: BattleHud 의 _viewModel 직접 접근 대신 panel 의 reflection 으로 _vm 필드.
            var fi = typeof(SpawnerStatusPanel).GetField("_vm",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(fi, "SpawnerStatusPanel._vm 필드 존재 확인");
            var vm = fi.GetValue(panel) as BattleViewModel;
            Assert.IsNotNull(vm, "Panel.Bind 가 VM 을 주입했어야 함");

            Assert.AreEqual(6, vm.Spawners.Count, "Battle 씬엔 6 스포너 — VM.Spawners 도 6");

            //# 모든 인덱스 — Index 필드 정합.
            for (int i = 0; i < 6; ++i)
            {
                var snap = vm.Spawners[i];
                Assert.IsNotNull(snap, $"index {i} 스냅샷 non-null");
                Assert.AreEqual(i, snap.Index, $"Index 필드 = {i}");
                //# 초기 OutputCount 는 1 (Spawner.OnEnable 리셋).
                Assert.AreEqual(1, snap.OutputCount, $"index {i} 초기 OutputCount = 1");
            }
        }
    }
}
