using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 도감 셀 — 해금(컬러+이름) / 미조우(실루엣) / 잠금 더미 (기획서 §6).
    //# 몬스터 셀은 강화 레벨을 4채널(발광 오버레이·아이콘 틴트·스케일·레벨 배지)로 표현 (기획서 §1~5).
    public class CodexCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;        //# 카드 일러스트 또는 몬스터 색칩
        [SerializeField] private CHText _nameText;
        //# 강화 표현 위젯 (프리팹 배선 Task 4). 미배선이어도 null 가드로 안전.
        [SerializeField] private CHText _levelBadge;        //# 우상단 "Lv N" 배지 (§5)
        [SerializeField] private Image _glowOverlay;        //# 아이콘 뒤 종족색 발광 아우라 (§3)
        [SerializeField] private RectTransform _iconRect;   //# 스케일 대상 = _icon 의 RectTransform (§4)

        //# 레벨→시각 매핑·4채널 적용은 공유 SoT EnhanceLevelVisual 로 이관 (기획서 §2 — 도감·상태 셀 drift 방지).

        private static readonly Color NormalBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color DummyBg = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        private static readonly Color SilhouetteColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        private static readonly Color DummyTextColor = new Color(0.612f, 0.639f, 0.686f, 1f);

        public void Bind(CodexCellData data)
        {
            if (data == null)
                return;

            if (_background != null)
            {
                _background.color = data.IsLockedDummy ? DummyBg : NormalBg;
            }

            bool showIcon = data.IsLockedDummy == false;
            if (_icon != null)
            {
                _icon.gameObject.SetActive(showIcon);
                if (showIcon)
                {
                    //# 카드 일러스트가 있으면 스프라이트, 없으면 몬스터 색칩(sprite=null). 색은 ApplyEnhancement 소유.
                    _icon.sprite = data.Icon;
                }
            }

            if (_nameText != null)
            {
                _nameText.SetText(data.DisplayName);
                _nameText.SetColor(data.Unlocked ? Color.white : DummyTextColor);
            }

            //# 강화 4채널 — 매 재사용마다 전부 재설정(풀 재사용 잔상 방지, §9).
            ApplyEnhancement(data);
        }

        //# 해금된 몬스터 셀만 강화 4채널 적용. 그 외(카드·더미·미해금)는 lv0 → "담백한 원본"(§6).
        //# 매핑·적용은 공유 SoT EnhanceLevelVisual.Apply — baseIconColor(쉬는 색)만 도감 규칙으로 계산해 전달.
        private void ApplyEnhancement(CodexCellData data)
        {
            bool enhanced = data.Species.HasValue && data.Unlocked;
            int level = enhanced ? data.EnhanceLevel : 0;
            EMonster species = enhanced ? data.Species.Value : default;
            EnhanceLevelVisual.Apply(level, species, _icon, _glowOverlay, _levelBadge, _iconRect, BaseIconColor(data));
        }

        //# lv0/미강화 아이콘의 쉬는 색 — 미조우=실루엣, 해금 카드=흰색(원본), 해금 색칩=종색(기획서 §6).
        private Color BaseIconColor(CodexCellData data)
        {
            if (data.Unlocked == false)
                return SilhouetteColor;
            return data.Icon != null ? Color.white : data.TintColor;
        }
    }
}
