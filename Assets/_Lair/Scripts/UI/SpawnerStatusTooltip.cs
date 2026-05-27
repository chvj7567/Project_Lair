using System;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class SpawnerStatusTooltipArg : UIArg
    {
        //# 어느 셀의 툴팁인지 — 헤더 "Spawner #N" 표시 + ViewModel.Spawners[N] 조회 키.
        public int SpawnerIndex;
        public BattleViewModel ViewModel;
        //# 셀 RectTransform — 툴팁이 그 위에 floating 으로 배치.
        public RectTransform AnchorCell;
        //# 툴팁이 닫힐 때 패널에 알려 활성 테두리 원복. 인자는 닫힌 셀 인덱스 — Panel 이
        //# stale 콜백(다른 셀로 전환되어 이전 disposable 이 강제 해제된 경우) 을 self-ignore 한다.
        public Action<int> OnClosed;
        //# 강화 줄 base 스탯의 단일 진실 (Rule 03 — 인터페이스 주입 우선).
        public BalanceConfig Balance;
    }

    //# 셀 위 floating 툴팁 — 헤더 + 강화 줄 (또는 "적용된 강화 없음").
    //# CHMUI.ShowUI(EUI.SpawnerStatusTooltip, arg) 로 띄움.
    public class SpawnerStatusTooltip : UIBase
    {
        [SerializeField] private RectTransform _root;     //# 툴팁 본체 (가로 180px, padding 8, 위치 조정 대상)
        [SerializeField] private CHText _headerText;      //# "Spawner #0 — Wisp ×2"
        [SerializeField] private CHText _buffText;        //# "H 체력 ×1.5 (200 → 300)" 또는 "적용된 강화 없음"

        private SpawnerStatusTooltipArg _arg;
        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            _arg = arg as SpawnerStatusTooltipArg;
            if (_arg == null) return;
            _vm = _arg.ViewModel;

            //# 동일 셀이라도 매번 InitUI 시 재구성 — 카드 픽 누적으로 강화 줄이 갱신될 수 있음.
            RefreshContent();
            PositionAboveAnchor();

            //# VM 갱신 구독 — 툴팁 열려있는 동안 출력 종 / count / 강화 픽 변경 시 자동 갱신.
            if (_vm != null)
            {
                _vm.OnSpawnerSnapshotChanged += HandleSnapshotChanged;
                closeDisposable.Add(() =>
                {
                    if (_vm != null) _vm.OnSpawnerSnapshotChanged -= HandleSnapshotChanged;
                });
            }

            //# 패널이 활성 테두리 원복 가능하도록 Close 시 콜백.
            //# 닫히는 셀 인덱스를 캡처 — CHMUI.ShowUI 재호출 시 이전 disposable 이 동기적으로 Clear 되어
            //# 콜백이 발화되는데, 그 시점 _openCellIndex 가 이미 새 셀로 바뀌어 있을 수 있음.
            //# 인덱스를 전달해 Panel 이 stale 콜백을 self-ignore 한다 (advisor BLOCKER).
            if (_arg.OnClosed != null)
            {
                var onClosed = _arg.OnClosed;
                int closedIndex = _arg.SpawnerIndex;
                closeDisposable.Add(() => onClosed(closedIndex));
            }
        }

        private void HandleSnapshotChanged(int index)
        {
            if (_arg == null || index != _arg.SpawnerIndex) return;
            RefreshContent();
        }

        //# 툴팁 위치 — 셀 RectTransform 상단 + gap 8px (기획서 §2.5.2).
        //# Pivot (0.5, 0) 으로 하단 중앙이 셀 상단을 가리킴.
        //# 부모(_root.parent) 는 tooltip 의 full-stretch root → pivot (0.5, 0.5) 라 localInCanvas.y 가
        //# [-H/2, H/2] 범위. anchor 도 (0.5, 0.5) 로 두어 localInCanvas.y 그대로 anchoredPosition 으로 사용.
        private void PositionAboveAnchor()
        {
            if (_root == null || _arg == null || _arg.AnchorCell == null) return;

            //# 안전 — 캔버스 좌표계가 일치한다고 가정 (둘 다 BattleHud 의 동일 캔버스 하위).
            var canvasRt = _root.parent as RectTransform;
            if (canvasRt == null) return;

            //# 셀의 월드 위치를 캔버스 로컬로 변환해서 셀 상단 중앙을 구함.
            var anchor = _arg.AnchorCell;
            Vector3 cellTopWorld = anchor.TransformPoint(new Vector3(0f, anchor.rect.yMax, 0f));
            Vector3 localInCanvas = canvasRt.InverseTransformPoint(cellTopWorld);

            //# Pivot (0.5, 0) — 하단 중앙이 anchored 위치. Anchor 를 부모 중심(0.5, 0.5)으로 맞춰
            //# localInCanvas 좌표계와 일치시킨다 (advisor BLOCKER 1).
            _root.pivot = new Vector2(0.5f, 0f);
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);

            //# 화면 좌우 clamp — 툴팁 width 의 절반 만큼 안전 margin.
            float halfWidth = _root.rect.width * 0.5f;
            float canvasHalfWidth = canvasRt.rect.width * 0.5f;
            float safeMargin = 4f;
            float minX = -canvasHalfWidth + halfWidth + safeMargin;
            float maxX =  canvasHalfWidth - halfWidth - safeMargin;
            float clampedX = Mathf.Clamp(localInCanvas.x, minX, maxX);

            _root.anchoredPosition = new Vector2(clampedX, localInCanvas.y + 8f);
        }

        private void RefreshContent()
        {
            if (_arg == null || _vm == null) return;
            var spawners = _vm.Spawners;
            if (spawners == null || _arg.SpawnerIndex < 0 || _arg.SpawnerIndex >= spawners.Count) return;
            var snap = spawners[_arg.SpawnerIndex];
            if (snap == null) return;

            //# 헤더 — "Spawner #N — Wisp ×2" / count==1 이면 "×1" 생략 (기획서 §2.5.4 예시 "×2" 유지)
            //# 일관성 — 헤더는 항상 ×count 표시 (1픽 케이스에도 명시). 기획서 §2.5.4 형식 그대로.
            string speciesName = SpawnerStatusCell.SpeciesName(snap.CurrentType);
            if (_headerText != null)
                _headerText.SetText($"Spawner #{snap.Index} — {speciesName} ×{snap.OutputCount}");

            //# 강화 줄.
            if (_buffText == null) return;

            var buffs = snap.AppliedBuffs;
            if (buffs == null || buffs.Count == 0)
            {
                _buffText.SetText("적용된 강화 없음");
                _buffText.SetColor(new Color(0.612f, 0.639f, 0.686f, 1f));    //# #9CA3AF
                return;
            }

            //# 종 1 ↔ 카드 1 매핑 — 첫 buff 만 사용.
            var first = buffs[0];
            if (first == null || first.Source == null)
            {
                _buffText.SetText("적용된 강화 없음");
                return;
            }

            string line = FormatBuffLine(snap.CurrentType, first);
            _buffText.SetText(line);
            _buffText.SetColor(Color.white);
        }

        //# 스탯별 줄 포맷 (기획서 §2.5.5). Hp/Power/Range/MoveSpeed/Cooldown/SlowFactor.
        //# Base 5스탯 → BalanceConfig 단일 진실. SlowFactor → PlagueSlowOnHit.BaseSlowFactor 상수.
        private string FormatBuffLine(EMonster type, BattleViewModel.AppliedBuff buff)
        {
            //# 아이콘 글자 + 픽 배지 prefix.
            var letterInfo = SpawnerStatusCell.IconLetterFor(buff.Source.Id);
            string prefix = letterInfo.letter == ' ' ? "" : letterInfo.letter.ToString();
            string pickBadge = buff.PickCount >= 2 ? $" ×{buff.PickCount}" : "";

            //# BalanceConfig 에서 base 스탯 읽기 (Plague SlowFactor 제외). Arg 로 주입된 단일 진실.
            BalanceConfig balance = _arg != null ? _arg.Balance : null;
            BalanceConfig.CharacterStat baseStat = balance?.GetMonster(type);

            //# 스탯별 분기 (기획서 §2.5.5 표).
            switch (buff.Stat)
            {
                case EMonsterStatKind.Hp:
                {
                    int baseHp = baseStat != null ? baseStat.Hp : 0;
                    int result = Mathf.Max(1, Mathf.RoundToInt(baseHp * buff.AggregateMultiplier));
                    return $"{prefix}{pickBadge} 체력 ×{buff.AggregateMultiplier:0.##} ({baseHp} → {result})";
                }
                case EMonsterStatKind.Power:
                {
                    int basePower = baseStat != null ? baseStat.Power : 0;
                    int result = Mathf.Max(1, Mathf.RoundToInt(basePower * buff.AggregateMultiplier));
                    return $"{prefix}{pickBadge} 공격력 ×{buff.AggregateMultiplier:0.##} ({basePower} → {result})";
                }
                case EMonsterStatKind.Range:
                {
                    float baseRange = baseStat != null ? baseStat.Range : 0f;
                    float result = baseRange * buff.AggregateMultiplier;
                    return $"{prefix}{pickBadge} 사거리 ×{buff.AggregateMultiplier:0.##} ({baseRange:0.0} → {result:0.0})";
                }
                case EMonsterStatKind.MoveSpeed:
                {
                    float baseMs = baseStat != null ? baseStat.MoveSpeed : 0f;
                    float result = baseMs * buff.AggregateMultiplier;
                    return $"{prefix}{pickBadge} 이동속도 ×{buff.AggregateMultiplier:0.##} ({baseMs:0.0} → {result:0.0})";
                }
                case EMonsterStatKind.Cooldown:
                {
                    //# CooldownMul 0.7 = 공격속도 ×1.43 (역수). 절대값은 cd 단위로 표시.
                    float baseCd = baseStat != null ? baseStat.Cooldown : 0f;
                    float resultCd = Mathf.Max(0.05f, baseCd * buff.AggregateMultiplier);
                    float aspeed = buff.AggregateMultiplier > 0f ? 1f / buff.AggregateMultiplier : 0f;
                    return $"{prefix}{pickBadge} 공격속도 ×{aspeed:0.##} (cd {baseCd:0.0}s → {resultCd:0.0}s)";
                }
                case EMonsterStatKind.SlowFactor:
                {
                    //# Plague — BalanceConfig 미보유. const 상수에서 직접.
                    float baseSlow = PlagueSlowOnHit.BaseSlowFactor;
                    float result = baseSlow * buff.AggregateMultiplier;
                    return $"{prefix}{pickBadge} 둔화 효과 ({baseSlow:0.##} → {result:0.##}) — 강화";
                }
                default:
                    return $"{prefix}{pickBadge} (알 수 없는 스탯)";
            }
        }

    }
}
