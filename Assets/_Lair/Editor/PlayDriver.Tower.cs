using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using UnityEditor;

namespace Lair.EditorTools
{
    //# Play 중 무한의 탑 팝업 미리보기 진입점 — 잠금 체크 생략, 메모리 전용 샘플 프로필(저장 안 함). 인자 없음.
    public static partial class PlayDriver
    {
        public static string ShowTowerPreview()
        {
            if (EditorApplication.isPlaying == false)
                return NotPlaying;

            TowerDatabase tower = TowerDatabase.Current;
            ChapterDatabase chapters = ChapterDatabase.Current;
            if (tower == null || chapters == null)
                return "FAIL tower data not loaded";

            //# 샘플 진행: 1~7층 클리어 → 도전 8층(현재), 9층 이후 잠김. MetaSession/Store 는 건드리지 않는다.
            MetaProfile sample = new MetaProfile();
            for (int floor = 1; floor <= 7; floor++)
            {
                sample.RecordTowerClear(floor, 60f + floor * 12.5f, tower.Config.seasonId);
            }

            int anchorHp = 0;
            int anchorPower = 0;
            BalanceConfig balance = BalanceConfig.Current;
            if (balance != null && balance.Hero != null)
            {
                anchorHp = balance.Hero.Hp;
                anchorPower = balance.Hero.Power;
            }

            TowerViewModel vm = new TowerViewModel(tower, chapters, sample, LookupTowerString, RelicDatabase.Current, anchorHp, anchorPower);
            CHMUI.Instance.ShowUI(EUI.TowerPopup, new TowerPopupArg
            {
                ViewModel = vm,
                OnChallenge = () => { },
            });
            return "OK tower";
        }

        private static string LookupTowerString(int id)
        {
            return CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
        }
    }
}
