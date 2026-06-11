using System.IO;
using NUnit.Framework;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 마을 루프 BGM (village-bgm) — Rule 03 §2 에셋 키 정합 검증.
    //# 실제 루프 재생(AudioSource)은 async+Addressable 이라 EditMode 검증 불가 → 에셋/등록 정합만 박제.
    public class VillageBgmAssetTests
    {
        private const string SoundFolder = "Assets/_Lair/Art/Sound";
        private const string SoundGroupAsset = "Assets/AddressableAssetsData/AssetGroups/Sound.asset";

        [Test]
        public void VillageBgm_에셋파일명이_Enum값명과_일치한다()
        {
            //# Rule 03 §2 — Enum 값 이름 = 에셋 파일명(확장자 제외) 정확 일치. 깨지면 런타임 Load null.
            string path = $"{SoundFolder}/{EAudio.VillageBgm}.mp3";
            Assert.IsTrue(File.Exists(path), $"마을 BGM 에셋 누락 또는 파일명 불일치: {path}");
        }

        [Test]
        public void VillageBgm_Addressables_주소와_Resource라벨이_등록되어있다()
        {
            //# 엣지 — enum/파일은 있는데 Addressables 미등록이면 CHMResource 로드 실패. Sound 그룹 직렬화로 확인.
            string yaml = File.ReadAllText(SoundGroupAsset);
            int addressIndex = yaml.IndexOf($"m_Address: {EAudio.VillageBgm}");
            Assert.GreaterOrEqual(addressIndex, 0, "Sound 그룹에 VillageBgm 주소 미등록");

            //# 라벨 검증 윈도우는 해당 엔트리의 종결 마커까지만 — 고정 300자 윈도우는 다음 엔트리 라벨로 false-pass 가능.
            int entryEnd = yaml.IndexOf("FlaggedDuringContentUpdateRestriction", addressIndex);
            Assert.Greater(entryEnd, addressIndex, "VillageBgm 엔트리 종결 마커 누락 — 그룹 직렬화 형식 변경 확인 필요");
            string entryTail = yaml.Substring(addressIndex, entryEnd - addressIndex);
            StringAssert.Contains("- Resource", entryTail, "VillageBgm 엔트리에 Resource 라벨 누락");
        }
    }
}
