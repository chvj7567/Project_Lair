using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# BalanceConfig.OverlayFromDto — 오버레이/검증 스킵/부분 dto 유지 검증.
    //# BalanceConfig 는 순수 C# 클래스 — new BalanceConfig() 로 격리 인스턴스 생성(SO/asset 미사용, teardown 불필요).
    public class BalanceConfigOverlayTests
    {
        private BalanceConfig MakeConfigWithHero(int hp, int power)
        {
            BalanceConfig c = new BalanceConfig();
            //# 테스트 전용 시드 — 런타임 세터로 hero 기본값 주입
            c.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = hp, Power = power, Range = 1.5f, Cooldown = 1f, MoveSpeed = 3f }
            });
            return c;
        }

        [Test]
        public void 유효_hero_dto는_hero스탯을_덮는다()
        {
            BalanceConfig c = MakeConfigWithHero(1000, 50);
            c.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 2000, Power = 80, Range = 2f, Cooldown = 0.8f, MoveSpeed = 4f }
            });
            Assert.AreEqual(2000, c.Hero.Hp);
            Assert.AreEqual(80, c.Hero.Power);
        }

        [Test]
        public void 불량값_hero_dto는_스킵되고_기존값_유지()
        {
            BalanceConfig c = MakeConfigWithHero(1000, 50);
            //# Hp<=0 불량 → hero 오버레이 스킵, 기존 1000 유지 (검증 스킵 경고 발생)
            LogAssert.Expect(LogType.Warning, new Regex("불량 스탯"));
            c.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 0, Power = 80, Range = 2f, Cooldown = 0.8f, MoveSpeed = 4f }
            });
            Assert.AreEqual(1000, c.Hero.Hp);
            Assert.AreEqual(50, c.Hero.Power);
        }

        [Test]
        public void hero가_null인_dto는_기존_hero를_유지()
        {
            BalanceConfig c = MakeConfigWithHero(1000, 50);
            c.OverlayFromDto(new BalanceConfigDto { Hero = null, RunDuration = 250f });
            Assert.AreEqual(1000, c.Hero.Hp);
            Assert.AreEqual(250f, c.RunDuration);
        }

        //# hero + Wisp + Wraith 시드한 격리 인스턴스. runDuration/thresholds 는 코드 기본값(300 / 9·5개).
        private BalanceConfig MakeSeededWithMonsters()
        {
            BalanceConfig c = new BalanceConfig();
            c.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 1000, Power = 50, Range = 1.5f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Wisp",   Stat = new CharacterStatDto { Hp = 200, Power = 5,  Range = 1f,   Cooldown = 1f, MoveSpeed = 1f },   SpawnPeriod = 9f },
                    new MonsterStatRowDto { Key = "Wraith", Stat = new CharacterStatDto { Hp = 500, Power = 10, Range = 1.3f, Cooldown = 1f, MoveSpeed = 0.8f }, SpawnPeriod = 20f },
                }
            });
            return c;
        }

        [Test]
        public void 몬스터_부분오버레이는_다른행을_건드리지않는다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            //# Wisp 만 갱신 — Wraith 는 무손실이어야 한다.
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Wisp", Stat = new CharacterStatDto { Hp = 250, Power = 6, Range = 1f, Cooldown = 1f, MoveSpeed = 1f }, SpawnPeriod = 8f },
                }
            });
            Assert.AreEqual(250, c.GetMonster(EMonster.Wisp).Hp);
            Assert.AreEqual(8f, c.GetSpawnPeriod(EMonster.Wisp));
            //# Wraith 는 시드값 그대로.
            Assert.AreEqual(500, c.GetMonster(EMonster.Wraith).Hp);
            Assert.AreEqual(20f, c.GetSpawnPeriod(EMonster.Wraith));
        }

        [Test]
        public void 불량몬스터스탯은_기존Stat유지하고_SpawnPeriod만_갱신()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            LogAssert.Expect(LogType.Warning, new Regex("불량 스탯"));
            //# Hp<=0 불량 → Stat 스킵(기존 200 유지), SpawnPeriod 는 유효(15) 이므로 갱신.
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Wisp", Stat = new CharacterStatDto { Hp = 0, Power = 6, Range = 1f, Cooldown = 1f, MoveSpeed = 1f }, SpawnPeriod = 15f },
                }
            });
            Assert.AreEqual(200, c.GetMonster(EMonster.Wisp).Hp);
            Assert.AreEqual(15f, c.GetSpawnPeriod(EMonster.Wisp));
        }

        [Test]
        public void 새몬스터키_유효스탯은_행을_추가한다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Reaper", Stat = new CharacterStatDto { Hp = 100, Power = 6, Range = 1f, Cooldown = 0.5f, MoveSpeed = 1.5f }, SpawnPeriod = 12f },
                }
            });
            BalanceConfig.CharacterStat reaper = c.GetMonster(EMonster.Reaper);
            Assert.IsNotNull(reaper);
            Assert.AreEqual(100, reaper.Hp);
            Assert.AreEqual(12f, c.GetSpawnPeriod(EMonster.Reaper));
        }

        [Test]
        public void 새몬스터키_불량스탯은_행을_추가하지않는다_제로행방지()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            //# 불량 스탯 경고 + 이후 GetMonster(Hex) 미발견 경고 두 개 발생.
            LogAssert.Expect(LogType.Warning, new Regex("불량 스탯"));
            LogAssert.Expect(LogType.Warning, new Regex("몬스터 스탯 미발견"));
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Hex", Stat = new CharacterStatDto { Hp = 0, Power = 0, Range = 0f, Cooldown = 0f, MoveSpeed = 0f }, SpawnPeriod = 5f },
                }
            });
            //# 제로행이 추가되지 않아야 한다.
            Assert.IsNull(c.GetMonster(EMonster.Hex));
        }

        [Test]
        public void 파싱불가_몬스터키는_경고후_스킵되고_기존행_유지()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            LogAssert.Expect(LogType.Warning, new Regex("EMonster 파싱 실패"));
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "NotAMonster", Stat = new CharacterStatDto { Hp = 999, Power = 9, Range = 1f, Cooldown = 1f, MoveSpeed = 1f }, SpawnPeriod = 3f },
                }
            });
            //# 기존 Wisp/Wraith 무손실.
            Assert.AreEqual(200, c.GetMonster(EMonster.Wisp).Hp);
            Assert.AreEqual(500, c.GetMonster(EMonster.Wraith).Hp);
        }

        //# EMonster 키는 대소문자 정확히 일치해야 한다(Enum.TryParse 대소문자 구분 — Rule 03 §2 이름일치).
        [Test]
        public void 소문자_몬스터키는_대소문자구분으로_스킵된다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            LogAssert.Expect(LogType.Warning, new Regex("EMonster 파싱 실패"));
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "wisp", Stat = new CharacterStatDto { Hp = 777, Power = 5, Range = 1f, Cooldown = 1f, MoveSpeed = 1f }, SpawnPeriod = 9f },
                }
            });
            //# "wisp"(소문자) 는 파싱 실패 → Wisp 시드값 유지.
            Assert.AreEqual(200, c.GetMonster(EMonster.Wisp).Hp);
        }

        [Test]
        public void 몬스터리스트의_null행은_건너뛰고_유효행은_적용된다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    null,
                    new MonsterStatRowDto { Key = "Reaper", Stat = new CharacterStatDto { Hp = 100, Power = 6, Range = 1f, Cooldown = 0.5f, MoveSpeed = 1.5f }, SpawnPeriod = 12f },
                }
            });
            Assert.IsNotNull(c.GetMonster(EMonster.Reaper));
            Assert.AreEqual(100, c.GetMonster(EMonster.Reaper).Hp);
        }

        [Test]
        public void ActiveThresholds_빈배열은_기존값을_유지한다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            //# 코드 기본값: active 5개 / passive 9개.
            c.OverlayFromDto(new BalanceConfigDto
            {
                ActiveThresholds = new float[0],
                PassiveThresholds = new float[0],
            });
            Assert.AreEqual(5, c.ActiveThresholds.Length);
            Assert.AreEqual(9, c.PassiveThresholds.Length);
        }

        [Test]
        public void Thresholds_null은_기존값을_유지한다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            c.OverlayFromDto(new BalanceConfigDto
            {
                ActiveThresholds = null,
                PassiveThresholds = null,
            });
            Assert.AreEqual(5, c.ActiveThresholds.Length);
            Assert.AreEqual(9, c.PassiveThresholds.Length);
        }

        [Test]
        public void runDuration_0이하는_기존값을_유지한다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            //# 기본 300 — 0 과 음수는 스킵되어 유지.
            c.OverlayFromDto(new BalanceConfigDto { RunDuration = 0f });
            Assert.AreEqual(300f, c.RunDuration);
            c.OverlayFromDto(new BalanceConfigDto { RunDuration = -5f });
            Assert.AreEqual(300f, c.RunDuration);
        }

        [Test]
        public void dto가_null이면_아무것도_바꾸지않고_예외없다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            Assert.DoesNotThrow(() => c.OverlayFromDto(null));
            Assert.AreEqual(1000, c.Hero.Hp);
            Assert.AreEqual(200, c.GetMonster(EMonster.Wisp).Hp);
        }

        //# 순수 클래스 격리 — 서로 다른 인스턴스는 오버레이가 독립적이다(BattleController 가 CreateDefault 로 매 판 새 인스턴스).
        [Test]
        public void 서로다른_인스턴스의_오버레이는_독립적이다()
        {
            BalanceConfig a = MakeSeededWithMonsters();
            BalanceConfig b = MakeSeededWithMonsters();
            b.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 9999, Power = 80, Range = 2f, Cooldown = 0.8f, MoveSpeed = 4f },
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Wisp", Stat = new CharacterStatDto { Hp = 250, Power = 6, Range = 1f, Cooldown = 1f, MoveSpeed = 1f }, SpawnPeriod = 8f },
                }
            });
            //# b 는 바뀌고,
            Assert.AreEqual(9999, b.Hero.Hp);
            Assert.AreEqual(250, b.GetMonster(EMonster.Wisp).Hp);
            //# a 는 불변.
            Assert.AreEqual(1000, a.Hero.Hp);
            Assert.AreEqual(200, a.GetMonster(EMonster.Wisp).Hp);
        }

        //# 손편집 방어(신규) — 새 몬스터 키를 spawnPeriod 0 으로 추가하면 유효 스탯이면 행은 생기되
        //# 주기는 period-0 스포너(폭주) 방지를 위해 9f 로 fallback 되어야 한다.
        [Test]
        public void 새몬스터키_spawnPeriod0은_9f로_fallback되고_스탯은_추가된다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Reaper", Stat = new CharacterStatDto { Hp = 100, Power = 6, Range = 1f, Cooldown = 0.5f, MoveSpeed = 1.5f }, SpawnPeriod = 0f },
                }
            });
            Assert.IsNotNull(c.GetMonster(EMonster.Reaper), "유효 스탯이면 새 행은 추가된다");
            Assert.AreEqual(9f, c.GetSpawnPeriod(EMonster.Reaper), "spawnPeriod 0 → period-0 스포너 방지 위해 9f fallback");
        }

        //# 손편집 방어(신규) — spawnPeriod 필드 자체 누락(DTO 기본 0f)도 동일하게 9f 로 fallback.
        [Test]
        public void 새몬스터키_spawnPeriod누락은_9f로_fallback된다()
        {
            BalanceConfig c = MakeSeededWithMonsters();
            //# SpawnPeriod 미지정 → DTO 기본값 0f (손편집 JSON 에서 필드 통째 누락 재현).
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Reaper", Stat = new CharacterStatDto { Hp = 100, Power = 6, Range = 1f, Cooldown = 0.5f, MoveSpeed = 1.5f } },
                }
            });
            Assert.AreEqual(9f, c.GetSpawnPeriod(EMonster.Reaper));
        }

        //# 대조 — 기존 키의 spawnPeriod 0 은 9f 로 덮지 않고 기존값을 유지한다(신규 키 fallback 과 비대칭).
        [Test]
        public void 기존키_spawnPeriod0은_9f아니라_기존값을_유지한다()
        {
            BalanceConfig c = MakeSeededWithMonsters();   //# Wraith 시드 20f
            c.OverlayFromDto(new BalanceConfigDto
            {
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto { Key = "Wraith", Stat = new CharacterStatDto { Hp = 500, Power = 10, Range = 1.3f, Cooldown = 1f, MoveSpeed = 0.8f }, SpawnPeriod = 0f },
                }
            });
            Assert.AreEqual(20f, c.GetSpawnPeriod(EMonster.Wraith), "기존 키 spawnPeriod 0 은 갱신 스킵 — 기존 20 유지(9f 아님)");
        }
    }
}
