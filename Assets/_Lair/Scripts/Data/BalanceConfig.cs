using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 캐릭터 스탯 + 전투 상수. 순수 C# 클래스 — JSON(StreamingAssets) 이 유일 정본.
    //# BattleController 가 CreateDefault() 로 코드 기본값을 만든 뒤 JSON 을 OverlayFromDto 로 덮는다.
    public class BalanceConfig
    {
        //# 한 캐릭터의 튜닝 가능한 스탯.
        public class CharacterStat
        {
            public int   Hp;
            public int   Power;
            public float Range;
            public float Cooldown;
            public float MoveSpeed;
        }

        //# EMonster 키 ↔ 스탯 매핑 행.
        public class MonsterStatRow
        {
            public EMonster Key;
            public CharacterStat Stat;
            //# 이 종을 출력하는 스포너의 base 스폰 주기(초). Stat 이 아닌 스포너 생산 설정 (기획서 §1).
            public float SpawnPeriod;
        }

        //# hero/monsters 는 기본값 없음(null) — JSON 이 채운다. JSON 부재 시 hero 는 CreateDefault 크래시넷,
        //# monsters 는 null → GetMonster null → BattleController 가 프리팹 기본 스탯 사용.
        private CharacterStat _hero;
        private MonsterStatRow[] _monsters;

        //# runDuration/thresholds 는 코드 기본값 fallback(JSON 부재 시 사용). JSON 오버레이가 있으면 그 값이 이김.
        private float _runDuration = 300f;
        private float[] _passiveThresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
        private float[] _activeThresholds =
            { 30f, 90f, 150f, 210f, 270f };

        public CharacterStat Hero => _hero;
        public float RunDuration => _runDuration;
        public float[] PassiveThresholds => _passiveThresholds;
        public float[] ActiveThresholds => _activeThresholds;

        //# JSON 없음/깨짐 시 크래시넷 — 유효 hero 만 채우고 monsters 는 비워 둔다(프리팹 기본 스탯 fallback).
        //# 진짜 밸런스는 StreamingAssets JSON 이 정본 — 여기에 전체 밸런스표를 박지 않는다(code-vs-JSON 이중소스 방지).
        public static BalanceConfig CreateDefault()
        {
            return new BalanceConfig
            {
                _hero = new CharacterStat { Hp = 1000, Power = 50, Range = 1.5f, Cooldown = 1f, MoveSpeed = 3f }
            };
        }

        //# EMonster 키로 스탯 행 조회. 미발견 시 null + 경고.
        public CharacterStat GetMonster(EMonster key)
        {
            if (_monsters != null)
            {
                foreach (MonsterStatRow row in _monsters)
                {
                    if (row != null && row.Key == key) return row.Stat;
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
                    if (row != null && row.Key == key) return row.SpawnPeriod;
                }
            }
            Debug.LogWarning($"[BalanceConfig] 스폰 주기 미발견: {key}");
            return 9f;
        }

        //# JSON DTO 를 이 인스턴스(복제본)에 오버레이. 있고·검증 통과한 값만 덮고, 없거나 불량이면 기존 SO 값 유지.
        //# 런타임 전용 — Editor 의 ApplyDto(SerializedObject)와 별개.
        public void OverlayFromDto(BalanceConfigDto dto)
        {
            if (dto == null)
                return;

            if (TryBuildStat(dto.Hero, out CharacterStat hero))
                _hero = hero;

            if (dto.Monsters != null)
            {
                foreach (MonsterStatRowDto row in dto.Monsters)
                {
                    if (row == null)
                        continue;
                    if (Enum.TryParse(row.Key, out EMonster key) == false)
                    {
                        Debug.LogWarning($"[BalanceConfig] EMonster 파싱 실패 — skip: {row.Key}");
                        continue;
                    }
                    OverlayMonster(key, row);
                }
            }

            if (dto.RunDuration > 0f)
                _runDuration = dto.RunDuration;
            if (dto.PassiveThresholds != null && dto.PassiveThresholds.Length > 0)
                _passiveThresholds = dto.PassiveThresholds;
            if (dto.ActiveThresholds != null && dto.ActiveThresholds.Length > 0)
                _activeThresholds = dto.ActiveThresholds;
        }

        //# dto 스탯이 유효(모든 값 > 0)하면 CharacterStat 로 빌드. 불량이면 false + 경고.
        private static bool TryBuildStat(CharacterStatDto dto, out CharacterStat stat)
        {
            stat = null;
            if (dto == null)
                return false;
            if (dto.Hp <= 0 || dto.Power <= 0 || dto.Range <= 0f || dto.Cooldown <= 0f || dto.MoveSpeed <= 0f)
            {
                Debug.LogWarning($"[BalanceConfig] 불량 스탯 — skip (hp={dto.Hp},power={dto.Power},range={dto.Range},cd={dto.Cooldown},spd={dto.MoveSpeed})");
                return false;
            }
            stat = new CharacterStat
            {
                Hp = dto.Hp, Power = dto.Power, Range = dto.Range, Cooldown = dto.Cooldown, MoveSpeed = dto.MoveSpeed
            };
            return true;
        }

        //# 기존 monster 행이 있으면 Stat/SpawnPeriod 를 유효할 때만 갱신.
        //# SO 에 없는 키는 스탯이 유효할 때만 새 행 추가 — 불량 스탯으로 제로행(HP 0 등)을 주입하지 않는다(손편집 방어).
        private void OverlayMonster(EMonster key, MonsterStatRowDto row)
        {
            MonsterStatRow target = null;
            if (_monsters != null)
            {
                foreach (MonsterStatRow r in _monsters)
                {
                    if (r != null && r.Key == key) { target = r; break; }
                }
            }

            bool statOk = TryBuildStat(row.Stat, out CharacterStat s);

            if (target == null)
            {
                //# 새 키: 유효 스탯이 있어야만 행 생성. 불량이면 아예 추가 안 함(제로행 방지).
                if (statOk == false)
                    return;
                //# SpawnPeriod 누락/0 이면 GetSpawnPeriod miss-fallback 과 동일한 9f — period-0 스포너 방지(손편집 방어).
                target = new MonsterStatRow { Key = key, Stat = s, SpawnPeriod = row.SpawnPeriod > 0f ? row.SpawnPeriod : 9f };
                List<MonsterStatRow> list = _monsters != null ? new List<MonsterStatRow>(_monsters) : new List<MonsterStatRow>();
                list.Add(target);
                _monsters = list.ToArray();
                return;
            }

            //# 기존 키: 유효할 때만 각각 갱신, 불량/누락은 기존값 유지.
            if (statOk)
                target.Stat = s;
            if (row.SpawnPeriod > 0f)
                target.SpawnPeriod = row.SpawnPeriod;
        }
    }
}
