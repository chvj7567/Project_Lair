using ChvjUnityInfra;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ESC(뒤로가기) 닫힘 정책 — 베이스 HUD 는 안 닫히고(false), 일반 팝업은 기본 닫힘(true) 유지(Rule 03 인프라).
    public class VillageHudEscCloseTests
    {
        [Test]
        public void 마을HUD_는_ESC로_닫히지_않는다()
        {
            GameObject go = new GameObject("VillageHudEscTest");
            VillageHud hud = go.AddComponent<VillageHud>();

            //# override 로 false 여야 ESC 가 HUD 를 닫지 않는다(버그 픽스 핵심).
            Assert.IsFalse(hud.CanCloseByEsc);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void 일반_팝업은_기본값_ESC_닫힘을_유지한다()
        {
            GameObject go = new GameObject("ResultPopupEscTest");
            UIBase popup = go.AddComponent<ResultPopup>();

            //# 엣지/회귀 가드 — 기본 true 라 상점·랭킹 등 다른 팝업의 ESC 닫힘이 그대로 유지돼야 한다.
            Assert.IsTrue(popup.CanCloseByEsc);

            Object.DestroyImmediate(go);
        }
    }
}
