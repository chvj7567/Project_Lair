using System;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 종족별 도감 이펙트 시트 프레임(인스펙터 직접 참조) — 오라(2~5등급)·_g4(불꽃 기둥, 4~5등급)·_g5(햇살 후광, 5등급).
    [Serializable]
    public class CodexRaceArt
    {
        public EMonster Race;
        public Sprite Body;          //# 풋내기(티어0) 대기 첫 프레임
        public Sprite[] Aura;
        public Sprite[] Pillar;
        public Sprite[] Halo;
    }

    //# 풀 목록 한 항목 — 등급 행 머리글(IsRowHeader) 또는 몬스터 칸. 목록 = 등급 수 x (티어 수 + 1), 열 수 = 티어 수 + 1.
    public class CodexMonsterCellData
    {
        public bool IsRowHeader;
        public string HeaderName;             //# 행 머리글 — 등급명(접두어)
        public int HeaderGrade;               //# 행 머리글 — 별 개수
        public CodexCellModel Model;          //# 칸일 때만
        public Sprite Body;                   //# 티어 시트 대기 첫 프레임 (null 이면 색칩)
        public CodexRaceArt Art;
        public Color RaceColor;
        public Color GradeColor;
        public CodexConfigRow Config;
        public Grade_CodexUnlockRow Unlock;
        public bool Selected;
        public float StaggerDelaySec;         //# 신규 칸 연출 시작 지연(칸 간 스태거)
        public string NewLabel;
        public Action<int> OnSelect;
        public Func<CodexCellModel, bool> ConsumeUnlock;   //# VM.ConsumeUnlock — 재생권 소비
    }

    //# 도감 매트릭스 항목 셀(풀 셀, monster-codex §2·§5.3·§6.1) — 행 머리글/몬스터 칸 겸용.
    //# 이펙트 애니 슬롯은 셀이 아니라 CodexMonsterPanel 의 배분기가 소유한다(풀 재사용 시 누수 없음). 셀은 SetFxLive 로 켜고 끄기만 한다.
    public class CodexMonsterCell : MonoBehaviour
    {
        [Header("행 머리글")]
        [SerializeField] private GameObject _headerRoot;
        [SerializeField] private CHText _headerText;
        [SerializeField] private Image[] _headerStars;

        [Header("몬스터 칸")]
        [SerializeField] private GameObject _cellRoot;
        [SerializeField] private CHButton _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;            //# 등급색 테두리 (잠금은 #232834)
        [SerializeField] private Image _selectedFrame;     //# 선택 금색 외곽선
        [SerializeField] private RectTransform _bodyRoot;  //# 크기 배율 대상 (피벗 50% 90%)
        [SerializeField] private Image _bodyGlow;          //# 몸 뒤 종족색 가산 틴트 복제 — 가산 머티리얼은 프리팹에서
        [SerializeField] private Image _body;
        [SerializeField] private Image _aura;
        [SerializeField] private Image[] _pillars;         //# 4등급 2개·5등급 4개(Grade_Fx Tornado 행 수) 사용
        [SerializeField] private Image _halo;
        [SerializeField] private CHText _nameText;
        [SerializeField] private GameObject _newBadge;
        [SerializeField] private CHText _newText;
        [SerializeField] private CodexUnlockFx _unlockFx;  //# 펄스·링·스파클 위젯 묶음

        private const float FxFps = 30f;
        private const float GlowAlpha = 0.55f;
        private const float BodyBasePx = 40f;

        private static readonly Vector3[] Corners = new Vector3[4];
        private static readonly Color LockedBorder = new Color32(0x23, 0x28, 0x34, 0xFF);
        private static readonly Color OpenBg = new Color32(0x1F, 0x29, 0x37, 0xF2);
        private static readonly Color LockedBg = new Color32(0x10, 0x15, 0x1F, 0xFF);
        private static readonly Color LockedName = new Color32(0x7D, 0x87, 0x9F, 0xFF);
        private static readonly Color Gold = new Color32(0xF7, 0xC6, 0x4A, 0xFF);

        private CodexMonsterCellData _data;
        private bool _clickWired;
        private bool _fxLive;
        private float _fxTime;
        private bool _unlockActive;
        private float _unlockTime;
        private float _unlockDelay;

        public CodexMonsterCellData Data => _data;
        public bool IsRowHeader => _data != null && _data.IsRowHeader;
        public int Index => _data != null && _data.Model != null ? _data.Model.Index : -1;
        public EMonster Race => _data != null && _data.Model != null ? _data.Model.Race : default;
        public int Grade => _data != null && _data.Model != null ? _data.Model.Grade : 0;
        public int Tier => _data != null && _data.Model != null ? _data.Model.Tier : 0;
        public bool IsFxCandidate => _data != null && _data.Model != null && _data.Model.Open && _data.Model.Grade > MonsterGrades.MinGrade;

        //# 풀 Pop/재사용 — 연출·이펙트 상태를 전부 초기화(Rule 03 §4).
        private void OnEnable()
        {
            ResetTransient();
        }

        private void OnDisable()
        {
            ResetTransient();
        }

        //# 셀 월드 사각형이 뷰포트와 겹치면 true — 스크롤 밖 칸은 이펙트 슬롯 후보에서 뺀다.
        public bool IsVisibleIn(Rect viewportWorld)
        {
            if (gameObject.activeInHierarchy == false)
                return false;
            RectTransform rt = transform as RectTransform;
            if (rt == null)
                return false;
            rt.GetWorldCorners(Corners);
            Rect cell = new Rect(Corners[0].x, Corners[0].y, Corners[2].x - Corners[0].x, Corners[2].y - Corners[0].y);
            return cell.Overlaps(viewportWorld);
        }

        public void Bind(CodexMonsterCellData data)
        {
            if (data == null)
                return;
            ResetTransient();
            _data = data;
            if (data.IsRowHeader)
            {
                BindHeader(data);
                return;
            }
            if (data.Model == null)
                return;
            SetRoots(header: false);
            BindCell(data);
        }

        //# 선택 표시만 갱신 — 재바인딩 없이(진행 중인 해금 연출을 끊지 않는다). 상태는 _data.Selected 가 진실.
        public void RefreshSelection()
        {
            if (_selectedFrame != null)
            {
                _selectedFrame.gameObject.SetActive(_data != null && _data.IsRowHeader == false && _data.Selected);
            }
        }

        //# 이펙트 레이어 애니 on/off — 배분기가 슬롯을 준 칸만 true. false 면 첫 프레임 정지.
        public void SetFxLive(bool live)
        {
            if (_fxLive == live)
                return;
            _fxLive = live;
            _fxTime = 0f;
            if (live == false)
            {
                ShowFxFrame(0);
            }
        }

        private void SetRoots(bool header)
        {
            if (_headerRoot != null)
            {
                _headerRoot.SetActive(header);
            }
            if (_cellRoot != null)
            {
                _cellRoot.SetActive(header == false);
            }
        }

        private void BindHeader(CodexMonsterCellData data)
        {
            SetRoots(header: true);
            if (_headerText != null)
            {
                _headerText.SetText(data.HeaderName ?? string.Empty);
                _headerText.SetColor(data.GradeColor);
            }
            if (_headerStars == null)
                return;
            for (int i = 0; i < _headerStars.Length; ++i)
            {
                if (_headerStars[i] == null)
                    continue;
                _headerStars[i].gameObject.SetActive(i < data.HeaderGrade);
                _headerStars[i].color = data.GradeColor;
            }
        }

        private void BindCell(CodexMonsterCellData data)
        {
            CodexCellModel model = data.Model;
            bool open = model.Open;

            //# 풀 셀은 재바인딩되므로 클릭 리스너는 한 번만 등록(HandleClick 이 현재 _data 를 읽는다).
            if (_button != null && _clickWired == false)
            {
                _clickWired = true;
                _button.OnClick(HandleClick);
            }
            if (_background != null)
            {
                _background.color = open ? OpenBg : LockedBg;
            }
            if (_border != null)
            {
                _border.color = open ? data.GradeColor : LockedBorder;
            }
            if (_selectedFrame != null)
            {
                _selectedFrame.color = Gold;
            }
            RefreshSelection();
            if (_nameText != null)
            {
                _nameText.SetText(model.Name);
                _nameText.SetColor(open ? Color.white : LockedName);
            }

            ApplyBody(data, open);
            ApplyStaticFx(data, open);

            bool badge = open && model.IsNew;
            if (_newBadge != null)
            {
                _newBadge.SetActive(badge);
            }
            if (badge && _newText != null)
            {
                _newText.SetText(data.NewLabel);
            }

            //# 재생권은 VM 이 한 번만 내준다 — 풀 재바인딩에서 다시 재생되지 않는다.
            bool play = open && data.Config != null && data.Unlock != null && data.ConsumeUnlock != null && data.ConsumeUnlock(model);
            if (play)
            {
                BeginUnlock(data);
            }
        }

        private void HandleClick()
        {
            if (_data != null && _data.IsRowHeader == false && _data.Model != null && _data.OnSelect != null)
            {
                _data.OnSelect(_data.Model.Index);
            }
        }

        private void ApplyBody(CodexMonsterCellData data, bool open)
        {
            CodexCellModel model = data.Model;
            float basePx = data.Config != null ? data.Config.cellBodyBasePx : BodyBasePx;
            float px = CodexRules.BodyPx(Mathf.RoundToInt(basePx), model.SizeMul);
            if (_bodyRoot != null)
            {
                _bodyRoot.sizeDelta = new Vector2(px, px);
            }
            if (_body != null)
            {
                _body.sprite = data.Body;
                _body.color = open ? Color.white : Color.black;   //# 잠금 = 검은 실루엣
            }
            if (_bodyGlow == null)
                return;
            bool glow = open && model.Grade > MonsterGrades.MinGrade && data.Body != null && data.Unlock != null;
            _bodyGlow.gameObject.SetActive(glow);
            if (glow == false)
                return;
            //# 발광 확장 = Grade_CodexUnlock.glowPx(반경 px) / 몸 크기 — 블러 불가 근사(복제 Image 확대).
            _bodyGlow.sprite = data.Body;
            Color color = data.RaceColor;
            color.a = GlowAlpha;
            _bodyGlow.color = color;
            float scale = 1f + data.Unlock.glowPx / Mathf.Max(1f, px);
            _bodyGlow.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        //# 해금 칸의 등급 이펙트 첫 프레임 — 1등급·잠금은 효과 없음. 오라/기둥/후광 유무는 GradeFxDatabase(몬스터 프리팹과 같은 값)에서 읽는다.
        private void ApplyStaticFx(CodexMonsterCellData data, bool open)
        {
            GradeFxSpec spec = open && data.Model.Grade > MonsterGrades.MinGrade && GradeFxDatabase.Current != null
                ? GradeFxDatabase.Current.SpecOf(data.Model.Race, data.Model.Grade)
                : null;
            bool fx = spec != null && data.Art != null;
            int pillars = 0;
            bool halo = false;
            if (fx)
            {
                foreach (Grade_FxRow row in spec.Fx)
                {
                    if (row.kind == GradeFxDatabase.KindTornado)
                    {
                        pillars++;
                    }
                    else
                    {
                        halo = true;
                    }
                }
            }
            SetActive(_aura, fx && spec.HasAura);
            SetActive(_halo, fx && halo);
            if (_pillars != null)
            {
                for (int i = 0; i < _pillars.Length; ++i)
                {
                    SetActive(_pillars[i], i < pillars);
                }
            }
            if (fx)
            {
                ShowFxFrame(0);
            }
        }

        private void ShowFxFrame(int frame)
        {
            if (_data == null || _data.Art == null)
                return;
            SetFrame(_aura, _data.Art.Aura, frame);
            SetFrame(_halo, _data.Art.Halo, frame);
            if (_pillars == null)
                return;
            for (int i = 0; i < _pillars.Length; ++i)
            {
                SetFrame(_pillars[i], _data.Art.Pillar, frame + i * 3);
            }
        }

        private static void SetFrame(Image image, Sprite[] frames, int frame)
        {
            if (image == null || frames == null || frames.Length == 0)
                return;
            image.sprite = frames[((frame % frames.Length) + frames.Length) % frames.Length];
        }

        private static void SetActive(Image image, bool active)
        {
            if (image != null)
            {
                image.gameObject.SetActive(active);
            }
        }

        private void BeginUnlock(CodexMonsterCellData data)
        {
            _unlockActive = true;
            _unlockTime = 0f;
            _unlockDelay = data.StaggerDelaySec;
            if (_body != null)
            {
                _body.color = Color.black;
            }
            SetFxAlpha(0f);
            if (_unlockFx != null)
            {
                CodexConfigRow cfg = data.Config;
                _unlockFx.Play(cfg, data.Unlock, data.GradeColor, _unlockDelay, cfg.cellPulseMax, cfg.cellRingMax, cfg.cellSparkMax, cfg.cellSparkRadiusPx);
            }
        }

        private void Update()
        {
            if (_fxLive)
            {
                _fxTime += Time.unscaledDeltaTime;
                ShowFxFrame(Mathf.FloorToInt(_fxTime * FxFps));
            }
            if (_unlockActive)
            {
                _unlockTime += Time.unscaledDeltaTime;
                TickUnlock(_unlockTime - _unlockDelay);
            }
        }

        //# t<0 은 스태거 대기. 몸 실루엣 → 컬러 페이드, 이펙트 레이어는 지연 뒤 서서히(펄스·링·스파클은 CodexUnlockFx).
        private void TickUnlock(float t)
        {
            CodexConfigRow cfg = _data.Config;
            if (t < 0f)
                return;
            float fade = cfg.unlockFadeSec > 0f ? Mathf.Clamp01(t / cfg.unlockFadeSec) : 1f;
            if (_body != null)
            {
                _body.color = Color.Lerp(Color.black, Color.white, fade);
            }
            float fxT = t - cfg.cellFxFadeDelaySec;
            float fxAlpha = cfg.cellFxFadeSec > 0f ? Mathf.Clamp01(fxT / cfg.cellFxFadeSec) : (fxT >= 0f ? 1f : 0f);
            SetFxAlpha(fxAlpha);
            if (fade >= 1f && fxAlpha >= 1f)
            {
                _unlockActive = false;
            }
        }

        private void SetFxAlpha(float alpha)
        {
            SetImageAlpha(_aura, alpha);
            SetImageAlpha(_halo, alpha);
            if (_pillars == null)
                return;
            foreach (Image pillar in _pillars)
            {
                SetImageAlpha(pillar, alpha);
            }
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
                return;
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private void ResetTransient()
        {
            _fxLive = false;
            _fxTime = 0f;
            _unlockActive = false;
            _unlockTime = 0f;
            SetFxAlpha(1f);
            if (_unlockFx != null)
            {
                _unlockFx.Stop();
            }
        }
    }
}
