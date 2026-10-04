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

        //# 캐러셀 상한 = 출격 가능 스테이지(ChapterGate 단일 소유). 진행 저장(StageProgress.MaxStage)은 챕터 전체를 가진다.
        private static int MaxStage => ChapterGate.PlayableStageCount;

        private readonly MetaProfile _profile;
        private readonly MetaConfig _config;
        private readonly ChapterDatabase _chapters;
        private readonly Func<int, string> _text;

        //# 캐러셀 현재 위치 — 진입 시 최대 플레이 가능 스테이지로 초기화(마지막 선택이 아님), 이후 이동은 in-memory 만(저장은 VillageController).
        private int _selectedStage;

        public event Action<int> OnSoulsChanged;
        public event Action OnChanged;
        //# 캐러셀 이동 통지 — View(인디케이터/오버레이/화살표) + Controller(쇼케이스 재스킨/저장) 가 구독.
        public event Action OnStageChanged;

        //# chapters/text 는 챕터 표기(영웅명·잠금 안내)용 — 생략하면 기존 표기(영웅명 폴백, 안내 빈 문자열).
        public VillageViewModel(MetaProfile profile, MetaConfig config, ChapterDatabase chapters = null, Func<int, string> text = null)
        {
            _profile = profile;
            _config = config;
            _chapters = chapters;
            _text = text;
            //# 진입 초기 위치 = 최대 플레이 가능 스테이지(캐러셀 상한 MaxStage 로 클램프). 저장된 SelectedStage 는 무시(기획: 항상 최고 도전 지점부터).
            int cleared = profile != null ? profile.ClearedStage : 0;
            _selectedStage = Math.Min(MaxStage, StageProgress.MaxPlayableStage(cleared));
        }

        //# 캐러셀 — 현재 선택 스테이지(선형 번호).
        public int SelectedStage => _selectedStage;
        //# 인디케이터 "STAGE {챕터}-{인덱스}" / 위협도(챕터 내 인덱스 기준) / 잠금 안내 — 표기 조립은 VM(Rule 02 §6).
        public int SelectedStageIndex => StageProgress.IndexOf(_selectedStage);
        public string StageIndicatorText => $"STAGE {StageLabels.Number(_selectedStage)}";
        public string StageThreatText => StageLabels.Stars(SelectedStageIndex);
        public string StageLockHintText => StageLabels.UnlockHint(_selectedStage, _text);
        //# 클리어 최고 스테이지(잠금 판정 입력). 라이브 프로필값.
        public int ClearedStage => _profile != null ? _profile.ClearedStage : 0;
        //# 현재 스테이지 해금 여부 — 판정은 StageProgress 순수 헬퍼 단일 소유(기획서 §4.6).
        public bool IsCurrentStageUnlocked => StageProgress.IsUnlocked(_selectedStage, ClearedStage);
        //# 경계 클램프 — 화살표 활성 여부(기획서 §4.4).
        public bool CanMovePrev => _selectedStage > MinStage;
        public bool CanMoveNext => _selectedStage < MaxStage;

        //# 월드맵 선택 확정 — 선형 번호로 직접 이동(상한 클램프). 실제 변경 시에만 통지(저장·쇼케이스 재스킨은 컨트롤러가 통지 구독으로 처리).
        public void SelectStage(int linearStage)
        {
            int next = Math.Min(MaxStage, Math.Max(MinStage, linearStage));
            if (next == _selectedStage)
                return;
            _selectedStage = next;
            OnStageChanged?.Invoke();
        }

        //# ◀/▶ 이동 — 1~MaxStage 클램프. 실제 변경 시에만 통지(저장은 컨트롤러가 통지 구독으로 처리, 기획서 §4.5).
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

        //# 마을 전적 패널(제안 1) — 현재 캐러셀 스테이지 기준. 스테이지 이동(OnStageChanged)·프로필 변경(OnChanged) 때 View 가 다시 읽는다.
        //# "이번 침입자": "기사 · 3단계" — 영웅 이름 + 선택 스테이지.
        //# 영웅명은 선택 스테이지 챕터의 영웅 직업(데이터)에서, 없으면 프로필 값 폴백. 단계는 챕터 내 인덱스.
        public string IntruderText => $"{ChapterHeroName()} · {SelectedStageIndex}단계";
        //# "최단 방어": 이 스테이지 최단 클리어 "3:42.1", 기록 없으면 "-".
        public string BestDefenseText => ClearTimeFormat.WithTenths(CurrentStageRecord.BestClearTime);
        //# "이 스테이지 전적": "7승 · 11판".
        public string StageRecordText => $"{CurrentStageRecord.Wins}승 · {CurrentStageRecord.Runs}판";

        private StageRecordEntry CurrentStageRecord => _profile != null
            ? _profile.GetStageRecord(_selectedStage)
            : new StageRecordEntry { Stage = _selectedStage };

        private string ChapterHeroName()
        {
            HeroArchetypeRow hero = _chapters != null && _text != null ? _chapters.GetHeroOfLinear(_selectedStage) : null;
            if (hero != null)
                return _text(hero.nameKey);
            return HeroDisplayName(_profile != null ? _profile.SelectedHero : null);
        }

        //# 영웅 표시 이름 — 기사/궁수. 미지정은 기본 영웅명(기사), 미등록은 원문. 챕터 데이터가 있으면 IntruderText 는 데이터 문구를 쓴다.
        public static string HeroDisplayName(string hero)
        {
            if (string.IsNullOrEmpty(hero) || hero == EHero.Knight.ToString())
                return "기사";
            if (hero == EHero.Archer.ToString())
                return "궁수";
            return hero;
        }

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
