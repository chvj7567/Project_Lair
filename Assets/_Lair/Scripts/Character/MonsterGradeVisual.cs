using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 등급 몬스터(2~5등급 Prefab Variant) 연출 루트 파사드(Rule 02 §10, monster-grades §2·§9) — 크기·몸 외곽 후광·밝기 맥동·바닥 오라·불꽃 기둥·후광.
    //# 수치는 전부 GradeFxDatabase(JSON) 가 정하고 BattleController 가 Configure 로 넘긴다. 1등급 프리팹에는 이 컴포넌트가 없다.
    //# 이펙트 자식 이름은 "Aura" 접두 → HitFlash·AttackJuice 번쩍임 제외(기존 규칙 재사용).
    public class MonsterGradeVisual : MonoBehaviour, IMonsterGradeVisual
    {
        [SerializeField] private float _cellWorld = 1f;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _sigil;
        [SerializeField] private GradeFxSheet _aura;
        [SerializeField] private GradeFxSheet[] _tornados;
        [SerializeField] private GradeFxSheet _sunburst;

        //# 시안 world 길이 환산 — 프레임 px x 시트 스케일 ÷ 160 x cellWorld, 스프라이트 PPU 100 기준(monster-grades §2.1).
        private const float FxPixelsPerUnit = 100f;
        private const float DesignCell = 160f;
        private const float DefaultSpritePpu = 100f;

        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
        private static readonly int GlowInnerId = Shader.PropertyToID("_GlowInner");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");

        private Vector3 _visualBaseScale = Vector3.one;
        private Vector3 _sigilBaseScale = Vector3.one;
        private Material _material;
        private IHealth _health;
        private Grade_PulseRow _pulse;
        private float _pulseTime;
        private bool _auraOn;
        private int _tornadoOn;
        private bool _sunburstOn;
        private bool _baseCaptured;

        private void Awake()
        {
            CaptureBase();
            LairCharacter character = GetComponent<LairCharacter>();
            _health = character != null ? character.Get<IHealth>() : null;
        }

        private void CaptureBase()
        {
            if (_baseCaptured)
                return;
            _baseCaptured = true;
            if (_visualRoot != null)
            {
                _visualBaseScale = _visualRoot.localScale;
            }
            if (_sigil != null)
            {
                _sigilBaseScale = _sigil.localScale;
            }
        }

        //# 풀 Pop — 연출 전부 끄고 기본값 복귀. 실제 값은 직후 BattleController 가 Configure 로 다시 넣는다(Rule 03 §4).
        private void OnEnable()
        {
            CaptureBase();
            ResetLook();
            if (_health != null) _health.OnDied += HandleDied;
        }

        //# 풀 Push — 이펙트 슬롯 반납·재생 정지.
        private void OnDisable()
        {
            if (_health != null) _health.OnDied -= HandleDied;
            ReleaseFx();
        }

        public void Configure(GradeFxSpec spec)
        {
            if (spec == null)
                return;
            ResetLook();

            if (_visualRoot != null)
            {
                _visualRoot.localScale = _visualBaseScale * spec.SizeMul;
            }
            if (_sigil != null)
            {
                _sigil.localScale = _sigilBaseScale * spec.SizeMul;
            }
            ApplyGlow(spec);
            ApplyFx(spec);
            _pulse = spec.Pulse;
            _pulseTime = Random.value * (spec.Pulse != null ? spec.Pulse.periodSec : 1f);
        }

        //# 5등급 맥동 — t = (경과 mod 주기) ÷ 주기, k = 0.5 - 0.5 cos(2πt)(0→1→0), 밝기·채도 같은 위상(§2.4).
        private void Update()
        {
            if (_pulse == null || _material == null)
                return;
            _pulseTime += Time.deltaTime;
            float t = (_pulseTime % _pulse.periodSec) / _pulse.periodSec;
            float k = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t);
            _material.SetFloat(BrightnessId, Mathf.Lerp(_pulse.brightnessMin, _pulse.brightnessMax, k));
            _material.SetFloat(SaturationId, Mathf.Lerp(_pulse.saturationMin, _pulse.saturationMax, k));
        }

        //# 몸 외곽 후광 — 폭(월드) = widthCell x cellWorld, 셰이더는 텍스처 픽셀 단위라 스프라이트 PPU·sizeMul 로 환산.
        private void ApplyGlow(GradeFxSpec spec)
        {
            if (_body == null || spec.HasGlow == false)
                return;
            _material = _body.material;
            float ppu = _body.sprite != null ? _body.sprite.pixelsPerUnit : DefaultSpritePpu;
            float texels = spec.GlowWidthCell * _cellWorld * ppu / Mathf.Max(0.01f, spec.SizeMul);
            _material.SetColor(GlowColorId, spec.GlowColor);
            _material.SetFloat(GlowWidthId, texels);
            _material.SetFloat(GlowInnerId, 0.5f);
        }

        private void ApplyFx(GradeFxSpec spec)
        {
            if (spec.HasAura && _aura != null && GradeFxBudget.TryAcquireAura())
            {
                _auraOn = true;
                _aura.Place(new Vector3(0f, spec.AuraOffsetY * _cellWorld, 0f), FxScale(spec.AuraScale));
                _aura.Play(spec.AuraOpacity, 0);
            }
            int tornadoSlot = 0;
            foreach (Grade_FxRow row in spec.Fx)
            {
                if (row.kind == GradeFxDatabase.KindTornado)
                {
                    if (_tornados == null || tornadoSlot >= _tornados.Length || _tornados[tornadoSlot] == null)
                        continue;
                    GradeFxSheet sheet = _tornados[tornadoSlot];
                    tornadoSlot++;
                    if (GradeFxBudget.TryAcquireTornado() == false)
                        continue;
                    _tornadoOn++;
                    sheet.Place(new Vector3(row.offsetX * _cellWorld, row.offsetY * _cellWorld, 0f), FxScale(row.scale));
                    sheet.Play(row.opacity, row.frameOffset);
                }
                else if (_sunburst != null && GradeFxBudget.TryAcquireSunburst())
                {
                    _sunburstOn = true;
                    _sunburst.Place(new Vector3(row.offsetX * _cellWorld, row.offsetY * _cellWorld, 0f), FxScale(row.scale));
                    _sunburst.Play(row.opacity, row.frameOffset);
                }
            }
        }

        private float FxScale(float sheetScale) => sheetScale * _cellWorld * FxPixelsPerUnit / DesignCell;

        //# 사망 순간 — 이펙트 즉시 숨김, 맥동 정지(밝기·채도 1.0). 몸 후광은 몸 알파를 따라 자연히 사라진다(셰이더가 몸 알파 기준).
        private void HandleDied()
        {
            ReleaseFx();
            _pulse = null;
            if (_material != null)
            {
                _material.SetFloat(BrightnessId, 1f);
                _material.SetFloat(SaturationId, 1f);
            }
        }

        private void ResetLook()
        {
            ReleaseFx();
            _pulse = null;
            if (_visualRoot != null) _visualRoot.localScale = _visualBaseScale;
            if (_sigil != null) _sigil.localScale = _sigilBaseScale;
            if (_material != null)
            {
                _material.SetFloat(GlowWidthId, 0f);
                _material.SetFloat(BrightnessId, 1f);
                _material.SetFloat(SaturationId, 1f);
            }
        }

        private void ReleaseFx()
        {
            if (_auraOn)
            {
                GradeFxBudget.ReleaseAura();
                _auraOn = false;
            }
            for (; _tornadoOn > 0; _tornadoOn--)
            {
                GradeFxBudget.ReleaseTornado();
            }
            if (_sunburstOn)
            {
                GradeFxBudget.ReleaseSunburst();
                _sunburstOn = false;
            }
            if (_aura != null) _aura.Stop();
            if (_sunburst != null) _sunburst.Stop();
            if (_tornados != null)
            {
                foreach (GradeFxSheet sheet in _tornados)
                {
                    if (sheet != null) sheet.Stop();
                }
            }
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public bool AuraOnForTest => _auraOn;
        public int TornadoOnForTest => _tornadoOn;
#endif
    }
}
