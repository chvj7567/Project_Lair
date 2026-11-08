using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# MonsterVisual2D 의 티어 시트 스프라이트 치환(monster-tier-sheets §6, U-T6·U-T8) — 이름 끝 숫자 인덱스 규약과 폴백 경로.
    //# EditMode 에선 LateUpdate·OnEnable 이 자동 호출되지 않으므로 private ApplyTierSheet / OnEnable 을 리플렉션으로 직접 부른다.
    public class MonsterVisual2DTierSheetTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private GameObject _go;
        private SpriteRenderer _body;
        private MonsterVisual2D _visual;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Visual2D");
            _body = _go.AddComponent<SpriteRenderer>();
            _visual = _go.AddComponent<MonsterVisual2D>();
            _visual.SetBodyForTest(_body);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (Object obj in _objects)
            {
                Object.DestroyImmediate(obj);
            }
            _objects.Clear();
        }

        private Sprite MakeSprite(string spriteName)
        {
            Texture2D tex = new Texture2D(4, 4);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 100f);
            sprite.name = spriteName;
            _objects.Add(tex);
            _objects.Add(sprite);
            return sprite;
        }

        private MonsterTierSheetSet MakeSheet(int frameCount, float hpBarHeight = 0.8f)
        {
            Sprite[] frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                frames[i] = MakeSprite($"Wisp_Sheet_T2_{i}");
            }
            MonsterTierSheetSet sheet = ScriptableObject.CreateInstance<MonsterTierSheetSet>();
            sheet.SetForTest(frames, hpBarHeight);
            _objects.Add(sheet);
            return sheet;
        }

        private void Apply()
        {
            MethodInfo mi = typeof(MonsterVisual2D).GetMethod("ApplyTierSheet", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "MonsterVisual2D.ApplyTierSheet 존재 - 시그니처 변경 감지");
            mi.Invoke(_visual, null);
        }

        [Test]
        public void 이름_끝_숫자와_같은_인덱스의_티어_스프라이트로_치환한다()
        {
            MonsterTierSheetSet sheet = MakeSheet(30);
            _body.sprite = MakeSprite("Wisp_Sheet_7");
            _visual.SetTierSheet(sheet);

            Apply();

            Assert.AreSame(sheet.GetFrame(7), _body.sprite);
            Assert.AreEqual("Wisp_Sheet_T2_7", _body.sprite.name);
        }

        [Test]
        public void 치환_결과_이름도_같은_인덱스라_연속_호출해도_결과가_같다()
        {
            MonsterTierSheetSet sheet = MakeSheet(30);
            _body.sprite = MakeSprite("Wisp_Sheet_12");
            _visual.SetTierSheet(sheet);

            Apply();
            Sprite first = _body.sprite;
            Apply();
            Apply();

            Assert.AreSame(first, _body.sprite, "멱등 - 프레임마다 호출돼도 같은 칸");
            Assert.AreSame(sheet.GetFrame(12), _body.sprite);
        }

        [Test]
        public void 첫칸과_마지막칸_인덱스도_치환된다()
        {
            MonsterTierSheetSet sheet = MakeSheet(30);
            _visual.SetTierSheet(sheet);

            _body.sprite = MakeSprite("Wisp_Sheet_0");
            Apply();
            Assert.AreSame(sheet.GetFrame(0), _body.sprite, "인덱스 0");

            _body.sprite = MakeSprite("Wisp_Sheet_29");
            Apply();
            Assert.AreSame(sheet.GetFrame(29), _body.sprite, "인덱스 29");
        }

        [TestCase("Wisp_Sheet_30")]
        [TestCase("Wisp_Sheet_35")]
        [TestCase("Wisp_Sheet_-1")]
        [TestCase("Wisp")]
        [TestCase("Wisp_")]
        [TestCase("Wisp_Sheet_abc")]
        [TestCase("")]
        public void 인덱스를_못_읽거나_범위_밖이면_몸_스프라이트를_그대로_둔다(string spriteName)
        {
            MonsterTierSheetSet sheet = MakeSheet(30);
            Sprite original = MakeSprite(spriteName);
            _body.sprite = original;
            _visual.SetTierSheet(sheet);

            Apply();

            Assert.AreSame(original, _body.sprite, $"'{spriteName}' 은 치환하지 않음");
        }

        [Test]
        public void 시트가_null_이면_치환하지_않는다()
        {
            Sprite original = MakeSprite("Wisp_Sheet_3");
            _body.sprite = original;
            _visual.SetTierSheet(null);

            Apply();

            Assert.AreSame(original, _body.sprite);
        }

        [Test]
        public void 몸_스프라이트가_null_이면_예외없이_무시한다()
        {
            _body.sprite = null;
            _visual.SetTierSheet(MakeSheet(30));

            Assert.DoesNotThrow(Apply);
            Assert.IsNull(_body.sprite);
        }

        [Test]
        public void 프레임_배열이_짧거나_해당_칸이_비면_몸_스프라이트를_유지한다()
        {
            MonsterTierSheetSet shortSheet = MakeSheet(5);
            Sprite original = MakeSprite("Wisp_Sheet_9");
            _body.sprite = original;
            _visual.SetTierSheet(shortSheet);

            Apply();
            Assert.AreSame(original, _body.sprite, "프레임 5개짜리 세트의 인덱스 9 는 범위 밖");

            MonsterTierSheetSet emptySlot = ScriptableObject.CreateInstance<MonsterTierSheetSet>();
            _objects.Add(emptySlot);
            emptySlot.SetForTest(new Sprite[30], 0.8f);
            _visual.SetTierSheet(emptySlot);

            Apply();
            Assert.AreSame(original, _body.sprite, "빈 슬롯은 치환하지 않음");
        }

        [Test]
        public void 치환은_flipX_를_건드리지_않는다()
        {
            MonsterTierSheetSet sheet = MakeSheet(30);
            _body.flipX = true;
            _body.sprite = MakeSprite("Wisp_Sheet_4");
            _visual.SetTierSheet(sheet);

            Apply();

            Assert.IsTrue(_body.flipX, "flipX 동기는 Facing 소관 - 시트 치환이 덮어쓰지 않음");
        }

        [Test]
        public void 풀_재사용_OnEnable_에서_시트_참조가_해제된다()
        {
            _visual.SetTierSheet(MakeSheet(30));
            Assert.IsNotNull(_visual.TierSheetForTest);
            MethodInfo mi = typeof(MonsterVisual2D).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "MonsterVisual2D.OnEnable 존재 - 시그니처 변경 감지");

            mi.Invoke(_visual, null);

            Assert.IsNull(_visual.TierSheetForTest, "Pop 시 티어 0 으로 복귀(U-T8)");
        }

        [Test]
        public void 시트를_다른_세트로_교체하면_다음_치환부터_새_세트를_쓴다()
        {
            MonsterTierSheetSet first = MakeSheet(30);
            MonsterTierSheetSet second = MakeSheet(30);
            _body.sprite = MakeSprite("Wisp_Sheet_2");
            _visual.SetTierSheet(first);
            Apply();
            Assert.AreSame(first.GetFrame(2), _body.sprite);

            _visual.SetTierSheet(second);
            Apply();

            Assert.AreSame(second.GetFrame(2), _body.sprite);
        }
    }
}
