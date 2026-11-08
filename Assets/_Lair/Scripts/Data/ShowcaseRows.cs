using System;

namespace Lair.Data
{
    //# 마을·로딩 몬스터 쇼케이스 연출 JSON row (village-monster-showcase §10). 좌표는 배경 480x270 도트.

    //# Showcase_Scene.json — 씬 하나(Village, Loading). 표시 높이 상한·마을 대기 시간.
    [Serializable]
    public class Showcase_SceneRow
    {
        public string scene;
        public float fitMaxHeightDots;
        public float startWaitMaxSec;
        public float waitMinSec;
        public float waitMaxSec;
    }

    //# Showcase_Actor.json — Showcase_Scene 의 관계, 복합키 scene + monster(기준 몬스터 = 종족): 한 종족의 동작.
    [Serializable]
    public class Showcase_ActorRow
    {
        public string scene;
        public string monster;
        public string behavior;
        public float speedDots;
        public bool flying;
        public bool startFacingLeft;
        public int orderBase;
    }

    //# Showcase_Actor_Waypoint.json — Showcase_Actor 의 관계, 복합키 scene + monster + index: 경유점(Stand 는 1개 = 고정 위치).
    [Serializable]
    public class Showcase_Actor_WaypointRow
    {
        public string scene;
        public string monster;
        public int index;
        public float x;
        public float y;
    }
}
