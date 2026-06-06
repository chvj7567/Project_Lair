using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 비주얼 스폰 헬퍼. CHMResource/CHMPool 미가용(EditMode/부트 전)이면 무동작.
    //# 자동 반환은 프리팹의 ReturnToPoolAfter 가 담당(빌더가 부착). 궤도형은 미부착 → OnDeactivate 가 회수.
    public static class HeroSkillFx
    {
        //# 부채꼴(평면) FX 의 바닥 위 고정 높이 — Floor(y=0)·MapBackground(y=0.01) 위로 띄워 occlusion/z-fight 회피.
        //# PoisonAura(0.05) 선례보다 약간 높여 확실히 맵 위, 캐릭터(캡슐)보다는 아래 → 캐릭터>범위>맵.
        private const float ConeFloorLiftY = 0.1f;

        //# 스킬 시전 SFX — 순수 C# 런타임이 인프라(CHMSound)에 직접 의존하지 않게 헬퍼로 위임.
        //# CHMSound 미가용(EditMode/부트 전)이면 무동작 — 비주얼 헬퍼와 동일한 null 가드.
        public static void PlaySound(EAudio key)
        {
            CHMSound.Instance?.Play(key);
        }

        //# 디스크/노바 — center 에 균일 스케일 비주얼 1개.
        public static void SpawnAt(EVisual key, Vector3 center, float scale)
        {
            if (CHMResource.Instance == null || CHMPool.Instance == null)
                return;
            CHMResource.Instance.Load<GameObject>(key, prefab =>
            {
                if (prefab == null)
                    return;
                CHPoolable p = CHMPool.Instance.Pop(prefab, null);
                if (p == null)
                    return;
                p.transform.position = center;
                p.transform.localScale = Vector3.one * scale;
            });
        }

        //# 돌진 — origin(영웅)에 부채꼴 mesh 를 dir 로 회전해 배치. mesh 는 빌더가 단위 반경(1)·고정 반각으로 생성 → length 로 균일 스케일.
        //# 부채꼴 mesh 는 로컬 +Z 가 중심축이라 dir(XZ) 로 LookRotation 정렬, XZ 평면에 눕힘.
        public static void SpawnCone(EVisual key, Vector3 origin, Vector3 dir, float length)
        {
            if (CHMResource.Instance == null || CHMPool.Instance == null)
                return;
            CHMResource.Instance.Load<GameObject>(key, prefab =>
            {
                if (prefab == null)
                    return;
                CHPoolable p = CHMPool.Instance.Pop(prefab, null);
                if (p == null)
                    return;
                Vector3 d = dir;
                d.y = 0f;
                if (d.sqrMagnitude < 0.0001f)
                    d = Vector3.forward;
                //# 바닥 AoE 라 높이는 영웅 y 가 아닌 바닥 기준 고정 — 영웅 y 가 바뀌어도 평면 유지.
                p.transform.position = new Vector3(origin.x, ConeFloorLiftY, origin.z);
                p.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                p.transform.localScale = Vector3.one * length;
            });
        }

        //# 영웅 등 Transform 자식으로 부착해 스폰 — origin 이 이동하면 FX 도 따라간다(공포 스컬용). 핸들 반환(없으면 null).
        //# Pop(prefab, parent) 로 부모 부착(worldPositionStays=false). 풀 반환(CHMPool.Push)은 풀 루트로 재부모화 → 분리 안전.
        public static CHPoolable SpawnAttached(EVisual key, Transform parent, Vector3 localOffset, float scale)
        {
            if (CHMResource.Instance == null || CHMPool.Instance == null)
                return null;
            if (parent == null)
                return null;
            GameObject prefab = CHMPool.Instance.GetOriginal(key.ToString());
            if (prefab == null)
                return null;
            CHPoolable p = CHMPool.Instance.Pop(prefab, parent);
            if (p == null)
                return null;
            p.transform.localPosition = localOffset;
            p.transform.localScale = Vector3.one * scale;
            return p;
        }

        //# 영웅 추적 궤도 비주얼 1개 Pop (회전 블레이드용). 핸들 반환(없으면 null).
        //# 동기 경로 — 사전 워밍된 풀의 원본 prefab 을 GetOriginal 로 즉시 조회(콜백 지연 없음 → 매 프레임 재요청 누수 방지).
        public static CHPoolable SpawnTracked(EVisual key)
        {
            if (CHMPool.Instance == null)
                return null;
            GameObject prefab = CHMPool.Instance.GetOriginal(key.ToString());
            if (prefab == null)
                return null;
            return CHMPool.Instance.Pop(prefab, null);
        }
    }
}
