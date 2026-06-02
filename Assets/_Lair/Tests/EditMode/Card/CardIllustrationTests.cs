using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lair.Card;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 일러스트 표시 — 데이터/에셋/빌더/뷰/프리팹 정합 본격 스위트.
    //# 정상 + 엣지(null 폴백·미와이어) + 회귀(프리팹 배선·비파괴 보존·아이콘/아트 비혼동).
    //# 기획서 docs/design/card-illustrations.md §5(검증) · §6(구현요청) · §7(불변).
    //# PlayMode 없음 — 본 기능은 데이터 배선 + 순수 view 메서드(ApplyArt) 라 런타임 틱 의존 0 (§7 락 범위).
    public class CardIllustrationTests
    {
        private const string CardArtDir = "Assets/_Lair/Art/Sprites/CardArt";
        private const string CardItemsDir = "Assets/_Lair/Art/Cards/Items";
        private const string CardSelectionPopupPath = "Assets/_Lair/Art/UI/CardSelectionPopup.prefab";

        //# 테스트가 코드로 만든 GameObject — TearDown 에서 일괄 정리(누수 방지).
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        //# ===== 데이터/에셋 정합 (Task 1~3, §6.4) =====

        //# Task 1 — CardData 가 Sprite CardImage 게터를 노출.
        [Test]
        public void CardData_CardImage_게터_존재_타입_Sprite()
        {
            PropertyInfo p = typeof(CardData).GetProperty("CardImage");
            Assert.IsNotNull(p, "CardData.CardImage 게터 없음");
            Assert.AreEqual(typeof(Sprite), p.PropertyType, "CardImage 타입은 Sprite");
        }

        //# Task 2 — CardArt PNG 28장이 ECardId 28개와 대소문자까지 정확 일치.
        //# CollectionAssert.AreEqual 은 순서·대소문자 민감(ordinal) → 여분·누락·대소문자 불일치 전부 포착.
        //# (네이밍 강건화 항목 #4 는 본 케이스로 완전 커버 — 별도 케이스 불필요.)
        [Test]
        public void CardArt_PNG_28장_ECardId_이름_정합()
        {
            Assert.IsTrue(Directory.Exists(CardArtDir), $"CardArt 폴더 부재: {CardArtDir}");

            string[] pngStems = Directory.GetFiles(CardArtDir, "*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(s => s).ToArray();
            string[] ids = System.Enum.GetNames(typeof(ECardId)).OrderBy(s => s).ToArray();

            Assert.AreEqual(28, pngStems.Length, "CardArt PNG 28장");
            CollectionAssert.AreEqual(ids, pngStems, "CardArt 파일명 = ECardId 28개 정확 일치(대소문자 포함)");
        }

        //# Task 3 + 비파괴 회귀(§7) — 28장 모두 CardImage 충전 + 기존 _icon 보존(아이콘 작업 비파괴).
        //# _cardImage 추가가 기존 _icon 필드를 깨지 않았음을 한 루프로 동시 검증.
        //# (_effect non-null 은 CardRenewalConsistencyTests.모든_CardData_Effect_null_아님 이 별도 박제 — 중복 회피.)
        [Test]
        public void 모든_CardData_28장_CardImage_충전_및_Icon_보존()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { CardItemsDir });
            Assert.AreEqual(28, guids.Length, "CardData SO 28장");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                Assert.IsNotNull(card, $"SO 로드 실패: {path}");
                Assert.IsNotNull(card.CardImage, $"{card.name}.CardImage 미연결");
                Assert.IsNotNull(card.Icon, $"{card.name}.Icon 비파괴 보존 실패(§7 아이콘 파이프라인 불변)");
            }
        }

        //# 회귀(§7) — _icon 과 _cardImage 가 서로 다른 폴더의 에셋(CardIcons/ vs CardArt/).
        //# 아이콘/일러스트가 뒤바뀌지 않았음. 스팟 3장(Tank/Tank/Swarm 대표): WispHpBoost·Berserk·Slow.
        [Test]
        public void 스팟3장_Icon_과_CardImage_가_서로_다른_폴더_에셋()
        {
            ECardId[] spots = { ECardId.WispHpBoost, ECardId.Berserk, ECardId.Slow };
            foreach (ECardId id in spots)
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>($"{CardItemsDir}/{id}.asset");
                Assert.IsNotNull(card, $"{id} SO 로드 실패");
                Assert.IsNotNull(card.Icon, $"{id}.Icon 미연결");
                Assert.IsNotNull(card.CardImage, $"{id}.CardImage 미연결");

                string iconPath = AssetDatabase.GetAssetPath(card.Icon);
                string artPath = AssetDatabase.GetAssetPath(card.CardImage);
                StringAssert.Contains("/CardIcons/", iconPath, $"{id}.Icon 은 CardIcons/ 폴더 에셋이어야 함: {iconPath}");
                StringAssert.Contains("/CardArt/", artPath, $"{id}.CardImage 는 CardArt/ 폴더 에셋이어야 함: {artPath}");
                Assert.AreNotEqual(iconPath, artPath, $"{id} 의 Icon 과 CardImage 가 동일 에셋(뒤바뀜/혼동)");
            }
        }

        //# ===== 프리팹 배선 회귀 (code-reviewer 권장, §6.5) =====

        //# CardSelectionPopup.prefab 의 3개 CardView 슬롯 모두 _artImage·_countBadge 가 non-null.
        //# 빌더 재실행 누락/regression 으로 와이어링이 끊기면 런타임에 아트/3픽 배지가 안 나옴 → 사전 차단.
        //# _countBadge: M4 UI 빌더가 슬롯 재생성 시 손-와이어된 CountBadge("N/3")를 날린 회귀 박제(빌더가 생성/와이어).
        //# GetComponentsInChildren(true) — inactive 슬롯도 포함(빠뜨림 방지). prefab 1회 로드로 두 필드 동시 검증.
        [Test]
        public void CardSelectionPopup_3슬롯_artImage_및_countBadge_와이어링_non_null()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardSelectionPopupPath);
            Assert.IsNotNull(prefab, $"CardSelectionPopup 프리팹 부재: {CardSelectionPopupPath}");

            CardView[] views = prefab.GetComponentsInChildren<CardView>(true);
            Assert.AreEqual(3, views.Length, "CardSelectionPopup 의 CardView 슬롯 3개");

            foreach (CardView cv in views)
            {
                SerializedObject so = new SerializedObject(cv);

                SerializedProperty artProp = so.FindProperty("_artImage");
                Assert.IsNotNull(artProp, "_artImage SerializedProperty 존재");
                Assert.IsNotNull(artProp.objectReferenceValue,
                    $"CardView({cv.gameObject.name}) 의 _artImage 와이어링 끊김 — 빌더 재실행 필요");

                SerializedProperty badgeProp = so.FindProperty("_countBadge");
                Assert.IsNotNull(badgeProp, "_countBadge SerializedProperty 존재");
                Assert.IsNotNull(badgeProp.objectReferenceValue,
                    $"CardView({cv.gameObject.name}) 의 _countBadge 와이어링 끊김 — 빌더가 CountBadge 생성/와이어 필요(M4 회귀 가드)");
            }
        }

        //# ===== ApplyArt 폴백/정상 경로 (Task 4, §5·§6.6) =====
        //# EditMode 라 Awake/OnEnable lifecycle 미호출 → _artImage 는 리플렉션 주입.
        //# Bind 는 _nameText/_descText/_border/_pickButton 미와이어로 NRE → ApplyArt 직접 호출.

        //# 엣지 — CardImage 가 null 인 카드를 ApplyArt 하면 아트 영역 비활성(폴백).
        [Test]
        public void CardView_ApplyArt_CardImage_null_이면_아트영역_비활성()
        {
            CardView cv = MakeCardViewWithArt(out UnityEngine.UI.Image art);

            CardData card = ScriptableObject.CreateInstance<CardData>();   //# _cardImage = null
            cv.ApplyArt(card);

            Assert.IsFalse(art.gameObject.activeSelf, "CardImage null 이면 아트 영역 숨김");
            Object.DestroyImmediate(card);
        }

        //# 엣지 — card 자체가 null 이어도 예외 없이 아트 영역 비활성.
        [Test]
        public void CardView_ApplyArt_card_null_이면_예외없이_아트영역_비활성()
        {
            CardView cv = MakeCardViewWithArt(out UnityEngine.UI.Image art);

            Assert.DoesNotThrow(() => cv.ApplyArt(null), "card null 이어도 NRE 없음");
            Assert.IsFalse(art.gameObject.activeSelf, "card null 이면 아트 영역 숨김");
        }

        //# 엣지 — _artImage 미와이어(null)면 예외 없이 무동작(가드 절).
        [Test]
        public void CardView_ApplyArt_artImage_미와이어면_예외없이_무동작()
        {
            GameObject go = new GameObject("CardViewTest");
            _spawned.Add(go);
            CardView cv = go.AddComponent<CardView>();   //# _artImage 미주입 = null

            CardData card = ScriptableObject.CreateInstance<CardData>();
            Assert.DoesNotThrow(() => cv.ApplyArt(card), "_artImage null 이어도 NRE 없음");
            Object.DestroyImmediate(card);
        }

        //# 정상 — _cardImage 가 채워진 실제 카드를 ApplyArt 하면 sprite 설정 + 아트 영역 활성.
        [Test]
        public void CardView_ApplyArt_CardImage_충전이면_sprite_설정_및_활성()
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>($"{CardItemsDir}/{ECardId.WispHpBoost}.asset");
            Assert.IsNotNull(card, "WispHpBoost SO 로드 실패");
            Assert.IsNotNull(card.CardImage, "WispHpBoost.CardImage 미연결 — 빌더 재실행 필요");

            CardView cv = MakeCardViewWithArt(out UnityEngine.UI.Image art);
            art.gameObject.SetActive(false);   //# 사전 비활성 → ApplyArt 가 다시 켜는지 확인

            cv.ApplyArt(card);

            Assert.IsTrue(art.gameObject.activeSelf, "CardImage 있으면 아트 영역 활성");
            Assert.AreSame(card.CardImage, art.sprite, "_artImage.sprite 가 card.CardImage 로 설정");
        }

        //# ===== 헬퍼 =====

        //# 코드로 CardView + 자식 Art(Image) 구성 후 _artImage 리플렉션 주입.
        private CardView MakeCardViewWithArt(out UnityEngine.UI.Image art)
        {
            GameObject go = new GameObject("CardViewTest");
            _spawned.Add(go);
            CardView cv = go.AddComponent<CardView>();

            GameObject artGo = new GameObject("Art");
            artGo.transform.SetParent(go.transform);
            art = artGo.AddComponent<UnityEngine.UI.Image>();

            typeof(CardView).GetField("_artImage", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(cv, art);
            return cv;
        }
    }
}
