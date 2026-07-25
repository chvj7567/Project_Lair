using System;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;

namespace Lair.Village
{
    //# 마을 허브 ViewModel — 소울/영주 레벨 게이지 가공 + 변경 통지 (Rule 02 §6, MonoBehaviour 아님).
    //# 스테이지 캐러셀(hero-stage-variant §4·§5): SelectedStage 를 in-memory 로 이동. persistence 없음(컨트롤러가 저장).
    public class VillageViewModel
    {
        private const int MinStage = 1;
        private const int MaxStage = 5;

        private readonly MetaProfile _profile;
        private readonly MetaConfig _config;

        //# 캐러셀 현재 위치 — 진입 시 최대 플레이 가능 스테이지로 초기화(마지막 선택이 아님), 이후 이동은 in-memory 만(저장은 VillageController).
        private int _selectedStage;

        public event Action<int> OnSoulsChanged;
        public event Action OnChanged;
        //# 캐러셀 이동 통지 — View(인디케이터/오버레이/화살표) + Controller(쇼케이스 재스킨/저장) 가 구독.
        public event Action OnStageChanged;

        public VillageViewModel(MetaProfile profile, MetaConfig config)
        {
            _profile = profile;
            _config = config;
            //# 진입 초기 위치 = 최대 플레이 가능 스테이지(min(ClearedStage+1, 5)). 저장된 SelectedStage 는 무시(기획: 항상 최고 도전 지점부터).
            int cleared = profile != null ? profile.ClearedStage : 0;
            _selectedStage = StageProgress.MaxPlayableStage(cleared);
        }

        //# 캐러셀 — 현재 선택 스테이지(1~5).
        public int SelectedStage => _selectedStage;
        //# 클리어 최고 스테이지(잠금 판정 입력). 라이브 프로필값.
        public int ClearedStage => _profile != null ? _profile.ClearedStage : 0;
        //# 현재 스테이지 해금 여부 — 판정은 StageProgress 순수 헬퍼 단일 소유(기획서 §4.6).
        public bool IsCurrentStageUnlocked => StageProgress.IsUnlocked(_selectedStage, ClearedStage);
        //# 경계 클램프 — 화살표 활성 여부(기획서 §4.4).
        public bool CanMovePrev => _selectedStage > MinStage;
        public bool CanMoveNext => _selectedStage < MaxStage;

        //# ◀/▶ 이동 — 1~5 클램프. 실제 변경 시에만 통지(저장은 컨트롤러가 통지 구독으로 처리, 기획서 §4.5).
        public void MoveStage(int delta)
        {
            int next = Math.Min(MaxStage, Math.Max(MinStage, _selectedStage + delta));
            if (next == _selectedStage)
                return;
            _selectedStage = next;
            OnStageChanged?.Invoke();
        }

        //# 상단바 표시명(기획서 §1) — DisplayName 우선, 빈 값이면 기본명. 해석은 헬퍼에 위임(View 는 표시만, Rule 02 §6).
        public string DisplayName => MetaProfile.ResolveDisplayName(_profile != null ? _profile.DisplayName : null, Lair.Net.AuthTokenStore.GetOrCreateDeviceId());

        public int Souls => _profile != null ? _profile.Souls : 0;
        public int LordLevel => LordLevelService.LevelFromXp(_profile != null ? _profile.LordXp : 0, _config);
        public float LordProgress => LordLevelService.ProgressInLevel(_profile != null ? _profile.LordXp : 0, _config);

        //# 상점 구매·영웅 선택 등 프로필 변경 후 호출 — View 갱신 트리거.
        public void NotifyProfileChanged()
        {
            OnSoulsChanged?.Invoke(Souls);
            OnChanged?.Invoke();
        }
    }
}
