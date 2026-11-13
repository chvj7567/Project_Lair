using System;
using System.Collections.Generic;
using UnityEngine;
//# 커밋 ② 에서 중첩 MonsterStatRow 를 지우면 이 별칭도 지운다 (so-to-json-migration §6.1).
using MonsterStatJsonRow = Lair.Data.MonsterStatRow;

namespace Lair.Data
{
    //# 캐릭터 스탯 + 전투 상수의 단일 진실. BattleController 가 씬에서 참조해 런타임 적용.
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Lair/BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        //# 한 캐릭터의 튜닝 가능한 스탯.
        [Serializable]
        public class CharacterStat
        {
            public int   Hp;
            public int   Power;
            public float Range;
            public float Cooldown;
            public float MoveSpeed;
        }

        //# 영웅 앵커 — HP/Power 만 가진다 (chapter-system §1.2 이관 규칙). range/cooldown/moveSpeed 는 HeroArchetype.json 소관.
        //# 실제 영웅 스탯 = ScaleHeroStat(앵커, HeroArchetype.scale x Stage.multiplier).
        [Serializable]
        public class HeroAnchor
        {
            public int Hp;
            public int Power;
        }

        //# EMonster 키 ↔ 스탯 매핑 행.
        [Serializable]
        public class MonsterStatRow
        {
            public EMonster Key;
            public CharacterStat Stat;
            //# 이 종을 출력하는 스포너의 base 스폰 주기(초). Stat 이 아닌 스포너 생산 설정 (기획서 §1).
            public float SpawnPeriod;
        }

        [SerializeField] private HeroAnchor _hero;
        [SerializeField] private MonsterStatRow[] _monsters;

        [SerializeField] private float _runDuration = 300f;
        [SerializeField] private float[] _passiveThresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
        [SerializeField] private float[] _activeThresholds =
            { 30f, 90f, 150f, 210f, 270f };

        public HeroAnchor Hero => _hero;
        public float RunDuration => _runDuration;
        public float[] PassiveThresholds => _passiveThresholds;
        public float[] ActiveThresholds => _activeThresholds;

        //# EMonster 키로 스탯 행 조회 — 등급 몬스터는 기준 몬스터 행(monster-grades §6). 미발견 시 null + 경고.
        public CharacterStat GetMonster(EMonster key)
        {
            if (_monsters != null)
            {
                foreach (MonsterStatRow row in _monsters)
                {
                    if (row != null && row.Key == MonsterGrades.BaseOf(key)) return row.Stat;
                }
            }
            Debug.LogWarning($"[BalanceConfig] 몬스터 스탯 미발견: {key}");
            return null;
        }

        //# EMonster 키로 base 스폰 주기 조회. 미발견 시 fallback 9f + 경고 (GetMonster 패턴 일치).
        public float GetSpawnPeriod(EMonster key)
        {
            if (_monsters != null)
            {
                foreach (MonsterStatRow row in _monsters)
                {
                    if (row != null && row.Key == MonsterGrades.BaseOf(key)) return row.SpawnPeriod;
                }
            }
            Debug.LogWarning($"[BalanceConfig] 스폰 주기 미발견: {key}");
            return 9f;
        }

        //# BalanceConfig.json + MonsterStat.json 조립 결과 (so-to-json-migration §6.2 1번). null = 미로드.
        //# 커밋 ① 동안 게임은 씬의 SO 를 읽고, 이 값은 패리티 비교에만 쓴다.
        public static BalanceConfig Current { get; set; }

        //# JSON row → 런타임 모델. 위반(§10 SJ3~SJ5)은 전부 모아 ChapterDataException 으로 던진다.
        public static BalanceConfig Build(BalanceConfigRow config, MonsterStatJsonRow[] monsters)
        {
            if (config == null)
                throw new ChapterDataException("BalanceConfig.json 이 비어 있음");

            List<string> errors = new List<string>();
            ValidateConfig(config, errors);
            MonsterStatRow[] rows = BuildMonsters(monsters, errors);
            if (errors.Count > 0)
                throw new ChapterDataException("밸런스 데이터 검증 실패:\n - " + string.Join("\n - ", errors));

            //# 커밋 ② 에서 SO 상속을 떼면 new BalanceConfig() 로 바뀐다.
            BalanceConfig result = CreateInstance<BalanceConfig>();
            result._hero = new HeroAnchor { Hp = config.heroHp, Power = config.heroPower };
            result._monsters = rows;
            result._runDuration = config.runDuration;
            result._passiveThresholds = config.passiveThresholds;
            result._activeThresholds = config.activeThresholds;
            return result;
        }

