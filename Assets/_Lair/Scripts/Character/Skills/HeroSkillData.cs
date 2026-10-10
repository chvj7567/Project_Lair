using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 데이터의 추상 베이스. 서브클래스가 튜닝 필드 + behavior 를 캡슐화한다.
    //# 공유 에셋이므로 가변 상태는 보관 금지 — CreateRuntime() 이 만든 런타임이 보유.
    public abstract class HeroSkillData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        public string DisplayName => _displayName;

        //# 같은 프레임에 여러 스킬 조건이 참일 때 먼저 Tick 되는 순서 — 클수록 먼저. 기본 0(기존 스킬은 활성화 순서 유지).
        public virtual int CastPriority => 0;

        //# 활성화 시 1회 호출. 이 스킬의 가변 상태 런타임 생성.
        public abstract IHeroSkillRuntime CreateRuntime();
    }
}
