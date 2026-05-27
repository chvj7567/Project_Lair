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
        [SerializeField] private RectTransform _iconRow; //# 강화 아이콘 row 컨테이너 (자식 = 아이콘 원 1개 최대)
        [SerializeField] private Image _iconCircle;      //# row 안 단일 강화 아이콘 원 (Phantom 외 흰색 글자 등 §2.3 매핑)
        [SerializeField] private CHText _iconLetter;     //# 아이콘 글자 (H/D/S/R/M/P)
        [SerializeField] private CHText _iconBadge;      //# 아이콘 ×N 배지 (PickCount≥2 일 때만)

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

        //# 강화 row — AppliedBuffs 가 비어 있으면 row 숨김, 있으면 첫 번째 buff 의 카드 ID 로 글자·색 결정.
        private void RebindIconRow(BattleViewModel.SpawnerSnapshot snapshot)
        {
            if (_iconRow == null) return;

            //# 종에 적용된 강화 카드가 1장 이상이면 표시.
            var buffs = snapshot.AppliedBuffs;
            if (buffs == null || buffs.Count == 0)
            {
                _iconRow.gameObject.SetActive(false);
                return;
            }

            //# 첫 번째 buff 만 — 종 1 ↔ 카드 1 매핑 전제.
            var first = buffs[0];
            if (first == null || first.Source == null)
            {
                _iconRow.gameObject.SetActive(false);
                return;
            }
            var letterInfo = IconLetterFor(first.Source.Id);
            if (letterInfo.letter == ' ')
            {
                //# 강화 카드가 아닌 다른 카드가 어찌어찌 들어왔으면 표시 안 함 (방어).
                _iconRow.gameObject.SetActive(false);
                return;
            }

            _iconRow.gameObject.SetActive(true);
            if (_iconCircle != null) _iconCircle.color = letterInfo.bgColor;
            if (_iconLetter != null)
            {
                _iconLetter.SetText(letterInfo.letter.ToString());
                _iconLetter.SetColor(letterInfo.fgColor);
            }
            if (_iconBadge != null)
            {
                bool showBadge = first.PickCount >= 2;
                _iconBadge.gameObject.SetActive(showBadge);
                if (showBadge) _iconBadge.SetText($"×{first.PickCount}");
            }
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

        //# 강화 카드 ID → 아이콘 글자·배경·글자색 매핑 (기획서 §2.3.3).
        public static (char letter, Color bgColor, Color fgColor) IconLetterFor(ECardId id) => id switch
        {
            ECardId.WispHpBoost            => ('H', new Color(0.133f, 0.773f, 0.369f, 1f), Color.black),
            ECardId.WraithDamageBoost      => ('D', new Color(0.420f, 0.447f, 0.502f, 1f), Color.black),
            ECardId.ReaperAtkSpeed         => ('S', new Color(0.937f, 0.267f, 0.267f, 1f), Color.black),
            ECardId.HexRangeBoost          => ('R', new Color(0.918f, 0.702f, 0.031f, 1f), Color.black),
            ECardId.PhantomMoveSpeedBoost  => ('M', new Color(0.122f, 0.161f, 0.216f, 1f), Color.white),
            ECardId.PlagueSlowBoost        => ('P', new Color(0.659f, 0.333f, 0.969f, 1f), Color.black),
            _                              => (' ', Color.gray, Color.white),
        };
    }
}
