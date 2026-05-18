using System.Collections.Generic;
using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// 자주 쓰는 List / GameObject 확장 메서드 모음.
    /// </summary>
    public static class CHUtil
    {
        /// <summary>null이거나 비어 있으면 true.</summary>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            if (list == null)
            {
                return true;
            }

            if (list.Count <= 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>해당 컴포넌트가 있으면 반환, 없으면 추가 후 반환.</summary>
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// 자식에서 컴포넌트 <typeparamref name="T"/>를 찾는다.
        /// </summary>
        /// <param name="name">지정하면 이름이 같은 자식만 검사. null/빈 문자열이면 이름 무관.</param>
        /// <param name="recursive">true면 손자 이하까지 탐색.</param>
        /// <param name="includeInactive">recursive 탐색 시 비활성 자식도 포함할지 여부. 기본 false.</param>
        public static T FindChild<T>(this GameObject obj, string name = null, bool recursive = false, bool includeInactive = false) where T : Object
        {
            if (obj == null)
            {
                return null;
            }

            if (recursive == false)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    Transform transform = obj.transform.GetChild(i);
                    if (string.IsNullOrEmpty(name) || transform.name == name)
                    {
                        T component = transform.GetComponent<T>();
                        if (component != null)
                        {
                            return component;
                        }
                    }
                }
            }
            else
            {
                foreach (T component in obj.GetComponentsInChildren<T>(includeInactive))
                {
                    if (string.IsNullOrEmpty(name) || component.name == name)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        /// <summary>이름으로 자식 GameObject를 찾는다. (<see cref="FindChild{T}"/>의 GameObject 오버로드)</summary>
        public static GameObject FindChild(this GameObject obj, string name = null, bool recursive = false, bool includeInactive = false)
        {
            Transform transform = FindChild<Transform>(obj, name, recursive, includeInactive);
            if (transform == null)
            {
                return null;
            }

            return transform.gameObject;
        }
    }
}
