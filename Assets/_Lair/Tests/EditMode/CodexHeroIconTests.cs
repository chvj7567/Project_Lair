using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 도감 몬스터 아이콘 — 해금/미해금 셀 데이터 경로 검증 (영웅 셀은 HeroSelectCellDataTests).
    public class CodexHeroIconTests
    {
        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object obj in _spawned)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            _spawned.Clear();
        }

        private Sprite NewSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            _spawned.Add(tex);
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            _spawned.Add(sprite);
            return sprite;
        }

        private MetaConfig NewConfig()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _spawned.Add(cfg);
            return cfg;
        }

        [Test]
        public void 도감_몬스터_해금_셀은_종별_아이콘을_싣는다()
        {
            MetaProfile profile = new MetaProfile();
            foreach (EMonster type in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                profile.SeenMonsters.Add(type.ToString());
            }
            Sprite icon = NewSprite();

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(profile, NewConfig(), _ => icon);

            int monsterCount = Enum.GetValues(typeof(EMonster)).Length;
            for (int i = 0; i < monsterCount; ++i)
            {
                Assert.AreSame(icon, list[i].Icon, $"몬스터 {i}번 셀에 아이콘 누락");
                Assert.IsTrue(list[i].Unlocked);
            }
        }

        [Test]
        public void 도감_미해금_몬스터도_아이콘은_싣되_Unlocked_false_로_실루엣_경로_유지()
        {
            MetaProfile profile = new MetaProfile();   //# SeenMonsters 비어 있음
            Sprite icon = NewSprite();

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(profile, NewConfig(), _ => icon);

            //# CodexCell.Bind 는 Icon!=null && Unlocked==false 면 SilhouetteColor 틴트 — 데이터 경로 확인.
            Assert.AreSame(icon, list[0].Icon);
            Assert.IsFalse(list[0].Unlocked);
            Assert.AreEqual("???", list[0].DisplayName);
        }

    }
}
