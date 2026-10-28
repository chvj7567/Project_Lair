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

        //# 데일리 세션 서버 시각 (daily-challenge §2.2) — 데일리 팝업이 서버 시각을 받을 때 세운다. null = 이번 세션 서버 시각 없음(시작 불가). 저장 안 함.
        public static DailyClock DailyClock;
        //# 데일리 전투 컨텍스트 (§10.1) — 팝업 "도전 시작" 이 확정, Battle 이 읽는다. 결과창 "다시 도전" 은 같은 컨텍스트로 재시작. 마을 진입 시 지운다.
        public static DailyChallenge DailyContext;

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
