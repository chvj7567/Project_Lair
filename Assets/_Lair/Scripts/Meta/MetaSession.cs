using Lair.Data;

namespace Lair.Meta
{
    //# 씬 전환 간 프로필 공유 static 홀더. Village 진입 시 Load, Battle 은 null 이면 직접 Load (에디터 Battle 직행 안전).
    //# 연동 핸들·플래그는 MetaSession.Net.cs (partial) 참조.
    public static partial class MetaSession
    {
        public static MetaProfile Profile;
        public static MetaProfileStore Store;

        //# 이번 전투가 챕터인지 탑인지 (infinite-tower §6.3) — 메모리 전용. 마을 진입 시 Chapter 로 되돌리고, 탑 팝업 "도전" 이 Tower 로 세운다.
        //# 결과창 "다음 층/다시 도전" 은 Battle 씬만 재시작하므로 모드가 유지된다(E-2). 앱 재시작이면 기본값 Chapter (E-4).
        public static EBattleMode BattleMode = EBattleMode.Chapter;

        public static MetaProfile GetOrLoad()
        {
            if (Store == null)
            {
                Store = new MetaProfileStore();
            }
            if (Profile == null)
            {
                Profile = Store.Load();
            }
            return Profile;
        }
    }
}
