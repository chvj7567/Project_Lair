using NUnit.Framework;
using UnityEngine;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI 자체 검증 — SpawnerStatusCell 의 정적 매핑 (종 색·종명·아이콘 글자) 검증.
    //# UI 컴포넌트 라이프사이클은 PlayMode + test-engineer 영역. 본 테스트는 매핑 정확성만 정상+엣지 1.
    public class SpawnerStatusCellTests
    {
        //# 정상 — 6종 종명 영문 풀네임이 기획서 §2.2.3 매핑과 정확히 일치.
        [Test]
        public void SpeciesName_6종_영문풀네임_매핑()
        {
            Assert.AreEqual("Wisp",    SpawnerStatusCell.SpeciesName(EMonster.Wisp));
            Assert.AreEqual("Wraith",  SpawnerStatusCell.SpeciesName(EMonster.Wraith));
            Assert.AreEqual("Reaper",  SpawnerStatusCell.SpeciesName(EMonster.Reaper));
            Assert.AreEqual("Hex",     SpawnerStatusCell.SpeciesName(EMonster.Hex));
            Assert.AreEqual("Plague",  SpawnerStatusCell.SpeciesName(EMonster.Plague));
            Assert.AreEqual("Phantom", SpawnerStatusCell.SpeciesName(EMonster.Phantom));
        }

        //# 정상 — 강화 카드 6종 아이콘 글자 매핑 (H/D/S/R/M/P, 기획서 §2.3.3).
        [Test]
        public void IconLetterFor_6종_강화_카드_글자_매핑()
        {
            Assert.AreEqual('H', SpawnerStatusCell.IconLetterFor(ECardId.WispHpBoost).letter);
            Assert.AreEqual('D', SpawnerStatusCell.IconLetterFor(ECardId.WraithDamageBoost).letter);
            Assert.AreEqual('S', SpawnerStatusCell.IconLetterFor(ECardId.ReaperAtkSpeed).letter);
            Assert.AreEqual('R', SpawnerStatusCell.IconLetterFor(ECardId.HexRangeBoost).letter);
            Assert.AreEqual('M', SpawnerStatusCell.IconLetterFor(ECardId.PhantomMoveSpeedBoost).letter);
            Assert.AreEqual('P', SpawnerStatusCell.IconLetterFor(ECardId.PlagueSlowBoost).letter);
        }

        //# 엣지 — 강화 외 카드(예: Frenzy)는 매핑 안 됨 (' ' 반환). 셀이 row 를 숨기는 트리거.
        [Test]
        public void IconLetterFor_비강화_카드는_공백_반환()
        {
            Assert.AreEqual(' ', SpawnerStatusCell.IconLetterFor(ECardId.Frenzy).letter,
                "강화 외 카드는 매핑 미정 — 셀이 IconRow 를 숨기는 트리거");
        }

        //# 엣지 — Phantom 만 글자색이 흰색 (검은 배경 #1F2937 대비). 나머지는 검정.
        [Test]
        public void IconLetterFor_Phantom_글자색_흰색_나머지_검정()
        {
            Assert.AreEqual(Color.white, SpawnerStatusCell.IconLetterFor(ECardId.PhantomMoveSpeedBoost).fgColor);
            Assert.AreEqual(Color.black, SpawnerStatusCell.IconLetterFor(ECardId.WispHpBoost).fgColor);
        }

        //# v1.0 정상 — Spawn 카드 5종 글자 '+' 매핑 (§2.3.3 v1.0).
        [Test]
        public void IconLetterFor_Spawn카드_5종_플러스_매핑_v1점0()
        {
            Assert.AreEqual('+', SpawnerStatusCell.IconLetterFor(ECardId.SpawnWisps).letter);
            Assert.AreEqual('+', SpawnerStatusCell.IconLetterFor(ECardId.SpawnWraith).letter);
            Assert.AreEqual('+', SpawnerStatusCell.IconLetterFor(ECardId.SpawnReapers).letter);
            Assert.AreEqual('+', SpawnerStatusCell.IconLetterFor(ECardId.SpawnPlagues).letter);
            Assert.AreEqual('+', SpawnerStatusCell.IconLetterFor(ECardId.SpawnPhantoms).letter);
        }

        //# v1.0 엣지 — Hex 종은 SpawnHex 카드 부재. 5 Spawn 카드 매핑에 없음 → Hex 셀의 Spawn 슬롯은
        //# AppliedBuffs 안에 Spawn 픽이 없거나 (정상 시나리오 — pool 에 SpawnHex 가 없음), 잘못 들어와도
        //# IconLetterFor 가 fallback(' ') 반환으로 슬롯 비활성 (§2.3.3 v1.0 Hex 종 행).
        [Test]
        public void IconLetterFor_Hex종_Spawn_슬롯은_매핑외_fallback_v1점0()
        {
            //# SpawnHex 같은 enum 값 자체가 ECardId 에 없음 — 컴파일 검증으로 끝.
            //# Spawn 카드 5종 외 의도치 않은 비강화 카드 (예: Replace) 가 들어왔을 때 fallback 동작 확인.
            (char letter, Color bgColor, Color fgColor) info = SpawnerStatusCell.IconLetterFor(ECardId.ReplaceWispsToWraith);
            Assert.AreEqual(' ', info.letter, "Spawn 도 Enhance 도 아닌 카드는 fallback");
        }
    }
}
