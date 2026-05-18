namespace Lair.Data
{
    //# Rule 09 — 여러 시스템에서 참조되는 공용 Enum 단일 파일.
    //# Rule 08 — 값명은 에셋(프리팹/씬) 파일명과 정확히 일치해야 함.

    //# === Asset Keys (Rule 08) ===

    //# CHMResource 로 영웅 프리팹 로드.
    public enum EHero
    {
        Knight,
    }

    //# CHMResource 로 몬스터 프리팹 로드.
    public enum EMonster
    {
        Slime,
        Golem,
        Orc,
    }

    //# CHMUI.ShowUI 로 UI 프리팹 로드.
    public enum EUI
    {
        BattleHud,
        ResultPopup,
    }

    //# SceneManager.LoadScene(EScene.X.ToString()).
    public enum EScene
    {
        Battle,
    }

    //# === Cross-System Communication ===

    //# 전투 결과 — BattleStateModel / BattleViewModel / BattleController / ResultPopup 공용.
    public enum BattleResult
    {
        None,
        Win,
        Lose,
    }
}
