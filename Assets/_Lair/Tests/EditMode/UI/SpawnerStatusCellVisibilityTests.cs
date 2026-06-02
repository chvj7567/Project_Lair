using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 중앙 아이콘 + 종색 테두리 visibility 회귀 (v1.1).
    //# v1.1 정책: 색칩 → 중앙 아이콘으로 역할 이관. 색칩은 항상 숨김, 종 식별은 아이콘 + 테두리 색.
    public class SpawnerStatusCellVisibilityTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        //# 비활성 셀 GO + 자식(colorChip / icon / border) 채워둔 상태로 생성. Unity OnEnable 자동 호출 회피용.
        //# wispSprite 가 주어지면 _wispIcon 필드에 주입 — 아이콘 표시 케이스 테스트용.
        private (SpawnerStatusCell cell, GameObject chipGo, GameObject iconGo, Image borderImg) CreateCell(Sprite wispSprite = null)
        {
            GameObject cellGo = new GameObject("Cell_UT");
            cellGo.SetActive(false);
            _spawned.Add(cellGo);

            SpawnerStatusCell cell = cellGo.AddComponent<SpawnerStatusCell>();

            GameObject chipGo = new GameObject("ColorChip");
            chipGo.transform.SetParent(cellGo.transform, false);
            Image chipImage = chipGo.AddComponent<Image>();
            SetField(cell, "_colorChip", chipImage);

            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cellGo.transform, false);
            Image iconImage = iconGo.AddComponent<Image>();
            SetField(cell, "_icon", iconImage);

            GameObject borderGo = new GameObject("Border");
            borderGo.transform.SetParent(cellGo.transform, false);
            Image borderImage = borderGo.AddComponent<Image>();
            SetField(cell, "_border", borderImage);

            if (wispSprite != null) SetField(cell, "_wispIcon", wispSprite);

            return (cell, chipGo, iconGo, borderImage);
        }

        private static void SetField(SpawnerStatusCell cell, string name, Object value)
        {
            FieldInfo fi = typeof(SpawnerStatusCell).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"SpawnerStatusCell.{name} 필드 존재 확인");
            fi.SetValue(cell, value);
        }

        private static Sprite MakeSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        private static void InvokeOnEnable(SpawnerStatusCell cell)
        {
            MethodInfo mi = typeof(SpawnerStatusCell).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SpawnerStatusCell.OnEnable 메서드 존재 확인");
            mi.Invoke(cell, null);
        }

        private static BattleViewModel.SpawnerSnapshot MakeSnapshot(int outputCount, EMonster type = EMonster.Wisp)
        {
            return new BattleViewModel.SpawnerSnapshot
            {
                Index = 0,
                CurrentType = type,
                OutputCount = outputCount,
                AppliedBuffs = new List<BattleViewModel.AppliedBuff>(),
            };
        }

        //# ===== 정상 케이스 — 스프라이트 있으면 중앙 아이콘 활성 =====

        [Test]
        public void RebindSnapshot_스프라이트_있으면_중앙아이콘_활성()
        {
            Sprite sprite = MakeSprite();
            (SpawnerStatusCell cell, _, GameObject iconGo, _) = CreateCell(wispSprite: sprite);
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1, type: EMonster.Wisp));
            Assert.IsTrue(iconGo.activeSelf, "Wisp 스프라이트 존재 → 중앙 아이콘 활성");
        }

        //# ===== 엣지 케이스 — 스프라이트 누락 시 아이콘 숨김 (테두리 색 fallback) =====

        [Test]
        public void RebindSnapshot_스프라이트_누락이면_아이콘_숨김()
        {
            //# wispSprite 미주입 → SpeciesSprite(Wisp) == null.
            (SpawnerStatusCell cell, _, GameObject iconGo, _) = CreateCell(wispSprite: null);
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1, type: EMonster.Wisp));
            Assert.IsFalse(iconGo.activeSelf, "스프라이트 누락 → 아이콘 숨김 (NRE 없이 fallback)");
        }

        //# ===== 테두리 — 종 대표색 적용 =====

        [Test]
        public void RebindSnapshot_테두리에_종대표색_적용()
        {
            (SpawnerStatusCell cell, _, _, Image borderImg) = CreateCell();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1, type: EMonster.Reaper));
            Assert.AreEqual(SpawnerStatusCell.SpeciesColor(EMonster.Reaper), borderImg.color,
                "테두리 색 = Reaper 종 대표색");
        }

        //# ===== 색칩 — v1.1 항상 숨김 (역할 이관) =====

        [Test]
        public void RebindSnapshot_색칩_항상_숨김()
        {
            (SpawnerStatusCell cell, GameObject chipGo, _, _) = CreateCell();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            Assert.IsFalse(chipGo.activeSelf, "v1.1 — 색칩은 중앙 아이콘으로 역할 이관, 항상 숨김");
        }

        //# ===== 풀 재사용 — OnEnable 이 직전 아이콘 스프라이트/활성 리셋 =====

        [Test]
        public void OnEnable_풀재사용시_아이콘_리셋()
        {
            (SpawnerStatusCell cell, _, GameObject iconGo, _) = CreateCell();
            //# 직전 셀이 아이콘 표시 상태로 풀 반환된 상황 시뮬.
            iconGo.SetActive(true);
            iconGo.GetComponent<Image>().sprite = MakeSprite();

            InvokeOnEnable(cell);

            Assert.IsFalse(iconGo.activeSelf, "OnEnable 이 직전 visibility 와 무관하게 아이콘 비활성 리셋 (Rule 03 §4)");
            Assert.IsNull(iconGo.GetComponent<Image>().sprite, "OnEnable 이 직전 스프라이트 제거 (잔존 방지)");
        }

        //# ===== null snapshot 방어 — NRE 없음 =====

        [Test]
        public void RebindSnapshot_null이면_NRE없음()
        {
            (SpawnerStatusCell cell, _, _, _) = CreateCell();
            InvokeOnEnable(cell);
            Assert.DoesNotThrow(() => cell.RebindSnapshot(null), "null snapshot → early return, NRE 없음");
        }
    }
}
