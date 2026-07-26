namespace Lair.Card
{
    //# Rule 02 §9 — 도메인별 공용 인터페이스 단일 파일. 분할 prefix: CommonInterface.BuildSynergy.
    //# 카드 리뉴얼 v0.6 — 빌드 시너지 Tier 효과 (Layer 1).
    //# 각 tier 는 자기 상수 + 자기 표기법으로 설명 메타(스트링 id + 수치 인자)를 소유한다 — 수치·표기 단일 소스.
    public interface IBuildSynergyTier
    {
        void Apply(IBattleContext ctx);

        //# 스트링 테이블(Strings_Ko.json) 템플릿 id (200 블록).
        int DescriptionStringId { get; }

        //# 자기 const 에서 문화권 무관(InvariantCulture)으로 포맷한 수치 인자 — 템플릿 {0} 치환용.
        string[] DescriptionArgs { get; }
    }
}
