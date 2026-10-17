using System;
using UnityEngine;

namespace Lair.UI
{
    //# 패널 접힘 상태 저장 경계 — 키 읽기·쓰기만(scene-2d-conversion §4.10). 테스트는 가짜 구현 주입.
    //# 구현체 1개 + 테스트 가짜만 쓰는 UI 내부 추상화라 CommonInterface 대신 VM 파일 옆에 둔다(Rule 02 §9).
    public interface IHudLayoutPrefs
    {
        bool LoadCollapsed(string key);
        void SaveCollapsed(string key, bool collapsed);
    }

    //# 기기별 화면 취향 — 클라우드 백업 대상이 아니므로 PlayerPrefs(int 0/1).
    public class PlayerPrefsHudLayoutPrefs : IHudLayoutPrefs
    {
        public bool LoadCollapsed(string key) => PlayerPrefs.GetInt(key, 0) == 1;

        public void SaveCollapsed(string key, bool collapsed)
        {
            PlayerPrefs.SetInt(key, collapsed ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    //# 오른쪽 열 시너지·빌드 패널 접힘 상태 소유·저장·통지(POCO, View 를 모름).
    public class HudLayoutViewModel
    {
        public const string SynergyKey = "Lair.Hud.SynergyCollapsed";
        public const string BuildKey = "Lair.Hud.BuildCollapsed";

        private readonly IHudLayoutPrefs _prefs;

        public bool IsSynergyCollapsed { get; private set; }
        public bool IsBuildCollapsed { get; private set; }

        public event Action<bool> OnSynergyCollapsedChanged;
        public event Action<bool> OnBuildCollapsedChanged;

        public HudLayoutViewModel(IHudLayoutPrefs prefs)
        {
            _prefs = prefs;
            IsSynergyCollapsed = prefs != null && prefs.LoadCollapsed(SynergyKey);
            IsBuildCollapsed = prefs != null && prefs.LoadCollapsed(BuildKey);
        }

        public void ToggleSynergy()
        {
            IsSynergyCollapsed = IsSynergyCollapsed == false;
            _prefs?.SaveCollapsed(SynergyKey, IsSynergyCollapsed);
            OnSynergyCollapsedChanged?.Invoke(IsSynergyCollapsed);
        }

        public void ToggleBuild()
        {
            IsBuildCollapsed = IsBuildCollapsed == false;
            _prefs?.SaveCollapsed(BuildKey, IsBuildCollapsed);
            OnBuildCollapsedChanged?.Invoke(IsBuildCollapsed);
        }
    }
}
