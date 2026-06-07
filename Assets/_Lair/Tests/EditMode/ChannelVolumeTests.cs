using NUnit.Framework;
using ChvjUnityInfra;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 효과음 채널별 볼륨 세분화 (spec 2026-06-08) — CHMSound.SetChannelVolume/GetChannelVolume 계약 검증.
    //# SetChannelVolume/GetChannelVolume 은 _audioSourceArr 를 만지지 않으므로 Init 없이 동작한다(싱글톤 dict 만 사용).
    //# 실제 source.volume 적용(= 채널배수 × Effect × Master)은 async+Addressable+PlayOneShot 이라 EditMode 검증 불가
    //# → "산식 재현"(GetChannelVolume × EffectVolume × MasterVolume)으로 계산식만 Assert, 실제 적용은 PlayMode/수동 검증 영역.
    //#
    //# 격리: _channelVolume 은 CHMSound 싱글톤이 들고 가는 in-memory dict 라 테스트 간 누수된다(미설정 채널만 1f fallback).
    //#       각 테스트가 의존하는 채널을 SetUp 에서 production 기본값(1f)으로 강제 리셋하고 TearDown 에서 되돌려 누수를 차단.
    public class ChannelVolumeTests
    {
        //# 본 스위트가 건드리는 모든 채널 — SetUp/TearDown 에서 1f 로 정리해 다른 테스트로 leak 방지.
        private static readonly EAudio[] _touchedChannels =
        {
            EAudio.Slash01, EAudio.Slash02, EAudio.Stab,
            EAudio.P1Skill, EAudio.P2Skill, EAudio.P3Skill,
            EAudio.Hit, EAudio.AcquireSkill, EAudio.CardSelect,
            EAudio.Bgm,
        };

        [SetUp]
        public void SetUp()
        {
            ResetTouchedChannels();
        }

        [TearDown]
        public void TearDown()
        {
            ResetTouchedChannels();
        }

        private void ResetTouchedChannels()
        {
            CHMSound sound = CHMSound.Instance;
            foreach (EAudio channel in _touchedChannels)
            {
                //# 1f 는 production 의 미설정 채널 fallback 값 — 깨끗한 베이스라인.
                sound.SetChannelVolume(channel, 1f);
            }
        }

        //# ── 기존 스모크 (round-trip / fallback / clamp) ──────────────────────────

        [Test]
        public void 채널배수_설정후_조회하면_같은값_반환()
        {
            CHMSound sound = CHMSound.Instance;
            sound.SetChannelVolume(EAudio.Slash01, 0.5f);
            Assert.AreEqual(0.5f, sound.GetChannelVolume(EAudio.Slash01), 0.0001f);
        }

        [Test]
        public void 미설정_채널은_배수_1로_fallback()
        {
            //# 격리상 SetUp 이 Hit 를 1f 로 세팅하지만, fallback 값과 동일하므로 의미는 "1.0 기대".
            CHMSound sound = CHMSound.Instance;
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Hit), 0.0001f);
        }

        [Test]
        public void 채널배수는_0과1로_clamp()
        {
            CHMSound sound = CHMSound.Instance;
            sound.SetChannelVolume(EAudio.P1Skill, 2f);
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.P1Skill), 0.0001f);
            sound.SetChannelVolume(EAudio.P1Skill, -1f);
            Assert.AreEqual(0f, sound.GetChannelVolume(EAudio.P1Skill), 0.0001f);
        }

        [Test]
        public void 채널배수_clamp_경계값_0과1은_그대로_유지()
        {
            CHMSound sound = CHMSound.Instance;
            sound.SetChannelVolume(EAudio.Stab, 0f);
            Assert.AreEqual(0f, sound.GetChannelVolume(EAudio.Stab), 0.0001f);
            sound.SetChannelVolume(EAudio.Stab, 1f);
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Stab), 0.0001f);
        }

        //# ── 1. 곱셈 산식 직접 검증 (핵심) ────────────────────────────────────────
        //# Play() 효과음 분기: source.volume = channelMul × EffectVolume × Ratio(=MasterVolume).
        //# EditMode 에서 실제 PlayOneShot 볼륨까지는 검증 불가(async+Addressable) →
        //# public 표면(GetChannelVolume × EffectVolume × MasterVolume)으로 산식을 재현해 고정한다(회귀 박제).

        [Test]
        public void 최종효과음볼륨_산식_채널배수x이펙트x마스터로_재현된다()
        {
            CHMSound sound = CHMSound.Instance;
            sound.SetChannelVolume(EAudio.Slash01, 0.5f);

            //# 산식 재현 — production Play() 의 효과음 분기와 동일한 곱셈 구조.
            float expected = 0.5f * sound.EffectVolume * sound.MasterVolume;
            float reproduced = sound.GetChannelVolume(EAudio.Slash01) * sound.EffectVolume * sound.MasterVolume;

            Assert.AreEqual(expected, reproduced, 0.0001f);
        }

        [Test]
        public void 채널배수_0이면_산식상_최종볼륨도_0()
        {
            CHMSound sound = CHMSound.Instance;
            sound.SetChannelVolume(EAudio.P2Skill, 0f);

            float reproduced = sound.GetChannelVolume(EAudio.P2Skill) * sound.EffectVolume * sound.MasterVolume;

            Assert.AreEqual(0f, reproduced, 0.0001f);
        }

        [Test]
        public void Ratio는_MasterVolume의_별칭이다()
        {
            //# Play() 효과음 분기가 Ratio 를 곱하는데 Ratio==MasterVolume 임을 고정(별칭 회귀 방지).
            CHMSound sound = CHMSound.Instance;
            Assert.AreEqual(sound.MasterVolume, sound.Ratio, 0.0001f);
        }

        //# ── 2. 채널 독립성 ──────────────────────────────────────────────────────

        [Test]
        public void 한채널_배수변경은_다른채널_배수에_영향없다()
        {
            CHMSound sound = CHMSound.Instance;

            //# Slash01 만 변경.
            sound.SetChannelVolume(EAudio.Slash01, 0.2f);

            //# 나머지 효과음 채널은 SetUp 의 1f 베이스라인을 유지해야 함.
            Assert.AreEqual(0.2f, sound.GetChannelVolume(EAudio.Slash01), 0.0001f, "변경한 채널");
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Slash02), 0.0001f, "Slash02 독립");
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Stab), 0.0001f, "Stab 독립");
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.P1Skill), 0.0001f, "P1Skill 독립");
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.P2Skill), 0.0001f, "P2Skill 독립");
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.P3Skill), 0.0001f, "P3Skill 독립");
        }

        [Test]
        public void 여섯채널을_서로다른값으로_설정해도_각자_독립적으로_보존된다()
        {
            CHMSound sound = CHMSound.Instance;

            sound.SetChannelVolume(EAudio.Slash01, 0.10f);
            sound.SetChannelVolume(EAudio.Slash02, 0.20f);
            sound.SetChannelVolume(EAudio.Stab, 0.30f);
            sound.SetChannelVolume(EAudio.P1Skill, 0.40f);
            sound.SetChannelVolume(EAudio.P2Skill, 0.50f);
            sound.SetChannelVolume(EAudio.P3Skill, 0.60f);

            Assert.AreEqual(0.10f, sound.GetChannelVolume(EAudio.Slash01), 0.0001f);
            Assert.AreEqual(0.20f, sound.GetChannelVolume(EAudio.Slash02), 0.0001f);
            Assert.AreEqual(0.30f, sound.GetChannelVolume(EAudio.Stab), 0.0001f);
            Assert.AreEqual(0.40f, sound.GetChannelVolume(EAudio.P1Skill), 0.0001f);
            Assert.AreEqual(0.50f, sound.GetChannelVolume(EAudio.P2Skill), 0.0001f);
            Assert.AreEqual(0.60f, sound.GetChannelVolume(EAudio.P3Skill), 0.0001f);
        }

        //# ── 3. 미대상 회귀 (Hit/AcquireSkill/CardSelect 는 SetChannelVolume 미호출 → 1.0) ──
        //# AudioVolumeSettings.Apply() 는 이 3채널에 SetChannelVolume 을 호출하지 않으므로
        //# 배수 1.0 → 기존 Effect×Master 경로 불변. 6 대상채널을 바꿔도 미대상은 1.0 이어야 한다.

        [Test]
        public void 미대상채널_Hit는_대상채널_조정후에도_배수1_유지()
        {
            CHMSound sound = CHMSound.Instance;

            //# 6 대상 채널을 전부 0.3 으로 조정.
            sound.SetChannelVolume(EAudio.Slash01, 0.3f);
            sound.SetChannelVolume(EAudio.Slash02, 0.3f);
            sound.SetChannelVolume(EAudio.Stab, 0.3f);
            sound.SetChannelVolume(EAudio.P1Skill, 0.3f);
            sound.SetChannelVolume(EAudio.P2Skill, 0.3f);
            sound.SetChannelVolume(EAudio.P3Skill, 0.3f);

            //# 미대상 3채널은 영향 없이 1.0 (= 기존 Effect×Master 경로).
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Hit), 0.0001f);
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.AcquireSkill), 0.0001f);
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.CardSelect), 0.0001f);
        }

        [Test]
        public void 미대상채널은_산식상_기존_Effect곱Master와_동일하다()
        {
            CHMSound sound = CHMSound.Instance;

            //# 미대상 채널 배수 1.0 × Effect × Master == 기존 Effect × Master (회귀 안전 검증).
            float legacy = sound.EffectVolume * sound.MasterVolume;
            float reproduced = sound.GetChannelVolume(EAudio.CardSelect) * sound.EffectVolume * sound.MasterVolume;

            Assert.AreEqual(legacy, reproduced, 0.0001f);
        }

        //# ── 4. 재설정 덮어쓰기 ──────────────────────────────────────────────────

        [Test]
        public void 같은채널_두번설정하면_마지막값으로_갱신된다()
        {
            CHMSound sound = CHMSound.Instance;

            sound.SetChannelVolume(EAudio.P3Skill, 0.8f);
            Assert.AreEqual(0.8f, sound.GetChannelVolume(EAudio.P3Skill), 0.0001f);

            sound.SetChannelVolume(EAudio.P3Skill, 0.25f);
            Assert.AreEqual(0.25f, sound.GetChannelVolume(EAudio.P3Skill), 0.0001f);
        }

        [Test]
        public void 재설정시_clamp도_매번_적용된다()
        {
            CHMSound sound = CHMSound.Instance;

            sound.SetChannelVolume(EAudio.Slash02, 0.4f);
            Assert.AreEqual(0.4f, sound.GetChannelVolume(EAudio.Slash02), 0.0001f);

            //# 두 번째 호출이 범위를 벗어나면 다시 clamp.
            sound.SetChannelVolume(EAudio.Slash02, 5f);
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Slash02), 0.0001f);
        }

        //# ── 5. BGM 불변 성격 ────────────────────────────────────────────────────
        //# 설계상 Play() 의 BGM 분기는 source.volume = BgmVolume × Ratio 로 채널배수를 쓰지 않는다(CHMSound.cs §Play).
        //# 따라서 Bgm 키에 SetChannelVolume 을 호출해도 dict 에는 저장되지만 BGM 재생 볼륨에는 영향이 없다.
        //# EditMode 에선 실제 BGM source.volume 까지 검증 불가 → dict 저장/조회의 무해성만 확인(과도한 white-box 금지).

        [Test]
        public void Bgm키에_채널배수설정해도_GetChannelVolume은_동작한다_무해성()
        {
            CHMSound sound = CHMSound.Instance;

            //# dict 레벨에서는 다른 채널과 동일하게 저장/조회된다(예외 없음).
            sound.SetChannelVolume(EAudio.Bgm, 0.3f);
            Assert.AreEqual(0.3f, sound.GetChannelVolume(EAudio.Bgm), 0.0001f);

            //# 단, 이 배수는 BGM 재생에 쓰이지 않는다(설계). 실제 BGM source.volume 불변은 PlayMode/수동 검증 영역.
        }

        [Test]
        public void Bgm키_배수설정은_효과음채널_배수에_영향없다()
        {
            CHMSound sound = CHMSound.Instance;

            sound.SetChannelVolume(EAudio.Bgm, 0f);

            //# BGM 키를 만져도 효과음 채널 배수는 독립.
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Slash01), 0.0001f);
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.P1Skill), 0.0001f);
        }
    }
}
