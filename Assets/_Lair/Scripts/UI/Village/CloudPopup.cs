using System;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Net;
using TMPro;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 03 §5 — UIArg 는 UIBase 와 같은 파일.
    //# 복원·표시명·충돌은 비즈니스 로직이라 VillageController 가 콜백으로 주입(Rule 02 §6 — View 는 표시·입력만).
    public class CloudPopupArg : UIArg
    {
        public bool IsConnected;          //# 연결됨/오프라인 표시
        public string DisplayName;        //# 현재 표시명(빈 값이면 기본명 표기)
        public bool ConflictPending;      //# 충돌 권유 영역 노출 여부(기획서 §3)
        public Action OnRestore;          //# "복원" 클릭 — VillageController 가 §2 플로우 수행
        //# 표시명 변경 — 서버 왕복(async). 컨트롤러가 권위 판정 후 결과 반환(Success 면 Name 에 서버 정규화 이름).
        public Func<string, Task<DisplayNameResult>> OnChangeName;
        public Action OnConflictRestore;  //# 충돌 권유 "클라우드로 복원" 클릭
        public Action OnConflictLater;    //# 충돌 권유 "나중에" 클릭
    }

    //# 클라우드 — 복원·표시명·충돌 권유 통합 팝업(기획서 §5). 표시·입력만(Rule 02 §6).
    public class CloudPopup : UIBase
    {
        //# 수동 복원은 당분간 닫아둔다 — 충돌 감지 시 권유(배지)로만 복원한다. true 로 바꾸면 되살아난다.
        private const bool ShowRestoreButton = false;

        [SerializeField] private CHText _connectionText;     //# 연결됨/오프라인
        [SerializeField] private CHText _displayNameText;    //# "표시명: 영주 #A3F9"
        [SerializeField] private CHButton _changeNameButton; //# [변경]
        [SerializeField] private TMP_InputField _nameInput;  //# 표시명 입력(Rule 03 §3 — 입력은 TMP_InputField)
        [SerializeField] private CHButton _nameConfirmButton;
        [SerializeField] private CHButton _nameCancelButton;
        [SerializeField] private GameObject _nameEditGroup;  //# 입력 영역(기본 숨김)
        [SerializeField] private CHButton _restoreButton;    //# 클라우드에서 복원
        [SerializeField] private GameObject _conflictGroup;  //# 충돌 권유 영역(배지 켜졌을 때만)
        [SerializeField] private CHText _conflictText;
        [SerializeField] private CHButton _conflictRestoreButton;
        [SerializeField] private CHButton _conflictLaterButton;
        [SerializeField] private GameObject _conflictDot;    //# 빨간 dot 배지

        //# 표시명 변경 서버 왕복 중 중복 클릭 차단(재진입 가드).
        private bool _isChangingName;

        public override void InitUI(UIArg arg)
        {
            CloudPopupArg cloudArg = arg as CloudPopupArg;
            if (cloudArg == null)
                return;

            if (_connectionText != null)
                _connectionText.SetText(cloudArg.IsConnected ? "연결됨" : "오프라인");

            RefreshDisplayName(cloudArg.DisplayName);
            SetNameEditActive(false);

            if (_changeNameButton != null)
                _changeNameButton.OnClick(() => SetNameEditActive(true), closeDisposable);

            if (_nameCancelButton != null)
                _nameCancelButton.OnClick(() => SetNameEditActive(false), closeDisposable);

            if (_nameConfirmButton != null)
            {
                Func<string, Task<DisplayNameResult>> onChange = cloudArg.OnChangeName;
                _nameConfirmButton.OnClick(() => OnClickConfirmName(onChange), closeDisposable);
            }

            if (_restoreButton != null)
            {
                Action onRestore = cloudArg.OnRestore;
                _restoreButton.gameObject.SetActive(ShowRestoreButton);
                _restoreButton.OnClick(() => onRestore?.Invoke(), closeDisposable);
            }

            SetupConflict(cloudArg);
        }

        //# 표시명 변경 확정 클릭 — 서버 왕복(async). 입력·분기만 담당(검증·토스트·상태변경은 컨트롤러).
        //# Success 면 서버 확정 이름으로 라벨 갱신 + 편집창 닫기, 그 외(중복/유효하지않음/오프라인)는 편집창 유지.
        private async void OnClickConfirmName(Func<string, Task<DisplayNameResult>> onChange)
        {
            if (onChange == null || _isChangingName)
                return;

            _isChangingName = true;
            SetConfirmInteractable(false);

            string typed = _nameInput != null ? _nameInput.text : string.Empty;
            DisplayNameResult result = await onChange.Invoke(typed != null ? typed.Trim() : string.Empty);

            //# 왕복 중 팝업이 닫혀 Destroy 됐을 수 있다 — Unity 의 destroyed 체크(this == null) 후 위젯 접근 금지.
            if (this == null)
                return;

            _isChangingName = false;
            SetConfirmInteractable(true);
            if (result.Status != DisplayNameStatus.Success)
                return;
            RefreshDisplayName(result.Name);
            SetNameEditActive(false);
        }

        private void SetConfirmInteractable(bool interactable)
        {
            if (_nameConfirmButton == null)
                return;
            _nameConfirmButton.Interactable = interactable;
        }

        //# 표시명 라벨 갱신 — 빈 값이면 안내(기본명 자체는 제출 시 결정, 여기선 미설정 표기).
        private void RefreshDisplayName(string displayName)
        {
            if (_displayNameText == null)
                return;
            string shown = string.IsNullOrEmpty(displayName) ? "(미설정 — 기본명 사용)" : displayName;
            _displayNameText.SetText("표시명: ", shown);
        }

        private void SetNameEditActive(bool active)
        {
            //# 열 때 인풋 클리어 — 이전 입력값 잔류 방지(placeholder 는 그대로 노출).
            if (active && _nameInput != null)
                _nameInput.text = string.Empty;
            if (_nameEditGroup != null)
                _nameEditGroup.SetActive(active);
        }

        private void SetupConflict(CloudPopupArg arg)
        {
            if (_conflictDot != null)
                _conflictDot.SetActive(arg.ConflictPending);
            if (_conflictGroup != null)
                _conflictGroup.SetActive(arg.ConflictPending);

            if (arg.ConflictPending == false)
                return;

            if (_conflictText != null)
                _conflictText.SetText("다른 기기의 진행이 클라우드에 더 최신으로 저장되어 있습니다. 클라우드 데이터로 복원하면 지금 기기의 진행은 사라질 수 있습니다.");

            if (_conflictRestoreButton != null)
            {
                Action onConflictRestore = arg.OnConflictRestore;
                _conflictRestoreButton.SetText("클라우드로 복원");
                _conflictRestoreButton.OnClick(() => onConflictRestore?.Invoke(), closeDisposable);
            }
            if (_conflictLaterButton != null)
            {
                Action onConflictLater = arg.OnConflictLater;
                _conflictLaterButton.SetText("나중에");
                _conflictLaterButton.OnClick(() => onConflictLater?.Invoke(), closeDisposable);
            }
        }
    }
}
