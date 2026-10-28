namespace Lair.Data
{
    //# 무한의 탑 Strings_Ko id (infinite-tower §6.6). 기획서의 전용 블록 1601~1624 는 실제 최대 id(1412) 다음 연속 구간 1413~1436 으로 병합했다 (기획 id - 188).
    public static class TowerStrings
    {
        public const int Button = 1413;           //# 탑                           (기획 1601)
        public const int Title = 1414;            //# 무한의 탑                     (1602)
        public const int SubtitleBest = 1415;     //# 시즌 {0} · 최고 {1}층          (1603)
        public const int SubtitleNone = 1416;     //# 시즌 {0} · 기록 없음           (1604)
        public const int FloorNumber = 1417;      //# {0}층                        (1605)
        public const int BossTag = 1418;          //# 보스층                        (1606)
        public const int ChallengeButton = 1419;  //# 도전 · {0}층                  (1607)
        public const int StatsLine = 1420;        //# 영웅 HP {0} · 공격력 {1}       (1608)
        public const int RulesLabel = 1421;       //# 층 규칙                       (1609)
        public const int RuleSource = 1422;       //# 규칙 출처: {0}-{1} {2}         (1610)
        public const int RewardLine = 1423;       //# 승리 시 유물 확률 {0}% · 등급 보정 {1} (1611)
        public const int ClearTag = 1424;         //# 클리어 {0}                    (1612)
        public const int PreviewTag = 1425;       //# 미리보기                      (1613)
        public const int RankingHeader = 1426;    //# 탑 랭킹 · 시즌 {0}             (1614) - 슬라이스 B
        public const int RankingPlace = 1427;     //# {0}위                        (1615) - 슬라이스 B
        public const int RankingMine = 1428;      //# 내 순위 {0}위 · {1}층          (1616) - 슬라이스 B
        public const int RankingFailed = 1429;    //# 랭킹을 불러오지 못했습니다       (1617)
        public const int LockedToast = 1430;      //# 탑은 {0} 클리어 시 열립니다      (1618)
        public const int Guide = 1431;            //# 하단 안내                      (1619)
        public const int NextFloor = 1432;        //# 다음 층                       (1620)
        public const int ReachedFloor = 1433;     //# 도달 층                       (1621)
        public const int FloorCleared = 1434;     //# {0}층 돌파!                   (1622)
        public const int TitleFloor = 1435;       //# 탑 {0}층                      (1623)
        public const int ChallengeTag = 1436;     //# 도전 중                       (1624)

        //# 재사용 기존 문구.
        public const int NoRecord = 1306;         //# 기록 없음
        public const int NoRules = 1310;          //# 특수 규칙 없음
        public const int HeroName = 1321;         //# 영웅: {0}
        public const int NotReady = 1322;         //# 준비 중
        public const int BestShort = 1336;        //# 최고 {0}
        public const int Retry = 34;              //# 다시 도전
    }
}