        private static void ValidateConfig(BalanceConfigRow config, List<string> errors)
        {
            if (config.heroHp < 1 || config.heroPower < 1)
            {
                errors.Add("BalanceConfig heroHp·heroPower 는 1 이상이어야 함");
            }
            if (config.runDuration <= 0f)
            {
                errors.Add("BalanceConfig runDuration 은 0 보다 커야 함");
            }
            ValidateThresholds("passiveThresholds", config.passiveThresholds, 1f, false, errors);
            ValidateThresholds("activeThresholds", config.activeThresholds, config.runDuration, true, errors);
        }

        //# 0 초과 upper 미만 + 엄격 단조(ascending 이면 오름차순, 아니면 내림차순). 빈 배열은 필수 필드 누락으로 본다.
        private static void ValidateThresholds(string name, float[] values, float upper, bool ascending, List<string> errors)
        {
            if (values == null || values.Length == 0)
            {
                errors.Add($"BalanceConfig {name} 가 비어 있음");
                return;
            }
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] <= 0f || values[i] >= upper)
                {
                    errors.Add($"BalanceConfig {name}[{i}] = {values[i]} 는 0 초과 {upper} 미만이어야 함");
                }
                if (i > 0 && (ascending ? values[i] <= values[i - 1] : values[i] >= values[i - 1]))
                {
                    errors.Add($"BalanceConfig {name}[{i}] = {values[i]} 는 {(ascending ? "오름차순" : "내림차순")}이어야 함");
                }
            }
        }

        //# PK 중복 0 · monster 는 기준 6종 값명 · 기준 6종 전부 존재. 결과 순서는 기준 6종 순(JSON 행 순서 무관).
        private static MonsterStatRow[] BuildMonsters(MonsterStatJsonRow[] monsters, List<string> errors)
        {
            Dictionary<EMonster, MonsterStatRow> byKey = new Dictionary<EMonster, MonsterStatRow>();
            if (monsters != null)
            {
                foreach (MonsterStatJsonRow row in monsters)
                {
                    if (row == null || FormationDatabase.TryParseMonster(row.monster, out EMonster key) == false || MonsterGrades.IsBase(key) == false)
                    {
                        errors.Add($"MonsterStat.json monster '{row?.monster}' 는 EMonster 기준 6종 값명이어야 함");
                        continue;
                    }
                    if (byKey.ContainsKey(key))
                    {
                        errors.Add($"MonsterStat.json monster {key} 중복");
                        continue;
                    }
                    if (row.hp < 1)
                    {
                        errors.Add($"MonsterStat.json {key} hp 는 1 이상이어야 함");
                    }
                    if (row.range <= 0f || row.cooldown <= 0f || row.moveSpeed <= 0f || row.spawnPeriod <= 0f)
                    {
                        errors.Add($"MonsterStat.json {key} range·cooldown·moveSpeed·spawnPeriod 는 0 보다 커야 함");
                    }
                    byKey[key] = new MonsterStatRow
                    {
                        Key = key,
                        Stat = new CharacterStat
                        {
                            Hp = row.hp,
                            Power = row.power,
                            Range = row.range,
                            Cooldown = row.cooldown,
                            MoveSpeed = row.moveSpeed,
                        },
                        SpawnPeriod = row.spawnPeriod,
                    };
                }
            }

            List<MonsterStatRow> ordered = new List<MonsterStatRow>();
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                if (byKey.TryGetValue(monster, out MonsterStatRow built))
                {
                    ordered.Add(built);
                }
                else
                {
                    errors.Add($"MonsterStat.json 에 {monster} 행이 없음");
                }
            }
            return ordered.ToArray();
        }
    }
}
