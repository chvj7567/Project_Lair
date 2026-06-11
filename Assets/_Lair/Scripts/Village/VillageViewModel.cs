using System;
using Lair.Data;
using Lair.Meta;

namespace Lair.Village
{
    //# 마을 허브 ViewModel — 소울/영주 레벨 게이지 가공 + 변경 통지 (Rule 02 §6, MonoBehaviour 아님).
    public class VillageViewModel
    {
        private readonly MetaProfile _profile;
        private readonly MetaConfig _config;

        public event Action<int> OnSoulsChanged;
        public event Action OnChanged;

        public VillageViewModel(MetaProfile profile, MetaConfig config)
        {
            _profile = profile;
            _config = config;
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
