using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 시너지 모달 BuildRows 테스트 공용 fake — 실제 tier 인스턴스(설명 계약 소유) + spec §6 스트링 템플릿 provider.
    //# BuildRows 가 provider·tierOf 주입형으로 바뀌어 모든 UI 테스트가 이 fake 로 조립 결과를 검증한다.
    internal static class SynergyModalTestFakes
    {
        //# spec §6 표의 "스트링 템플릿(Ko)" — Strings_Ko.json id 200~211 과 동일.
        private static readonly Dictionary<int, string> Templates = new()
        {
            { 200, "도깨비불·망령 HP ×{0}" },
            { 201, "도깨비불·망령 공격력 ×{0}" },
            { 202, "도깨비불·망령 HP ×{0}" },
            { 203, "사신·저주술사 공격력 ×{0}" },
            { 204, "사신·저주술사 공속 +{0}%" },
            { 205, "사신·저주술사 사거리 ×{0}" },
            { 206, "역병귀 둔화 ×{0}" },
            { 207, "영웅 공격력 ×{0}" },
            { 208, "출혈 영구 — 이동 시 1s당 HP -{0}%" },
            { 209, "환령·도깨비불 이동속도 ×{0}" },
            { 210, "모든 스포너 주기 ×{0}" },
            { 211, "모든 스포너 동시 출력 +{0}" },
        };

        //# (축, 임계 3/5/7) → 실제 tier 인스턴스. BattleController 의 BindTier 매핑과 동일.
        private static readonly Dictionary<(EBuildAxis, int), IBuildSynergyTier> Tiers = new()
        {
            { (EBuildAxis.Tank, 3),   new TankSynergyTier1() },
            { (EBuildAxis.Tank, 5),   new TankSynergyTier2() },
            { (EBuildAxis.Tank, 7),   new TankSynergyTier3() },
            { (EBuildAxis.Dps, 3),    new DpsSynergyTier1() },
            { (EBuildAxis.Dps, 5),    new DpsSynergyTier2() },
            { (EBuildAxis.Dps, 7),    new DpsSynergyTier3() },
            { (EBuildAxis.Debuff, 3), new DebuffSynergyTier1() },
            { (EBuildAxis.Debuff, 5), new DebuffSynergyTier2() },
            { (EBuildAxis.Debuff, 7), new DebuffSynergyTier3() },
            { (EBuildAxis.Swarm, 3),  new SwarmSynergyTier1() },
            { (EBuildAxis.Swarm, 5),  new SwarmSynergyTier2() },
            { (EBuildAxis.Swarm, 7),  new SwarmSynergyTier3() },
        };

        //# 실 provider(StringTableProvider) 대체 — id → 템플릿, 없으면 빈 문자열.
        private sealed class FakeStringProvider : IStringProvider
        {
            public string GetString(int stringID)
                => Templates.TryGetValue(stringID, out string t) ? t : "";
        }

        internal static readonly IStringProvider Strings = new FakeStringProvider();

        internal static IBuildSynergyTier TierOf(EBuildAxis axis, int threshold)
            => Tiers.TryGetValue((axis, threshold), out IBuildSynergyTier tier) ? tier : null;

        //# tierOf/strings 를 fake 로 고정한 BuildRows 래퍼 — 기존 1-arg/2-arg 호출부 시그니처를 그대로 재현.
        internal static List<SynergyModalCellData> BuildRows(Func<EBuildAxis, int> countOf)
            => Lair.UI.SynergyModalPopup.BuildRows(countOf, TierOf, Strings);

        internal static List<SynergyModalCellData> BuildRows(Func<EBuildAxis, int> countOf,
            Func<EBuildAxis, UnityEngine.Sprite> iconOf)
            => Lair.UI.SynergyModalPopup.BuildRows(countOf, TierOf, Strings, iconOf);
    }
}
