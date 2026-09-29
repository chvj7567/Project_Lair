using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 강화 오버레이(Enhance) — 몸 스프라이트와 같은 시트 인덱스를 표시(기획서 §6.3.6).
    //# 인덱스는 스프라이트명 접미 숫자로 매칭 — 몸·오버레이 시트가 같은 그리드로 슬라이스되면 N 이 곧 같은 칸.
    //# 프레임 순서는 루트(MonsterVisual2D)가 Tick 으로 매 LateUpdate 소유(Rule 02 §10).
    public class MonsterTierOverlay : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _overlayRenderer;
        [SerializeField] private Sprite[] _tier1Frames;
        [SerializeField] private Sprite[] _tier2Frames;
        [SerializeField] private Sprite[] _tier3Frames;
        //# 영웅 스테이지 5(Tier 4) 오버레이 프레임 — 몬스터는 미사용(null, hero-2d-conversion §6.4).
        [SerializeField] private Sprite[] _tier4Frames;

        private Dictionary<int, Sprite> _tier1Map;
        private Dictionary<int, Sprite> _tier2Map;
        private Dictionary<int, Sprite> _tier3Map;
        private Dictionary<int, Sprite> _tier4Map;
        private Dictionary<int, Sprite> _activeMap;
        private int _currentTier;

        private void Awake()
        {
            if (_overlayRenderer == null) _overlayRenderer = GetComponent<SpriteRenderer>();
            RebuildMaps();
        }

        //# 풀 재사용 — 티어 0(오버레이 off)으로 복귀(Rule 03 §4).
        private void OnEnable() => SetTier(0);

        //# 레벨→티어 매핑 결과(0~3)를 받아 활성 오버레이 시트를 고른다. 0 이면 오버레이 off.
        public void SetTier(int tier)
        {
            _currentTier = tier;
            if (tier == 1)
            {
                _activeMap = _tier1Map;
            }
            else if (tier == 2)
            {
                _activeMap = _tier2Map;
            }
            else if (tier == 3)
            {
                _activeMap = _tier3Map;
            }
            else if (tier == 4)
            {
                _activeMap = _tier4Map;
            }
            else
            {
                _activeMap = null;
            }

            if (_activeMap == null && _overlayRenderer != null)
            {
                _overlayRenderer.enabled = false;
            }
        }

        //# 루트(MonsterVisual2D)가 매 LateUpdate 몸 스프라이트·flipX 를 전달 — 프레임 인덱스 동기.
        public void Tick(Sprite bodySprite, bool flipX)
        {
            if (_overlayRenderer == null)
                return;
            if (_activeMap == null || bodySprite == null)
            {
                _overlayRenderer.enabled = false;
                return;
            }
            int index = ParseFrameIndex(bodySprite.name);
            if (index < 0 || _activeMap.TryGetValue(index, out Sprite overlaySprite) == false || overlaySprite == null)
            {
                _overlayRenderer.enabled = false;
                return;
            }
            _overlayRenderer.sprite = overlaySprite;
            _overlayRenderer.flipX = flipX;
            _overlayRenderer.enabled = true;
        }

        //# 시트 슬라이스 명명 "<파일명>_<N>" 의 끝 정수를 읽는다.
        private static int ParseFrameIndex(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return -1;
            int underscoreIndex = spriteName.LastIndexOf('_');
            if (underscoreIndex < 0 || underscoreIndex == spriteName.Length - 1)
                return -1;
            string suffix = spriteName.Substring(underscoreIndex + 1);
            if (int.TryParse(suffix, out int index) == false)
                return -1;
            return index;
        }

        private static Dictionary<int, Sprite> BuildMap(Sprite[] frames)
        {
            Dictionary<int, Sprite> map = new Dictionary<int, Sprite>();
            if (frames == null)
                return map;
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = frames[i];
                if (frame == null)
                    continue;
                int index = ParseFrameIndex(frame.name);
                if (index < 0)
                    continue;
                map[index] = frame;
            }
            return map;
        }

        private void RebuildMaps()
        {
            _tier1Map = BuildMap(_tier1Frames);
            _tier2Map = BuildMap(_tier2Frames);
            _tier3Map = BuildMap(_tier3Frames);
            _tier4Map = BuildMap(_tier4Frames);
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public int CurrentTierForTest => _currentTier;
        public void SetOverlayRendererForTest(SpriteRenderer renderer) => _overlayRenderer = renderer;

        public void SetFramesForTest(Sprite[] tier1, Sprite[] tier2, Sprite[] tier3)
        {
            _tier1Frames = tier1;
            _tier2Frames = tier2;
            _tier3Frames = tier3;
            RebuildMaps();
        }

        public void SetTier4FramesForTest(Sprite[] tier4)
        {
            _tier4Frames = tier4;
            RebuildMaps();
        }
#endif
    }
}
