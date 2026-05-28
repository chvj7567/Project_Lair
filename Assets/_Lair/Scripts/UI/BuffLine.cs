using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 툴팁 본문의 강화 줄 1개 — 아이콘 원 + 글자(H/D/S/R/M/P) + ×N 배지 + 본문 텍스트.
    //# CHPoolingScrollView<BuffLine, AppliedBuff> 의 TItem (Rule 11 v0.8).
    //# 기존 SpawnerStatusTooltip.FormatBuffLine 의 스탯별 분기 로직을 본 클래스로 이주 —
    //# 한 줄 한 줄을 BuffLine 자기책임 (기획서 §2.5.5 v0.8 "다중 자식 BuffLine 단정").
    public class BuffLine : MonoBehaviour
    {
        [SerializeField] private Image _iconCircle;   //# 종 6색 배경 원
        [SerializeField] private CHText _iconLetter;  //# H/D/S/R/M/P
        [SerializeField] private CHText _badge;       //# ×N (PickCount≥2 일 때만)
        [SerializeField] private CHText _bodyText;    //# "체력 ×1.5 (200 → 300)"

        //# ×N 배지 노랑 (#FBBF24).
        private static readonly Color BadgeColor = new Color(0.984f, 0.749f, 0.141f, 1f);

        //# CHPoolingScrollView 풀 재사용 시 잔존 상태 reset (Rule 12).
        private void OnEnable()
        {
            if (_badge != null) _badge.gameObject.SetActive(false);
        }

        //# 한 줄 바인딩 — 스탯별 분기는 본 메서드 안. balance 가 null 이면 base 값 0 으로 안전 표시.
        public void Bind(BattleViewModel.AppliedBuff buff, EMonster type, BalanceConfig balance)
        {
            if (buff == null || buff.Source == null)
            {
                if (_bodyText != null) _bodyText.SetText("");
                if (_iconLetter != null) _iconLetter.SetText("");
                if (_badge != null) _badge.gameObject.SetActive(false);
                return;
            }

            //# 아이콘 글자·배경 — SpawnerStatusCell 의 매핑 함수 재사용 (단일 진실).
            (char letter, Color bgColor, Color fgColor) letterInfo = SpawnerStatusCell.IconLetterFor(buff.Source.Id);
            if (_iconCircle != null) _iconCircle.color = letterInfo.bgColor;
            if (_iconLetter != null)
            {
                _iconLetter.SetText(letterInfo.letter == ' ' ? "" : letterInfo.letter.ToString());
                _iconLetter.SetColor(letterInfo.fgColor);
            }

            //# ×N 배지 — PickCount≥2 일 때만.
            if (_badge != null)
            {
                bool showBadge = buff.PickCount >= 2;
                _badge.gameObject.SetActive(showBadge);
                if (showBadge)
                {
                    _badge.SetText($"×{buff.PickCount}");
                    _badge.SetColor(BadgeColor);
                }
            }

            //# 본문 — 스탯별 포맷 (기획서 §2.5.5).
            if (_bodyText != null) _bodyText.SetText(FormatBody(buff, type, balance));
        }

        //# 스탯별 줄 포맷 — Hp/Power/Range/MoveSpeed/Cooldown/SlowFactor.
        //# v1.0 — Spawn 카테고리는 stat 필드 무관, 단일 포맷 "동시 출력 +{PickCount}" 로 먼저 분기 (§2.5.5 v1.0).
        //# Base 5스탯 → BalanceConfig 단일 진실. SlowFactor → PlagueSlowOnHit.BaseSlowFactor 상수.
        private static string FormatBody(BattleViewModel.AppliedBuff buff, EMonster type, BalanceConfig balance)
        {
            //# v1.0 — Spawn 카테고리는 stat 분기 전에 단일 줄 단정. Stat 필드 안 읽음.
            if (buff.Source != null && buff.Source.Category == ECardCategory.Spawn)
                return $"동시 출력 +{buff.PickCount}";

            BalanceConfig.CharacterStat baseStat = balance != null ? balance.GetMonster(type) : null;
            switch (buff.Stat)
            {
                case EMonsterStatKind.Hp:
                {
                    int baseHp = baseStat != null ? baseStat.Hp : 0;
                    int result = Mathf.Max(1, Mathf.RoundToInt(baseHp * buff.AggregateMultiplier));
                    return $"체력 ×{buff.AggregateMultiplier:0.##} ({baseHp} → {result})";
                }
                case EMonsterStatKind.Power:
                {
                    int basePower = baseStat != null ? baseStat.Power : 0;
                    int result = Mathf.Max(1, Mathf.RoundToInt(basePower * buff.AggregateMultiplier));
                    return $"공격력 ×{buff.AggregateMultiplier:0.##} ({basePower} → {result})";
                }
                case EMonsterStatKind.Range:
                {
                    float baseRange = baseStat != null ? baseStat.Range : 0f;
                    float result = baseRange * buff.AggregateMultiplier;
                    return $"사거리 ×{buff.AggregateMultiplier:0.##} ({baseRange:0.0} → {result:0.0})";
                }
                case EMonsterStatKind.MoveSpeed:
                {
                    float baseMs = baseStat != null ? baseStat.MoveSpeed : 0f;
                    float result = baseMs * buff.AggregateMultiplier;
                    return $"이동속도 ×{buff.AggregateMultiplier:0.##} ({baseMs:0.0} → {result:0.0})";
                }
                case EMonsterStatKind.Cooldown:
                {
                    //# CooldownMul 0.7 = 공격속도 ×1.43 (역수). 절대값은 cd 단위로 표시.
                    float baseCd = baseStat != null ? baseStat.Cooldown : 0f;
                    float resultCd = Mathf.Max(0.05f, baseCd * buff.AggregateMultiplier);
                    float aspeed = buff.AggregateMultiplier > 0f ? 1f / buff.AggregateMultiplier : 0f;
                    return $"공격속도 ×{aspeed:0.##} (cd {baseCd:0.0}s → {resultCd:0.0}s)";
                }
                case EMonsterStatKind.SlowFactor:
                {
                    //# Plague — BalanceConfig 미보유. const 상수에서 직접 (§2.5.5 BaseSlowFactor 상수 정책).
                    float baseSlow = PlagueSlowOnHit.BaseSlowFactor;
                    float result = baseSlow * buff.AggregateMultiplier;
                    return $"둔화 효과 ({baseSlow:0.##} → {result:0.##}) — 강화";
                }
                default:
                    return "(알 수 없는 스탯)";
            }
        }
    }
}
