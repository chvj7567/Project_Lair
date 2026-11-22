using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lair.UI
{
    public class FormationSpeciesPoolingScrollView : CHPoolingScrollView<FormationSpeciesRow, FormationSpeciesData>
    {
        private Func<EMonster, Sprite> _iconOf;
        private Action<EMonster> _onPlace;
        //# 베이스 Awake 가 private 라 후크가 없어 첫 사용 때 1회 캐싱한다.
        private ScrollRect _scrollRect;

        public int RowCount => liData.Count;

        public void Configure(Func<EMonster, Sprite> iconOf, Action<EMonster> onPlace)
        {
            _iconOf = iconOf;
            _onPlace = onPlace;
        }

        public void ShowRows(IReadOnlyList<FormationSpeciesData> rows, bool resetScroll)
        {
            if (resetScroll || liData.Count != rows.Count)
            {
                SetItemList(new List<FormationSpeciesData>(rows));
                return;
            }

            //# 같은 필터에서는 행의 순서가 유지된다. 보이는 셀만 다시 바인딩해 위치와 포커스를 보존한다.
            for (int i = 0; i < rows.Count; ++i)
            {
                liData[i] = rows[i];
            }
            foreach (PoolingScrollViewItem<FormationSpeciesRow> visible in liPoolItem)
            {
                if (visible.item != null && visible.item.gameObject.activeSelf && visible.index >= 0 && visible.index < liData.Count)
                {
                    InitItem(visible.item, liData[visible.index], visible.index);
                }
            }
        }

        public override void InitItem(FormationSpeciesRow item, FormationSpeciesData data, int index)
        {
            if (item == null || data == null)
                return;
            item.BindPooled(data, _iconOf?.Invoke(data.Monster), () => _onPlace?.Invoke(data.Monster));
        }

        public override void InitPoolingObject(FormationSpeciesRow item)
        {
            item.InitPooledClick();
        }

        public int FocusedRowIndex(GameObject selection)
        {
            foreach (PoolingScrollViewItem<FormationSpeciesRow> visible in liPoolItem)
            {
                if (visible.item != null && visible.item.FocusTarget == selection)
                    return visible.index;
            }
            return -1;
        }

        public void FocusRow(int index)
        {
            FormationSpeciesRow row = VisibleRow(index);
            if (_scrollRect == null)
            {
                _scrollRect = GetComponent<ScrollRect>();
            }
            RectTransform viewport = _scrollRect.viewport;
            if (row != null && viewport != null)
            {
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, row.transform);
                if (bounds.min.y >= viewport.rect.yMin && bounds.max.y <= viewport.rect.yMax)
                {
                    EventSystem.current?.SetSelectedGameObject(row.FocusTarget);
                    return;
                }
            }
            SetScrollPosition(index, () =>
            {
                FormationSpeciesRow focused = VisibleRow(index);
                if (focused != null)
                {
                    EventSystem.current?.SetSelectedGameObject(focused.FocusTarget);
                }
            });
        }

        private FormationSpeciesRow VisibleRow(int index)
        {
            foreach (PoolingScrollViewItem<FormationSpeciesRow> visible in liPoolItem)
            {
                if (visible.index == index && visible.item != null && visible.item.gameObject.activeSelf)
                    return visible.item;
            }
            return null;
        }
    }
}
