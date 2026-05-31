using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Card;
using Lair.Data;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class CardDataSyncerTests
    {
        private CardData _card;
        private Sprite _originalIcon;

        [SetUp]
        public void SetUp()
        {
            _card = ScriptableObject.CreateInstance<CardData>();
            SerializedObject so = new SerializedObject(_card);
            so.FindProperty("_id").enumValueIndex          = (int)ECardId.Berserk;
            so.FindProperty("_axis").enumValueIndex    = (int)EBuildAxis.Tank;
            so.FindProperty("_displayName").stringValue    = "폭주";
            so.FindProperty("_description").stringValue    = "테스트 설명";
            so.FindProperty("_effect").managedReferenceValue = new GuardianRageEffect();

            //# _icon 에 더미 Sprite 를 세팅 — ApplyDto 후 변경 여부 확인용
            Texture2D tex = new Texture2D(1, 1);
            _originalIcon = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
            so.FindProperty("_icon").objectReferenceValue = _originalIcon;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_originalIcon != null)
            {
                Object.DestroyImmediate(_originalIcon.texture);
                Object.DestroyImmediate(_originalIcon);
            }
            Object.DestroyImmediate(_card);
        }

        //# Export → JSON 에 id 필드 포함
        [Test]
        public void ExportToJson_Id포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            Assert.AreEqual("Berserk", arr[0]["id"]?.Value<string>());
        }

        //# Export → JSON 에 axis 필드 포함 (카드 리뉴얼 v0.6 — 키명 category → axis 변경).
        [Test]
        public void ExportToJson_Axis포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            //# 카드 리뉴얼 v0.6 — json 의 "axis" 키 + EBuildAxis enum 명 ("Tank").
            Assert.AreEqual("Tank", arr[0]["axis"]?.Value<string>());
        }

        //# Export → JSON 에 displayName 한글 포함
        [Test]
        public void ExportToJson_DisplayName포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            Assert.AreEqual("폭주", arr[0]["displayName"]?.Value<string>());
        }

        //# Export → JSON 에 description 필드 포함
        [Test]
        public void ExportToJson_Description포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            Assert.AreEqual("테스트 설명", arr[0]["description"]?.Value<string>());
        }

        //# Export → JSON 에 effect.$type 포함
        [Test]
        public void ExportToJson_EffectType포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            Assert.AreEqual("GuardianRageEffect", arr[0]["effect"]?["$type"]?.Value<string>());
        }

        //# Export → 빈 목록이면 빈 JSON 배열 반환
        [Test]
        public void ExportToJson_빈목록_빈배열반환()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData>());
            JArray arr = JArray.Parse(json);

            Assert.AreEqual(0, arr.Count);
        }

        //# Export → 여러 카드 순서대로 배열에 포함
        [Test]
        public void ExportToJson_다수카드_순서보존()
        {
            CardData card2 = ScriptableObject.CreateInstance<CardData>();
            SerializedObject so2 = new SerializedObject(card2);
            so2.FindProperty("_id").enumValueIndex       = (int)ECardId.Frenzy;
            so2.FindProperty("_axis").enumValueIndex = (int)EBuildAxis.Tank;
            so2.FindProperty("_displayName").stringValue = "광폭화";
            so2.FindProperty("_description").stringValue = "설명2";
            so2.FindProperty("_effect").managedReferenceValue = new FrenzyEffect();
            so2.ApplyModifiedPropertiesWithoutUndo();

            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card, card2 });
            JArray arr = JArray.Parse(json);

            Object.DestroyImmediate(card2);

            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual("Berserk", arr[0]["id"]?.Value<string>());
            Assert.AreEqual("Frenzy",  arr[1]["id"]?.Value<string>());
        }

        //# ApplyDto 성공 시 true 반환 (회귀 고정 — bool 반환 계약)
        [Test]
        public void ApplyDto_성공시_true반환()
        {
            CardDataDto dto = new CardDataDto
            {
                Id          = "Berserk",
                Category    = "Tank",
                DisplayName = "폭주",
                Description = "설명",
                Effect      = new GuardianRageEffect()
            };

            bool result = CardDataSyncer.ApplyDto(dto, _card);

            Assert.IsTrue(result);
        }

        //# ApplyDto → displayName 갱신
        [Test]
        public void ApplyDto_DisplayName갱신()
        {
            CardDataDto dto = new CardDataDto
            {
                Id          = "Berserk",
                Category    = "Tank",
                DisplayName = "새이름",
                Description = "새설명",
                Effect      = new GuardianRageEffect()
            };

            CardDataSyncer.ApplyDto(dto, _card);

            Assert.AreEqual("새이름", _card.DisplayName);
        }

        //# ApplyDto → description 갱신
        [Test]
        public void ApplyDto_Description갱신()
        {
            CardDataDto dto = new CardDataDto
            {
                Id          = "Berserk",
                Category    = "Tank",
                DisplayName = "폭주",
                Description = "바뀐 설명",
                Effect      = new GuardianRageEffect()
            };

            CardDataSyncer.ApplyDto(dto, _card);

            Assert.AreEqual("바뀐 설명", _card.Description);
        }

        //# ApplyDto → effect 타입 갱신 (GuardianRageEffect → FrenzyEffect)
        [Test]
        public void ApplyDto_Effect타입갱신()
        {
            CardDataDto dto = new CardDataDto
            {
                Id          = "Frenzy",
                Category    = "Tank",
                DisplayName = "광폭화",
                Description = "설명",
                Effect      = new FrenzyEffect()
            };

            CardDataSyncer.ApplyDto(dto, _card);

            Assert.IsInstanceOf<FrenzyEffect>(_card.Effect);
        }

        //# ApplyDto → _icon 은 건드리지 않음 (기획서 §8, §2 명시)
        [Test]
        public void ApplyDto_Icon미변경()
        {
            CardDataDto dto = new CardDataDto
            {
                Id          = "Berserk",
                Category    = "Tank",
                DisplayName = "새이름",
                Description = "새설명",
                Effect      = new GuardianRageEffect()
            };

            CardDataSyncer.ApplyDto(dto, _card);

            Assert.AreEqual(_originalIcon, _card.Icon,
                "ApplyDto 후 _icon 이 변경됨 — LairCardPrefabBuilder 전용 필드이므로 ApplyDto 가 건드려선 안 됨");
        }

        //# ApplyDto — 잘못된 Id 이면 false 반환 + SO 미변경 (기획서 §7 step4)
        [Test]
        public void ApplyDto_Id파싱실패_false반환_SO미변경()
        {
            string originalDisplay = _card.DisplayName;
            ECardId originalId     = _card.Id;
            CardDataDto dto = new CardDataDto
            {
                Id          = "InvalidId",
                Category    = "Tank",
                DisplayName = "변경된이름",
                Description = "설명",
                Effect      = new GuardianRageEffect()
            };

            bool result = CardDataSyncer.ApplyDto(dto, _card);

            Assert.IsFalse(result);
            Assert.AreEqual(originalDisplay, _card.DisplayName, "DisplayName 이 변경됨");
            Assert.AreEqual(originalId,      _card.Id,          "Id 가 변경됨");
        }

        //# ApplyDto — 잘못된 Category 이면 false 반환 + SO 미변경
        [Test]
        public void ApplyDto_카테고리파싱실패_false반환_SO미변경()
        {
            string originalName     = _card.DisplayName;
            ECardId originalId      = _card.Id;
            //# 카드 리뉴얼 v0.6 — Category → Axis (Phase 1).
            EBuildAxis originalAxis = _card.Axis;
            CardDataDto dto = new CardDataDto
            {
                Id          = "Berserk",
                Category    = "InvalidCategory",
                DisplayName = "변경된이름",
                Description = "설명",
                Effect      = new GuardianRageEffect()
            };

            bool result = CardDataSyncer.ApplyDto(dto, _card);

            Assert.IsFalse(result);
            Assert.AreEqual(originalName, _card.DisplayName, "DisplayName 이 변경됨");
            Assert.AreEqual(originalId,   _card.Id,          "Id 가 변경됨");
            Assert.AreEqual(originalAxis, _card.Axis,        "Axis 가 변경됨");
        }
    }
}
