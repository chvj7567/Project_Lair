using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 탑 사다리 셀 — 표시·입력만 (Rule 02 §6). 상태별 색만 고르고 문구·상태 판정은 TowerViewModel 이 확정한다.
    public class TowerFloorCell : MonoBehaviour
    {
        private static readonly Color PreviewBackground = new Color32(0x14, 0x19, 0x24, 0xFF);
        private static readonly Color ChallengeBackground = new Color32(0x3A, 0x4A, 0x7A, 0xFF);
        private static readonly Color ClearedBackground = new Color32(0x23, 0x2C, 0x4A, 0xFF);
        private static readonly Color PreviewNumber = new Color32(0x2A, 0x32, 0x45, 0xFF);
        private static readonly Color GoldNumber = new Color32(0xF7, 0xC6, 0x4A, 0xFF);
        private static readonly Color BossNumber = new Color32(0xE5, 0x48, 0x4D, 0xFF);
        private static readonly Color SubText = new Color32(0x8E, 0x98, 0xAD, 0xFF);
        private static readonly Color BoneText = new Color32(0xE8, 0xE1, 0xCF, 0xFF);

        [SerializeField] private Image _background;
        [SerializeField] private Image _numberBox;
        [SerializeField] private CHText _numberText;
        [SerializeField] private CHText _heroText;
        [SerializeField] private GameObject[] _ruleDots = new GameObject[0];   //# 규칙 1개당 하늘색 점 1개 (최대 2)
        [SerializeField] private CHText _tagText;
        [SerializeField] private GameObject _bossTag;
        [SerializeField] private CHText _bossTagText;
        [SerializeField] private GameObject _selectedMark;
        [SerializeField] private CHButton _button;

        private TowerFloorCellData _data;
        private bool _clickRegistered;

        //# 풀 재사용 — 이전 바인딩 잔존 방지 (Rule 03 §4).
        private void OnEnable()
        {
            _data = null;
        }

        public void Bind(TowerFloorCellData data)
        {
            if (data == null)
                return;
            _data = data;

            bool preview = data.State == ETowerFloorState.Preview;
            if (_background != null)
            {
                _background.color = preview ? PreviewBackground : data.State == ETowerFloorState.Challenge ? ChallengeBackground : ClearedBackground;
            }
            if (_numberBox != null)
            {
                _numberBox.color = data.IsBoss ? BossNumber : preview ? PreviewNumber : GoldNumber;
            }
            if (_numberText != null)
            {
                _numberText.SetText(data.NumberText);
            }
            if (_heroText != null)
            {
                _heroText.SetText(data.HeroName);
                _heroText.SetColor(preview ? SubText : BoneText);
            }
            for (int i = 0; i < _ruleDots.Length; ++i)
            {
                if (_ruleDots[i] != null)
                {
                    _ruleDots[i].SetActive(i < data.RuleCount);
                }
            }
            if (_tagText != null)
            {
                _tagText.SetText(data.TagText);
                _tagText.SetColor(data.State == ETowerFloorState.Challenge ? GoldNumber : SubText);
            }
            if (_bossTag != null)
            {
                _bossTag.SetActive(data.IsBoss);
            }
            if (_bossTagText != null && data.IsBoss)
            {
                _bossTagText.SetText(data.BossTagText);
            }
            if (_selectedMark != null)
            {
                _selectedMark.SetActive(data.IsSelected);
            }
            //# CHButton.OnClick 은 listener 누적 — 풀 재사용 셀은 1회만 등록하고 현재 _data 로 위임.
            if (_clickRegistered == false && _button != null)
            {
                _clickRegistered = true;
                _button.OnClick(HandleClick);
            }
        }

        //# 보이는 셀을 현재 데이터로 다시 그린다 (선택 표식 갱신용).
        public void Refresh()
        {
            if (_data != null)
            {
                Bind(_data);
            }
        }

        private void HandleClick()
        {
            if (_data != null)
            {
                _data.OnSelect?.Invoke(_data.Floor);
            }
        }
    }
}
