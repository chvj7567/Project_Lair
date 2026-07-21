using NUnit.Framework;
using Lair.Meta;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# MetaProfile 스테이지 필드 엣지/회귀 — 직렬화 왕복 · 구버전 역직렬화 기본값 · CopyFrom 격리
    //# (hero-stage-variant plan Task 1, 기획서 §3). 기본값/2필드 복사는 MetaProfileStageTests 가 이미 커버.
    public class MetaProfileStageEdgeTests
    {
        [Test]
        public void JsonUtility_왕복해도_SelectedStage_ClearedStage가_보존된다()
        {
            MetaProfile src = new MetaProfile { SelectedStage = 4, ClearedStage = 3 };
            string json = JsonUtility.ToJson(src);
            MetaProfile dst = JsonUtility.FromJson<MetaProfile>(json);
            Assert.AreEqual(4, dst.SelectedStage);
            Assert.AreEqual(3, dst.ClearedStage);
        }

        [Test]
        public void 구버전_JSON은_스테이지_필드가_없어도_기본값1_0으로_채워진다()
        {
            //# Version 1 시절 세이브 문자열 재현 — 스테이지 필드가 아예 존재하지 않는다.
            //# JsonUtility 는 기본 생성자로 인스턴스화 후 존재 필드만 덮으므로 부재 필드는 C# 초기값(1/0) 유지.
            string oldJson = "{\"Version\":1,\"Souls\":500,\"SelectedHero\":\"Knight\"}";
            MetaProfile p = JsonUtility.FromJson<MetaProfile>(oldJson);

            Assert.AreEqual(1, p.SelectedStage);   //# 부재 → 초기값 1
            Assert.AreEqual(0, p.ClearedStage);    //# 부재 → 초기값 0
            Assert.AreEqual(500, p.Souls);         //# 존재 필드는 정상 복원
        }

        [Test]
        public void CopyFrom_은_진행필드를_복사하되_DisplayName은_로컬로_남긴다()
        {
            MetaProfile src = new MetaProfile
            {
                SelectedStage = 5,
                ClearedStage = 5,
                Souls = 1234,
                DisplayName = "서버영주",
            };
            MetaProfile dst = new MetaProfile { DisplayName = "로컬영주" };
            dst.CopyFrom(src);

            Assert.AreEqual(5, dst.SelectedStage);
            Assert.AreEqual(5, dst.ClearedStage);
            Assert.AreEqual(1234, dst.Souls);
            //# DisplayName 은 로컬 전용 — 클라우드 복원이 덮지 않는다(기획서 §1).
            Assert.AreEqual("로컬영주", dst.DisplayName);
        }

        [Test]
        public void CopyFrom_null이면_기존값을_유지하고_예외없다()
        {
            MetaProfile dst = new MetaProfile { SelectedStage = 3, ClearedStage = 2 };
            Assert.DoesNotThrow(() => dst.CopyFrom(null));
            Assert.AreEqual(3, dst.SelectedStage);
            Assert.AreEqual(2, dst.ClearedStage);
        }
    }
}
