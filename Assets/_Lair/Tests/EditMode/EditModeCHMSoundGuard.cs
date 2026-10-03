using System;
using ChvjUnityInfra;
using NUnit.Framework;

//# 네임스페이스 없음 — EditMode 어셈블리 전체에 한 번 적용된다.
//# CHSingleton.Instance 는 첫 접근에서 인스턴스를 만든 뒤 DontDestroyOnLoad 를 부르는데, EditMode 에선 그 호출이 InvalidOperationException 이다.
//# 인스턴스 할당은 예외 이전이라 이후 접근은 정상 → 실행 순서상 맨 처음 CHMSound.Instance 를 건드린 테스트만 실패하던 문제를 선접근으로 흡수한다.
[SetUpFixture]
public class EditModeCHMSoundGuard
{
    [OneTimeSetUp]
    public void CHMSound_싱글톤을_선접근해_첫_접근_예외를_흡수한다()
    {
        try
        {
            CHMSound sound = CHMSound.Instance;
        }
        catch (InvalidOperationException)
        {
        }
    }
}
