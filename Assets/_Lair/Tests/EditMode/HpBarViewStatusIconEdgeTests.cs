using System.Reflection;
using ChvjUnityInfra;
using NUnit.Framework;
using Lair.Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# HpBarView 상태 아이콘 행 — 엣지/회귀 보강 (test-engineer).
    //# 기본 Add/Remove/같은key중복 정상 케이스는 HpBarViewTests 가 이미 커버 — 여기선 망라.
    //# 다중 동시 + 부분 제거 / 슬롯 경계(>8) / 빈슬롯 후 재추가 / null sprite graceful /
    //# 미등록 key 제거 no-op / ClearStatusIcons 를 검증한다.
    public class HpBarViewStatusIconEdgeTests
    {
        private GameObject _barGo;
        private HpBarView _view;
        private GameObject _iconRow;
        private Image[] _iconSlots;

        private const int SlotCount = 8;

        [SetUp]
        public void SetUp()
        {
            _barGo = new GameObject("TestHpBar");
            _view = _barGo.AddComponent<HpBarView>();

            //# Fill / txtHp 도 와이어링 — HpBarView 가 다른 경로에서 NRE 안 나게 SetUp 일관성 유지.
            Image fill = new GameObject("Fill").AddComponent<Image>();
            fill.transform.SetParent(_barGo.transform, false);
            fill.type = Image.Type.Filled;

            GameObject txtGo = new GameObject("txtHp");
            txtGo.transform.SetParent(_barGo.transform, false);
            txtGo.AddComponent<TextMeshProUGUI>();
            CHText txtHp = txtGo.AddComponent<CHText>();

            SetPrivate("_fill", fill);
            SetPrivate("_txtHp", txtHp);

            //# 상태 아이콘 행 — 8 슬롯 Image + 컨테이너(기본 비활성).
            _iconRow = new GameObject("StatusIconRow");
            _iconRow.transform.SetParent(_barGo.transform, false);
            _iconSlots = new Image[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                _iconSlots[i] = new GameObject($"Slot{i}").AddComponent<Image>();
                _iconSlots[i].transform.SetParent(_iconRow.transform, false);
                _iconSlots[i].gameObject.SetActive(false);
            }
            _iconRow.SetActive(false);
            SetPrivate("_statusIconRow", _iconRow);
            SetPrivate("_iconSlots", _iconSlots);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_barGo);
        }

        private void SetPrivate(string field, object value)
        {
            typeof(HpBarView)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_view, value);
        }

        private static Sprite MakeSprite()
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        }

        //# 1. 다중 동시 상태 — 서로 다른 key 3개 → 슬롯 0/1/2 가 순서대로 채워진다.
        [Test]
        public void AddStatusIcon_서로다른key3개_낮은슬롯부터_순서대로채움()
        {
            Sprite sp = MakeSprite();
            _view.AddStatusIcon("slow", sp);
            _view.AddStatusIcon("fear", sp);
            _view.AddStatusIcon("bleed", sp);

            Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
            Assert.IsTrue(_iconSlots[1].gameObject.activeSelf);
            Assert.IsTrue(_iconSlots[2].gameObject.activeSelf);
            Assert.IsFalse(_iconSlots[3].gameObject.activeSelf);
            Assert.IsTrue(_iconRow.activeSelf);
        }

        //# 1. 부분 제거 — 다중 중 가운데 key 만 제거 시 그 슬롯만 비활성, 나머지 유지, 컨테이너 활성.
        [Test]
        public void RemoveStatusIcon_다중중_가운데key만제거_해당슬롯만비활성()
        {
            Sprite sp = MakeSprite();
            _view.AddStatusIcon("slow", sp);   //# slot 0
            _view.AddStatusIcon("fear", sp);   //# slot 1
            _view.AddStatusIcon("bleed", sp);  //# slot 2

            _view.RemoveStatusIcon("fear");    //# 가운데(slot 1) 제거

            Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
            Assert.IsFalse(_iconSlots[1].gameObject.activeSelf);
            Assert.IsTrue(_iconSlots[2].gameObject.activeSelf);
            //# 아직 2개 남았으니 컨테이너는 활성 유지.
            Assert.IsTrue(_iconRow.activeSelf);
        }

        //# 2. 슬롯 경계 — 8개 채운 뒤 9번째 추가는 graceful no-op (예외 없음, 추가 무시).
        [Test]
        public void AddStatusIcon_8개초과_조용히무시_예외없음()
        {
            Sprite sp = MakeSprite();
            for (int i = 0; i < SlotCount; i++)
            {
                _view.AddStatusIcon($"k{i}", sp);
            }

            //# 9번째 — 빈 슬롯 없음. 예외 없이 무시.
            Assert.DoesNotThrow(() => _view.AddStatusIcon("overflow", sp));

            //# 8 슬롯 전부 활성, 추가분은 어디에도 안 들어감.
            foreach (Image slot in _iconSlots)
            {
                Assert.IsTrue(slot.gameObject.activeSelf);
            }
            //# overflow key 제거는 미등록 key → no-op. 등록 안 됐음을 간접 확인:
            //# overflow 가 슬롯을 점유하지 않았으므로 RemoveStatusIcon 후에도 변화 없음.
            _view.RemoveStatusIcon("overflow");
            foreach (Image slot in _iconSlots)
            {
                Assert.IsTrue(slot.gameObject.activeSelf);
            }
        }

        //# 2. 빈 슬롯 없는 상태에서 하나 제거 후 재추가 가능 — 비워진 가장 낮은 슬롯 재사용.
        [Test]
        public void AddStatusIcon_가득찬후_제거하고재추가_빈슬롯재사용()
        {
            Sprite sp = MakeSprite();
            for (int i = 0; i < SlotCount; i++)
            {
                _view.AddStatusIcon($"k{i}", sp);
            }

            //# slot 3 을 비움.
            _view.RemoveStatusIcon("k3");
            Assert.IsFalse(_iconSlots[3].gameObject.activeSelf);

            //# 재추가 — lowest free slot(=3) 재사용 (§4.1).
            _view.AddStatusIcon("again", sp);
            Assert.IsTrue(_iconSlots[3].gameObject.activeSelf);
        }

        //# 6. null sprite graceful — 슬롯 매핑은 잡되 Image 미표시(enabled=false), GameObject 는 활성.
        [Test]
        public void AddStatusIcon_null스프라이트_슬롯활성이되_Image비표시_예외없음()
        {
            Assert.DoesNotThrow(() => _view.AddStatusIcon("noicon", null));

            //# 슬롯/컨테이너는 활성 — 매핑은 잡힌다.
            Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
            Assert.IsTrue(_iconRow.activeSelf);
            //# 하지만 Image 컴포넌트는 그리지 않음(graceful).
            Assert.IsFalse(_iconSlots[0].enabled);
        }

        //# 6. null sprite 슬롯도 점유로 카운트 — 다음 추가는 slot 1 로 간다 + Remove 정상.
        [Test]
        public void AddStatusIcon_null슬롯도점유_다음은slot1_그리고제거정상()
        {
            Sprite sp = MakeSprite();
            _view.AddStatusIcon("noicon", null);  //# slot 0 (enabled=false)
            _view.AddStatusIcon("real", sp);      //# slot 1

            Assert.IsTrue(_iconSlots[1].gameObject.activeSelf);
            Assert.IsTrue(_iconSlots[1].enabled);

            //# null 슬롯 제거 정상 — slot 0 비활성, 매핑 해제.
            _view.RemoveStatusIcon("noicon");
            Assert.IsFalse(_iconSlots[0].gameObject.activeSelf);
            //# real 은 그대로.
            Assert.IsTrue(_iconSlots[1].gameObject.activeSelf);
        }

        //# 7. 미등록 key 제거 — no-op, 예외 없음, 기존 상태 불변.
        [Test]
        public void RemoveStatusIcon_미등록key_noop_예외없음()
        {
            Sprite sp = MakeSprite();
            _view.AddStatusIcon("slow", sp);  //# slot 0 점유

            Assert.DoesNotThrow(() => _view.RemoveStatusIcon("notthere"));

            //# 기존 slow 슬롯 영향 없음.
            Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
            Assert.IsTrue(_iconRow.activeSelf);
        }

        //# 7. 아무것도 없는 상태에서 미등록 key 제거 — no-op, 컨테이너 비활성 유지.
        [Test]
        public void RemoveStatusIcon_빈상태_미등록key_noop()
        {
            Assert.DoesNotThrow(() => _view.RemoveStatusIcon("nothing"));
            Assert.IsFalse(_iconRow.activeSelf);
        }

        //# 8. ClearStatusIcons — 다중 추가 후 Clear → 전 슬롯 비활성 + 컨테이너 비활성 + 매핑 비움.
        //# 매핑 비움은 행동 검증: Clear 후 Add 가 slot 0 부터 다시 채워지면 _keyToSlot 가 비었다는 뜻.
        [Test]
        public void ClearStatusIcons_다중추가후_전부비활성_그리고매핑비움()
        {
            Sprite sp = MakeSprite();
            _view.AddStatusIcon("a", sp);
            _view.AddStatusIcon("b", sp);
            _view.AddStatusIcon("c", sp);

            _view.ClearStatusIcons();

            foreach (Image slot in _iconSlots)
            {
                Assert.IsFalse(slot.gameObject.activeSelf);
            }
            Assert.IsFalse(_iconRow.activeSelf);

            //# 매핑이 비었으면 — 같은 key("a") 도 다시 slot 0 부터 채워진다.
            _view.AddStatusIcon("a", sp);
            Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
        }

        //# 8. ClearStatusIcons 멱등 — 빈 상태에서 Clear 해도 예외 없음.
        [Test]
        public void ClearStatusIcons_빈상태_멱등_예외없음()
        {
            Assert.DoesNotThrow(() => _view.ClearStatusIcons());
            Assert.IsFalse(_iconRow.activeSelf);
        }

        //# 참고: 같은 key 중복(슬롯 1개 유지)은 HpBarViewTests.AddStatusIcon_같은key중복_슬롯1개만사용 가
        //# 이미 커버하므로 여기서 중복하지 않는다. #4 의 신규 회귀는 runner 측
        //# HeroAuraRunnerStatusIconEdgeTests.Attach_같은타입재부착_OnStatusShown_추가발행안됨_회귀 가 담당.
    }
}
