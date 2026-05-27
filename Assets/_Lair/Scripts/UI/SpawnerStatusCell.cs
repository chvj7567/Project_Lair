using System;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 화면 하단 6셀 패널의 1셀 — 색칩·종명·×N·진행 바·강화 아이콘 row.
    //# Bind(snapshot, progress, onClick) 로 받아, 스냅샷은 이벤트 수신 시 교체하고
    //# Progress 는 매 프레임 ISpawnerProgress.Progress 폴링 (기획서 §4.4·§4.6).
    //# 클릭은 CHButton (Rule 11). 풀링은 CHMPool (Rule 12) — 패널이 Pop/Push.
    public class SpawnerStatusCell : MonoBehaviour
    {
        //# Cool 진행 바 색 (#60A5FA), Warm (#F97316), Background (#374151) — 기획서 §3.1.
        public static readonly Color CoolColor = new Color(0.376f, 0.647f, 0.980f, 1f);
        public static readonly Color WarmColor = new Color(0.976f, 0.451f, 0.086f, 1f);
        public static readonly Color BarBackgroundColor = new Color(0.216f, 0.255f, 0.318f, 1f);

        //# Threshold (기획서 §3.1). 0.70 경계는 Warm (>= threshold).
        public const float WarmThreshold = 0.7f;

        //# 셀 배경 활성 테두리 (#FBBF24 노랑) / 기본 없음.
        public static readonly Color ActiveBorderColor   = new Color(0.984f, 0.749f, 0.141f, 1f);
        public static readonly Color InactiveBorderColor = new Color(0f, 0f, 0f, 0f);

        //# ×N 노랑 (#FBBF24).
        public static readonly Color CountTextColor = new Color(0.984f, 0.749f, 0.141f, 1f);

        [SerializeField] private Image _border;          //# 활성 시 노란 테두리 표시용
        [SerializeField] private Image _colorChip;       //# 종 색칩 (정사각형)
        [SerializeField] private CHText _speciesText;    //# 종명 영문
        [SerializeField] private CHText _countText;      //# ×N (N≥2 일 때만 노출)
        [SerializeField] private Image _progressFill;    //# 진행 바 Fill (fillAmount)
        [SerializeField] private CHButton _button;       //# 셀 클릭 — Panel 콜백
        [SerializeField] private RectTransform _iconRow; //# 강화·생산 아이콘 row 컨테이너 (2 슬롯: 좌 Enhance / 우 Spawn)

        //# v1.0 — IconRow 2 슬롯 분리. 슬롯 1 (Enhance, x=12, 글자 H/D/S/R/M/P).
        [SerializeField] private Image _iconCircleEnhance;   //# Enhance 슬롯 원 (종 색 배경)
        [SerializeField] private CHText _iconLetterEnhance;  //# Enhance 슬롯 글자 (H/D/S/R/M/P)
        [SerializeField] private CHText _iconBadgeEnhance;   //# Enhance 슬롯 ×N 배지 (PickCount≥2)

        //# v1.0 — 슬롯 2 (Spawn, x=68, 글자 '+').
        [SerializeField] private Image _iconCircleSpawn;     //# Spawn 슬롯 원 (종 색 배경)
        [SerializeField] private CHText _iconLetterSpawn;    //# Spawn 슬롯 글자 ('+')
        [SerializeField] private CHText _iconBadgeSpawn;     //# Spawn 슬롯 ×N 배지 (PickCount≥2)

        //# 클릭 리스너 수명 관리 (BuildIconCell 선례 패턴).
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        //# 매 프레임 폴링 대상.
        private ISpawnerProgress _progressSource;
        //# 현재 표시 스냅샷 캐시 (이벤트 갱신 시 RebindSnapshot 만 호출).
        private BattleViewModel.SpawnerSnapshot _snapshot;

        //# 풀 재사용 시 리스너 누적 / 이전 상태 잔존 방지 (Rule 12).
        private void OnEnable()
        {
            _disposable.Clear();
            if (_countText != null) _countText.gameObject.SetActive(false);
            if (_iconRow != null) _iconRow.gameObject.SetActive(false);
            //# v1.0 — 2 슬롯 circle 도 비활성화 (badge/letter 는 circle 의 자식이라 자동 비활성).
            if (_iconCircleEnhance != null) _iconCircleEnhance.gameObject.SetActive(false);
            if (_iconCircleSpawn != null)   _iconCircleSpawn.gameObject.SetActive(false);
            if (_progressFill != null) _progressFill.fillAmount = 0f;
            //# 색칩 기본 노출 회복 — 직전 셀이 N≥2 였다면 숨겨진 채 풀로 반환됐을 수 있음 (Rule 12).
            if (_colorChip != null) _colorChip.gameObject.SetActive(true);
            SetActiveBorder(false);
        }

        //# Panel 이 셀 생성·바인딩 시 호출 — snapshot + progress + onClick 3 인자 (기획서 §4.6).
        //# onClick 인자: 현재 인덱스 (Panel 의 콜백이 토글 동작을 결정).
        public void Bind(BattleViewModel.SpawnerSnapshot snapshot, ISpawnerProgress progress, Action<int> onClick)
        {
            _progressSource = progress;
            RebindSnapshot(snapshot);

            if (_button != null && onClick != null)
            {
                int idx = snapshot != null ? snapshot.Index : -1;
                _button.OnClick(() => onClick(idx), _disposable);
            }
        }

        //# 같은 셀(같은 인덱스) 에서 스냅샷만 갱신 (Output type/count, AppliedBuffs 변경).
        public void RebindSnapshot(BattleViewModel.SpawnerSnapshot snapshot)
        {
            _snapshot = snapshot;
            if (snapshot == null) return;

            //# 색칩 — 종 색.
            if (_colorChip != null)
            {
                _colorChip.color = SpeciesColor(snapshot.CurrentType);
                //# 색칩 visibility — N≥2 일 때 숨겨 종명 가용 폭 확보 (기획서 §2.2.2 v0.5).
                //# 강화 아이콘 row 배경색 + 디스크 본체 틴트와의 3중 redundancy 로 색 정보 보전.
                _colorChip.gameObject.SetActive(snapshot.OutputCount < 2);
            }

            //# 종명 영문.
            if (_speciesText != null) _speciesText.SetText(SpeciesName(snapshot.CurrentType));

            //# ×N — N≥2 일 때만 노출.
            if (_countText != null)
            {
                bool showCount = snapshot.OutputCount >= 2;
                _countText.gameObject.SetActive(showCount);
                if (showCount)
                {
                    _countText.SetText($"×{snapshot.OutputCount}");
                    _countText.SetColor(CountTextColor);
                }
            }

            //# 강화 아이콘 row — 종 1 ↔ 카드 1 매핑이므로 distinct 아이콘은 0 또는 1 (§2.3 전제).
            RebindIconRow(snapshot);
        }

        //# v1.0 — AppliedBuffs 를 Source.Category 로 분기해 2 슬롯 (좌 Enhance / 우 Spawn) 각각에 바인딩.
        //# 종 1 ↔ Enhance 카드 1 + 종 1 ↔ Spawn 카드 1 (Hex 제외) 매핑이라 각 슬롯의 distinct 아이콘은 0 또는 1.
        //# 양 슬롯 모두 비활성이면 IconRow 자체를 숨김 (기존 정책 유지 — 본체 row 위치 고정).
        private void RebindIconRow(BattleViewModel.SpawnerSnapshot snapshot)
        {
            if (_iconRow == null) return;

            //# 슬롯별 매칭된 buff 찾기 (Category 분기).
            BattleViewModel.AppliedBuff enhanceBuff = null;
            BattleViewModel.AppliedBuff spawnBuff = null;

            var buffs = snapshot.AppliedBuffs;
            if (buffs != null)
            {
                for (int i = 0; i < buffs.Count; ++i)
                {
                    var b = buffs[i];
                    if (b == null || b.Source == null) continue;
                    if (b.Source.Category == ECardCategory.Enhance && enhanceBuff == null)
                        enhanceBuff = b;
                    else if (b.Source.Category == ECardCategory.Spawn && spawnBuff == null)
                        spawnBuff = b;
                }
            }

            //# 슬롯별 바인딩.
            bool enhanceShown = BindIconSlot(enhanceBuff, _iconCircleEnhance, _iconLetterEnhance, _iconBadgeEnhance);
            bool spawnShown   = BindIconSlot(spawnBuff,   _iconCircleSpawn,   _iconLetterSpawn,   _iconBadgeSpawn);

            //# 둘 다 비활성이면 row 숨김 — 기존 정책 유지.
            _iconRow.gameObject.SetActive(enhanceShown || spawnShown);
        }

        //# 한 슬롯 바인딩 — buff 가 유효 카드면 circle/letter/badge 세팅하고 true 반환, 아니면 비활성 + false.
        private static bool BindIconSlot(BattleViewModel.AppliedBuff buff, Image circle, CHText letter, CHText badge)
        {
            if (buff == null || buff.Source == null)
            {
                if (circle != null) circle.gameObject.SetActive(false);
                return false;
            }
            var info = IconLetterFor(buff.Source.Id);
            if (info.letter == ' ')
            {
                //# 매핑 외 카드(예: Hex 종의 Spawn 슬롯 — SpawnHex 부재). 슬롯 비활성.
                if (circle != null) circle.gameObject.SetActive(false);
                return false;
            }

            if (circle != null)
            {
                circle.gameObject.SetActive(true);
                circle.color = info.bgColor;
            }
            if (letter != null)
            {
                letter.SetText(info.letter.ToString());
                letter.SetColor(info.fgColor);
            }
            if (badge != null)
            {
                bool showBadge = buff.PickCount >= 2;
                badge.gameObject.SetActive(showBadge);
                if (showBadge) badge.SetText($"×{buff.PickCount}");
            }
            return true;
        }

        //# 매 프레임 Progress 폴링 — VM 이벤트 우회 (기획서 §4.3·§4.6).
        private void Update()
        {
            if (_progressSource == null || _progressFill == null) return;
            float p = _progressSource.Progress;
            _progressFill.fillAmount = p;
            _progressFill.color = p < WarmThreshold ? CoolColor : WarmColor;
        }

        //# Panel 이 호출 — 활성 셀(툴팁 표시 중) 노란 테두리 표시.
        public void SetActiveBorder(bool active)
        {
            if (_border == null) return;
            _border.color = active ? ActiveBorderColor : InactiveBorderColor;
        }

        //# 종 색상 매핑 (기획서 §2.4 · 컨셉 §11.4).
        public static Color SpeciesColor(EMonster type) => type switch
        {
            EMonster.Wisp    => new Color(0.133f, 0.773f, 0.369f, 1f),   //# #22C55E
            EMonster.Wraith  => new Color(0.420f, 0.447f, 0.502f, 1f),   //# #6B7280
            EMonster.Reaper  => new Color(0.937f, 0.267f, 0.267f, 1f),   //# #EF4444
            EMonster.Hex     => new Color(0.918f, 0.702f, 0.031f, 1f),   //# #EAB308
            EMonster.Plague  => new Color(0.659f, 0.333f, 0.969f, 1f),   //# #A855F7
            EMonster.Phantom => new Color(0.122f, 0.161f, 0.216f, 1f),   //# #1F2937
            _                => Color.white,
        };

        //# 종 영문 풀네임 (기획서 §2.2.3).
        public static string SpeciesName(EMonster type) => type switch
        {
            EMonster.Wisp    => "Wisp",
            EMonster.Wraith  => "Wraith",
            EMonster.Reaper  => "Reaper",
            EMonster.Hex     => "Hex",
            EMonster.Plague  => "Plague",
            EMonster.Phantom => "Phantom",
            _                => "?",
        };

        //# 카드 ID → 아이콘 글자·배경·글자색 매핑 (기획서 §2.3.3).
        //# Enhance 6: H/D/S/R/M/P. Spawn 5: '+' (종 색 배경). Hex 종은 SpawnHex 카드 부재 → 자연 fallback.
        public static (char letter, Color bgColor, Color fgColor) IconLetterFor(ECardId id) => id switch
        {
            //# Enhance 카드.
            ECardId.WispHpBoost            => ('H', new Color(0.133f, 0.773f, 0.369f, 1f), Color.black),
            ECardId.WraithDamageBoost      => ('D', new Color(0.420f, 0.447f, 0.502f, 1f), Color.black),
            ECardId.ReaperAtkSpeed         => ('S', new Color(0.937f, 0.267f, 0.267f, 1f), Color.black),
            ECardId.HexRangeBoost          => ('R', new Color(0.918f, 0.702f, 0.031f, 1f), Color.black),
            ECardId.PhantomMoveSpeedBoost  => ('M', new Color(0.122f, 0.161f, 0.216f, 1f), Color.white),
            ECardId.PlagueSlowBoost        => ('P', new Color(0.659f, 0.333f, 0.969f, 1f), Color.black),
            //# v1.0 — Spawn 카드. 글자 '+', 배경색은 종 색 (§2.3.3 v1.0).
            ECardId.SpawnWisps             => ('+', new Color(0.133f, 0.773f, 0.369f, 1f), Color.black),   //# Wisp 초록
            ECardId.SpawnWraith            => ('+', new Color(0.420f, 0.447f, 0.502f, 1f), Color.black),   //# Wraith 회색
            ECardId.SpawnReapers           => ('+', new Color(0.937f, 0.267f, 0.267f, 1f), Color.black),   //# Reaper 빨강
            ECardId.SpawnPlagues           => ('+', new Color(0.659f, 0.333f, 0.969f, 1f), Color.black),   //# Plague 보라
            ECardId.SpawnPhantoms          => ('+', new Color(0.122f, 0.161f, 0.216f, 1f), Color.white),   //# Phantom 검정
            _                              => (' ', Color.gray, Color.white),
        };
    }
}
