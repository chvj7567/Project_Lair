using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 카드 효과 하나의 수치 묶음 — paramKey → 값. 필수 키 집합은 Build 가 검증하므로 Get 은 항상 값이 있다.
    public sealed class CardParams
    {
        private readonly Dictionary<string, float> _values;

        public CardParams(Dictionary<string, float> values)
        {
            _values = values;
        }

        public IReadOnlyDictionary<string, float> Values => _values;

        public float Get(string key) => _values[key];
    }

    //# Card 한 행 + 그 카드 효과의 수치 묶음 — 표시 문구는 Strings_Ko 로 푼 문자열이다.
    public sealed class CardEntry
    {
        public ECardId Id;
        public EBuildAxis Axis;
        public string DisplayName;
        public string Description;
        public ECardEffect Effect;
        public CardParams Params;
    }

    //# 카드 JSON 2종 검증 + 조회 (so-to-json-migration §6.2 6번). Unity 비의존 순수 클래스. 위반은 ChapterDataException 으로 전부 모아 던진다.
    public sealed class CardDatabase
    {
        //# 로드·검증 성공 시에만 채워진다. null 이면 카드 데이터 미로드 — 전투 진입 불가(§6.4).
        public static CardDatabase Current { get; set; }

        //# 효과별 필수 paramKey 집합 (§3.10) — 누락·초과 모두 오류. 값은 효과 클래스 생성자가 p.Get 으로 읽는 키(= 필드명에서 앞 '_' 제외)와 같다.
        private static readonly Dictionary<ECardEffect, string[]> RequiredParams = new Dictionary<ECardEffect, string[]>
        {
            { ECardEffect.WispHpBoost, new string[] { "hpMul" } },
            { ECardEffect.WraithDamageBoost, new string[] { "hpMul" } },
            { ECardEffect.ReaperAtkSpeed, new string[] { "cdMul" } },
            { ECardEffect.HexRangeBoost, new string[] { "rangeMul" } },
            { ECardEffect.PlagueToxinBoost, new string[] { "toxinMul" } },
            { ECardEffect.PhantomMoveSpeedBoost, new string[] { "speedMul" } },
            { ECardEffect.SpawnWisps, Array.Empty<string>() },
            { ECardEffect.SpawnWraith, Array.Empty<string>() },
            { ECardEffect.SpawnReapers, Array.Empty<string>() },
            { ECardEffect.SpawnPlagues, Array.Empty<string>() },
            { ECardEffect.SpawnPhantoms, Array.Empty<string>() },
            { ECardEffect.WispWraithPowerBoost, new string[] { "powerMul" } },
            { ECardEffect.ReaperHexPowerBoost, new string[] { "powerMul" } },
            { ECardEffect.HeroAttackDown, Array.Empty<string>() },
            { ECardEffect.Fear, new string[] { "duration" } },
            { ECardEffect.Bleed, new string[] { "ratio", "duration" } },
            { ECardEffect.Weaken, new string[] { "factor", "duration" } },
            { ECardEffect.Slow, new string[] { "heroFactor", "monsterMul", "duration" } },
            { ECardEffect.Frenzy, new string[] { "duration" } },
            { ECardEffect.FastBreeding, new string[] { "periodMul" } },
            { ECardEffect.BloodThirst, new string[] { "duration" } },
            { ECardEffect.IronWill, new string[] { "duration" } },
            { ECardEffect.TimeStop, new string[] { "duration" } },
            { ECardEffect.GuardianRage, new string[] { "duration" } },
            { ECardEffect.ToughHide, Array.Empty<string>() },
            { ECardEffect.MarkOfDeath, new string[] { "dmgTakenMul", "duration" } },
            { ECardEffect.SpawnerHaste, new string[] { "periodMul" } },
            { ECardEffect.LingeringCurse, new string[] { "durationBonusPerPick" } },
        };

        private readonly Dictionary<ECardId, CardEntry> _byId = new Dictionary<ECardId, CardEntry>();
        private readonly List<CardEntry> _ordered = new List<CardEntry>();

        private CardDatabase() { }

        //# ECardId 정수값 순 (= Card.json 행 순서).
        public IReadOnlyList<CardEntry> Cards => _ordered;

        public CardEntry GetCard(ECardId id)
        {
            return _byId.TryGetValue(id, out CardEntry entry) ? entry : null;
        }

        //# nameKeyExists·text 는 Strings_Ko 조회(Data 가 로더에 기대지 않게 주입). 풀 SO·Card_Species 와의 대조는 런타임이 아니라 EditMode 테스트(SJ5)가 맡는다.
        public static CardDatabase Build(CardDataSet data, Func<int, bool> nameKeyExists, Func<int, string> text)
        {
            if (data == null)
                throw new ChapterDataException("카드 데이터가 비어 있음");

            List<string> errors = new List<string>();
            CardDatabase db = new CardDatabase();
            db.LoadCards(data.Cards, nameKeyExists, text, errors);
            db.LoadParams(data.Params, errors);
            if (errors.Count > 0)
                throw new ChapterDataException("카드 데이터 검증 실패:\n - " + string.Join("\n - ", errors));
            return db;
        }

        //# Strings_Ko 에 id 0 이 실재해 존재 검사만으로는 누락(0 채움)을 못 잡는다 — 양수 조건을 함께 건다.
        private static bool KeyOk(int key, Func<int, bool> nameKeyExists) => key > 0 && nameKeyExists(key);

        //# PK cardId 중복 0 · 행 순서는 ECardId 정수값 오름차순 · axis·effectKind 는 값명 · nameKey·descKey 는 양수이며 Strings_Ko 에 존재.
        private void LoadCards(CardRow[] rows, Func<int, bool> nameKeyExists, Func<int, string> text, List<string> errors)
        {
            rows = rows ?? Array.Empty<CardRow>();
            if (rows.Length == 0)
            {
                errors.Add("Card.json 이 비어 있음");
            }

            int previous = -1;
            foreach (CardRow row in rows)
            {
                if (row == null || FormationDatabase.TryParseEnum(row.cardId, out ECardId id) == false)
                {
                    errors.Add($"Card.json cardId '{row?.cardId}' 가 ECardId 에 없음");
                    continue;
                }
                if (_byId.ContainsKey(id))
                {
                    errors.Add($"Card.json cardId {id} 중복");
                    continue;
                }
                if ((int)id <= previous)
                {
                    errors.Add($"Card.json 행 순서는 ECardId 정수값 오름차순이어야 함: {id}");
                }
                previous = (int)id;
                bool axisOk = FormationDatabase.TryParseEnum(row.axis, out EBuildAxis axis);
                bool effectOk = FormationDatabase.TryParseEnum(row.effectKind, out ECardEffect effect);
                if (axisOk == false)
                {
                    errors.Add($"Card[{id}].axis '{row.axis}' 가 EBuildAxis 에 없음");
                }
                if (effectOk == false)
                {
                    errors.Add($"Card[{id}].effectKind '{row.effectKind}' 가 ECardEffect 에 없음");
                }
                if (KeyOk(row.nameKey, nameKeyExists) == false || KeyOk(row.descKey, nameKeyExists) == false)
                {
                    errors.Add($"Card[{id}] nameKey {row.nameKey}·descKey {row.descKey} 가 Strings_Ko 에 없음");
                }
                CardEntry entry = new CardEntry
                {
                    Id = id,
                    Axis = axis,
                    DisplayName = text(row.nameKey) ?? string.Empty,
                    Description = text(row.descKey) ?? string.Empty,
                    Effect = effect,
                };
                _byId.Add(id, entry);
                _ordered.Add(entry);
            }
        }

        //# 복합 PK(cardId, paramKey) 중복 0 · cardId FK · value 는 0 초과(0 채움 방지) · 효과별 필수 paramKey 집합과 정확히 일치(파라미터 없는 효과는 행 없음).
        private void LoadParams(Card_ParamRow[] rows, List<string> errors)
        {
            Dictionary<ECardId, Dictionary<string, float>> byCard = new Dictionary<ECardId, Dictionary<string, float>>();
            foreach (Card_ParamRow row in rows ?? Array.Empty<Card_ParamRow>())
            {
                if (row == null || FormationDatabase.TryParseEnum(row.cardId, out ECardId id) == false || _byId.ContainsKey(id) == false)
                {
                    errors.Add($"Card_Param.json cardId '{row?.cardId}' 가 Card.json 에 없음");
                    continue;
                }
                if (string.IsNullOrEmpty(row.paramKey))
                {
                    errors.Add($"Card_Param.json {id} 의 paramKey 가 비어 있음");
                    continue;
                }
                if (byCard.TryGetValue(id, out Dictionary<string, float> values) == false)
                {
                    values = new Dictionary<string, float>();
                    byCard.Add(id, values);
                }
                if (values.ContainsKey(row.paramKey))
                {
                    errors.Add($"Card_Param.json 복합키 중복: {id}/{row.paramKey}");
                    continue;
                }
                if (row.value <= 0f || float.IsNaN(row.value))
                {
                    errors.Add($"Card_Param[{id}/{row.paramKey}].value {row.value} 는 0 보다 커야 함");
                }
                values.Add(row.paramKey, row.value);
            }

            foreach (CardEntry entry in _ordered)
            {
                if (RequiredParams.TryGetValue(entry.Effect, out string[] required) == false)
                {
                    errors.Add($"필수 paramKey 표에 {entry.Effect} 가 없음");
                    continue;
                }
                byCard.TryGetValue(entry.Id, out Dictionary<string, float> actual);
                actual = actual ?? new Dictionary<string, float>();
                foreach (string key in required)
                {
                    if (actual.ContainsKey(key) == false)
                    {
                        errors.Add($"Card_Param.json 에 {entry.Id}/{key} 가 없음");
                    }
                }
                foreach (string key in actual.Keys)
                {
                    if (Array.IndexOf(required, key) < 0)
                    {
                        errors.Add($"Card_Param.json {entry.Id}/{key} 는 {entry.Effect} 효과의 수치가 아님");
                    }
                }
                entry.Params = new CardParams(actual);
            }
        }
    }
}
