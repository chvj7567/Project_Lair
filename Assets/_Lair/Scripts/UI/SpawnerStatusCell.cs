using System;
using System.Collections.Generic;
using System.Globalization;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 화면 하단 6셀 패널의 1셀 — 색칩·종명·×N·진행 바·강화 아이콘 row.
    //# Bind(snapshot, progress, onClick) 로 받아, 스냅샷은 이벤트 수신 시 교체하고
    public class SpawnerStatusCell : MonoBehaviour
    {
        //# Cool 진행 바 색 (#60A5FA), Warm (#F97316), Background (#374151) — 기획서 §3.1.
        public static readonly Color CoolColor = new Color(0.376f, 0.647f, 0.980f, 1f);
        public static readonly Color WarmColor = new Color(0.976f, 0.451f, 0.086f, 1f);
        public static readonly Color BarBackgroundColor = new Color(0.216f, 0.255f, 0.318f, 1f);

        //# Threshold (기획서 §3.1). 0.70 경계는 Warm (>= threshold).
        public const float WarmThreshold = 0.7f;

        //# 셀 배경 테두리 기본 색 — 투명 (종색은 RebindSnapshot 에서 설정).
        public static readonly Color InactiveBorderColor = new Color(0f, 0f, 0f, 0f);

        //# ×N 노랑 (#FBBF24).
        public static readonly Color CountTextColor = new Color(0.984f, 0.749f, 0.141f, 1f);

        [SerializeField] private Image _border;          //# 셀 테두리 — 종 대표색 프레임 (종색 적용)
        [SerializeField] private Image _colorChip;       //# 종 색칩 (정사각형) — v1.1 중앙 아이콘으로 역할 이관, 숨김
        [SerializeField] private Image _icon;            //# 셀 중앙 몬스터 아이콘
        [SerializeField] private CHText _speciesText;    //# 종명 한글 (SpeciesVisual.SpeciesName SoT)
        [SerializeField] private CHText _countText;      //# ×N (N≥2 일 때만 노출)
        [SerializeField] private Image _progressFill;    //# 진행 바 Fill (fillAmount)
        [SerializeField] private CHText _periodText;     //# 다음 스폰까지 남은 초 (Ns)
        [SerializeField] private CHButton _button;       //# 셀 클릭 — Panel 콜백

        //# 종 → 중앙 아이콘 스프라이트. 인스펙터 직접 참조 (CardData._icon·시너지축 관례, Addressables 키 아님).
        [SerializeField] private Sprite _wispIcon;
        [SerializeField] private Sprite _wraithIcon;
        [SerializeField] private Sprite _reaperIcon;
        [SerializeField] private Sprite _hexIcon;
        [SerializeField] private Sprite _plagueIcon;
        [SerializeField] private Sprite _phantomIcon;

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
            if (_progressFill != null) _progressFill.fillAmount = 0f;
            if (_periodText != null) _periodText.SetText("");
            //# 중앙 아이콘 리셋 — 직전 셀 스프라이트 잔존 방지 (Rule 03 §4 풀 재사용).
            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.gameObject.SetActive(false);
            }
            //# 색칩은 v1.1 에서 중앙 아이콘에 역할 이관 — 항상 숨김 (Rule 03 §4 잔존 방지).
            if (_colorChip != null) _colorChip.gameObject.SetActive(false);
            //# 테두리 색은 RebindSnapshot 에서 종색으로 설정 — 기본 투명으로 초기화.
            if (_border != null) _border.color = InactiveBorderColor;
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

            //# 테두리 — 종 대표색 프레임 (v1.1). 색칩 대신 셀 외곽이 종색을 담당.
            if (_border != null) _border.color = SpeciesColor(snapshot.CurrentType);

            //# 색칩 — v1.1 중앙 아이콘으로 역할 이관, 숨김 유지.
            if (_colorChip != null) _colorChip.gameObject.SetActive(false);

            //# 중앙 아이콘 — 종 스프라이트. 누락 시 숨김 (테두리 색만으로 종 식별 fallback).
            if (_icon != null)
            {
                Sprite sprite = SpeciesSprite(snapshot.CurrentType);
                _icon.sprite = sprite;
                _icon.gameObject.SetActive(sprite != null);
            }

            //# 종명 한글 — SpeciesVisual 단일 SoT (인게임 표기 통일).
            if (_speciesText != null)
            {
                _speciesText.SetText(SpeciesVisual.SpeciesName(snapshot.CurrentType));
            }

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

        }

        //# 매 프레임 Progress 폴링 — VM 이벤트 우회 (기획서 §4.3·§4.6).
        private void Update()
        {
            if (_progressSource == null || _progressFill == null) return;
            float p = _progressSource.Progress;
            _progressFill.fillAmount = p;
            _progressFill.color = p < WarmThreshold ? CoolColor : WarmColor;
            if (_periodText != null)
            {
                //# 남은 초 소수점 첫째자리 (예: 2.5s). RemainingSeconds 는 Spawner 에서 0 클램프됨.
                //# 한국어 로캘에서 소수점이 콤마로 찍히지 않도록 InvariantCulture 고정.
                string remain = _progressSource.RemainingSeconds.ToString("F1", CultureInfo.InvariantCulture);
                _periodText.SetText($"{remain}s");
            }
        }

        //# 종 → 중앙 아이콘 스프라이트 매핑 (인스펙터 직접 참조). 미할당이면 null → 아이콘 숨김.
        private Sprite SpeciesSprite(EMonster type) => type switch
        {
            EMonster.Wisp    => _wispIcon,
            EMonster.Wraith  => _wraithIcon,
            EMonster.Reaper  => _reaperIcon,
            EMonster.Hex     => _hexIcon,
            EMonster.Plague  => _plagueIcon,
            EMonster.Phantom => _phantomIcon,
            _                => null,
        };

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
