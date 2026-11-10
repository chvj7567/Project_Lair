using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ChvjUnityInfra
{

public enum PoolingScrollViewDirection
{
    Vertical,
    Horizontal,
}

public enum PoolingScrollViewAlign
{
    LeftOrTop,
    Center,
    RightOrBottom,
}

public enum PoolingScrollViewScrollingDirection
{
    UpOrLeft,
    DownOrRight,
}

/// <summary>
/// 뷰포트 폭 맞춤 그리드 산식 (순수 함수 - 테스트 가능). 남는 폭을 아이템 폭에 균등 분배해 좌우 여백이 0 이 되게 한다.
/// </summary>
public static class PoolingGridFit
{
    /// <param name="viewportWidth">뷰포트 폭</param>
    /// <param name="paddingHorizontal">좌우 패딩 합</param>
    /// <param name="gapX">열 사이 간격</param>
    /// <param name="minItemWidth">아이템 최소 폭(origin 폭)</param>
    /// <param name="maxItemWidth">아이템 최대 폭 (0 이하이면 제한 없음). 넘으면 열을 하나 늘린다</param>
    /// <param name="minColumns">열 수 하한 (1 미만은 1)</param>
    /// <param name="maxColumns">열 수 상한 (0 이하이면 제한 없음)</param>
    /// <param name="fixedColumns">0 보다 크면 그 열 수 고정(위 규칙 무시), 0 이하이면 자동 계산</param>
    public static void Calc(float viewportWidth, float paddingHorizontal, float gapX, float minItemWidth, float maxItemWidth,
        int minColumns, int maxColumns, int fixedColumns, out int columns, out float itemWidth)
    {
        float available = Mathf.Max(0f, viewportWidth - paddingHorizontal);
        float minWidth = Mathf.Max(1f, minItemWidth);
        if (fixedColumns > 0)
        {
            columns = fixedColumns;
        }
        else
        {
            //# 최소 폭 기준 최대 열 수 -> 폭이 최대 폭을 넘으면 열 하나 추가 -> 하한/상한 적용
            columns = Mathf.Max(1, Mathf.FloorToInt((available + gapX) / (minWidth + gapX)));
            if (maxItemWidth > 0f && WidthOf(available, gapX, columns) > maxItemWidth)
            {
                columns++;
            }
            columns = Mathf.Max(columns, Mathf.Max(1, minColumns));
            if (maxColumns > 0)
            {
                columns = Mathf.Min(columns, Mathf.Max(maxColumns, Mathf.Max(1, minColumns)));
            }
        }
        itemWidth = WidthOf(available, gapX, columns);
        if (itemWidth <= 0f)
        {
            itemWidth = minWidth;
        }
    }

    private static float WidthOf(float available, float gapX, int columns)
    {
        return (available - gapX * (columns - 1)) / columns;
    }
}

public class PoolingScrollViewItem<TItem>
{
    public int index;
    public TItem item;
}

[RequireComponent(typeof(ScrollRect))]
public abstract class CHPoolingScrollView<TItem, TData> : MonoBehaviour where TItem : MonoBehaviour
{
    protected LinkedList<PoolingScrollViewItem<TItem>> liPoolItem = new LinkedList<PoolingScrollViewItem<TItem>>();

    public List<TItem> ItemList
    {
        get
        {
            return liPoolItem.Select(_ => _.item).ToList();
        }
    }

    protected List<TData> liData = new List<TData>();

    public List<TData> DataList
    {
        get
        {
            return liData.ToList();
        }
    }

    [SerializeField, Header("아이템 원본 오브젝트")]
    private GameObject _origin;

    [SerializeField, Header("아이템 크기 (Zero이면 Origin 사이즈로 적용)")]
    private Vector2 _itemSize = Vector2.zero;

    [SerializeField, Header("아이템 사이 간격")]
    private Vector2 _itemGap = Vector2.zero;

    [SerializeField, Header("패딩 설정")]
    private RectOffset _padding = new RectOffset();

    [SerializeField, Header("스크롤 진행 방향")]
    private PoolingScrollViewDirection _scrollDirection = PoolingScrollViewDirection.Vertical;

    [SerializeField, Header("행 개수 (0 이하이면 자동 계산)")]
    private int _rowCount = 0;

    [SerializeField, Header("열 개수 (0 이하이면 자동 계산)")]
    private int _columnCount = 0;

    [SerializeField, Header("오브젝트 풀 개수 (0 이하이면 자동 할당)")]
    private int _poolItemCount = 0;

    [SerializeField, Header("아이템 정렬 방향")]
    private PoolingScrollViewAlign _align = PoolingScrollViewAlign.Center;

    [SerializeField, Header("스크롤뷰 사이즈 변경 시 새로고침 여부")]
    private bool _refresh = false;

    [SerializeField, Header("세로 스크롤 한정: 뷰포트 폭에 맞춰 아이템 폭을 늘려 좌우 여백 제거 (열 수 > 0 이면 고정 열, 0 이면 origin 폭을 최소 폭으로 열 수 계산)")]
    private bool _fitWidth = false;

    [SerializeField, Header("폭 맞춤 시 높이도 origin 비율대로 늘릴지 (끄면 높이 고정)")]
    private bool _fitKeepAspect = false;

    [SerializeField, Header("폭 맞춤 최대 아이템 폭 (0 이하이면 제한 없음, 넘으면 열 추가). 최소 폭 = origin 폭")]
    private float _fitMaxItemWidth = 0f;

    [SerializeField, Header("폭 맞춤 자동 열 수 하한 / 상한 (상한 0 이하이면 제한 없음)")]
    private int _fitMinColumns = 1;

    [SerializeField]
    private int _fitMaxColumns = 0;

    [Space]

    private Vector2 _prevScrollPosition = Vector2.zero;
    private GameObject _contentObject;
    private RectTransform _contentRectTransform;
    private RectTransform _viewPortRectTransform;
    private CanvasGroup _canvasGroupContent;
    private ScrollRect _scrollRect;

    private int _calcRowCount = 0;
    private int _calcColumnCount = 0;
    private int _poolItemCountInit = 0;
    private bool _refreshPending;
    private bool _refreshing;
    private Vector2 _lastViewportSize = Vector2.zero;

    public int LineCount
    {
        get
        {
            int line = 0;

            if (liData.Count == 0)
                return line;

            int count = 1;

            switch (_scrollDirection)
            {
                case PoolingScrollViewDirection.Vertical:
                    {
                        count = _calcColumnCount;
                    }
                    break;
                case PoolingScrollViewDirection.Horizontal:
                    {
                        count = _calcRowCount;
                    }
                    break;
            }

            line = liData.Count / count;
            if (liData.Count % count != 0)
            {
                line += 1;
            }

            return line;
        }
    }

    public float ItemsWidth
    {
        get
        {
            return _calcColumnCount * _itemSize.x + (_calcColumnCount - 1) * _itemGap.x;
        }
    }

    public float ItemsHeight
    {
        get
        {
            return _calcRowCount * _itemSize.y + (_calcRowCount - 1) * _itemGap.y;
        }
    }

    public float ContentWidth
    {
        get
        {
            return _contentRectTransform.rect.width;
        }
    }

    public float ContentHeight
    {
        get
        {
            return _contentRectTransform.rect.height;
        }
    }

    public float ScrollViewWidth
    {
        get
        {
            int line = LineCount;
            float width = line * _itemSize.x;
            width += (line - 1) * _itemGap.x;
            width += _padding.left + _padding.right;
            return width;
        }
    }

    public float ScrollViewHeight
    {
        get
        {
            int line = LineCount;
            float height = line * _itemSize.y;
            height += (line - 1) * _itemGap.y;
            height += _padding.top + _padding.bottom;
            return height;
        }
    }

    private void Awake()
    {
        _poolItemCountInit = _poolItemCount;

        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();

        if (_viewPortRectTransform == null)
            _viewPortRectTransform = _scrollRect.viewport;

        if (_contentObject == null)
            _contentObject = _scrollRect.content.gameObject;

        if (_contentRectTransform == null)
            _contentRectTransform = _scrollRect.content.GetComponent<RectTransform>();

        if (_canvasGroupContent == null)
            _canvasGroupContent = _scrollRect.content.GetComponent<CanvasGroup>();

        //# origin 비활성화는 SetItemList 호출보다 반드시 먼저 끝나야 함 — Start 는 end-of-frame 발화라
        //# InitUI 분기에서 호출된 첫 SetItemList 가 origin 을 데이터 cell 로 사용한 후 Start 가 origin 을
        //# 비활성화시켜 데이터 셀이 사라지는 버그(첫 열림 빈 화면) 를 회피하기 위해 Awake 로 이동.
        if (_origin != null)
            _origin.SetActive(false);
    }

    private void Start()
    {
        var scrollRect = GetComponent<ScrollRect>();
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    /// <summary>
    /// RectTransform 크기 변경 시 Unity가 자동 호출. _refresh가 true면 재구성.
    /// </summary>
    private void OnRectTransformDimensionsChange()
    {
        //# 이 콜백은 캔버스 레이아웃 패스 안에서 불린다 - 여기서 SetItemList(Instantiate·크기 변경)를 돌리면 재진입·예외가 레이아웃 갱신을 멈춰 UI 전체가 먹통이 될 수 있다.
        //# 그래서 표시만 하고 실제 재계산은 LateUpdate 에서 한다.
        if (_refresh && _refreshing == false)
        {
            _refreshPending = true;
        }
    }

    private void LateUpdate()
    {
        if (_refreshPending == false)
            return;
        _refreshPending = false;
        if (_refreshing || _scrollRect == null || _viewPortRectTransform == null || _contentRectTransform == null || _origin == null || liData.Count == 0)
            return;
        //# 뷰포트 크기가 실제로 달라졌을 때만(자기 변경이 만든 콜백으로 매 프레임 재계산되는 루프 방지).
        Rect viewport = _viewPortRectTransform.rect;
        if (Mathf.Abs(viewport.width - _lastViewportSize.x) < 0.5f && Mathf.Abs(viewport.height - _lastViewportSize.y) < 0.5f)
            return;
        _refreshing = true;
        try
        {
            Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CHPoolingScrollView] Refresh 실패 ({name}): {e}");
        }
        finally
        {
            _refreshing = false;
        }
    }

    /// <summary>
    /// 데이터 초기화 후 스크롤하여 인덱스가 바뀔 때마다 호출.
    /// </summary>
    /// <param name="item">풀에서 재사용 중인 아이템 인스턴스</param>
    /// <param name="data">해당 인덱스의 데이터</param>
    /// <param name="index">현재 인덱스</param>
    public abstract void InitItem(TItem item, TData data, int index);

    /// <summary>
    /// 풀링 오브젝트 생성 시 호출 (최초 1회).
    /// </summary>
    /// <param name="item">새로 생성된 아이템 인스턴스</param>
    public abstract void InitPoolingObject(TItem item);

    /// <summary>
    /// 스크롤 뷰 데이터 설정.
    /// </summary>
    /// <param name="dataList">표시할 데이터 목록</param>
    public void SetItemList(List<TData> dataList)
    {
        liData.Clear();
        liData.AddRange(dataList);

        switch (_scrollDirection)
        {
            case PoolingScrollViewDirection.Vertical:
                {
                    _scrollRect.horizontal = false;
                    _scrollRect.vertical = true;
                }
                break;
            case PoolingScrollViewDirection.Horizontal:
                {
                    _scrollRect.horizontal = true;
                    _scrollRect.vertical = false;
                }
                break;
        }

        _lastViewportSize = _viewPortRectTransform != null ? _viewPortRectTransform.rect.size : Vector2.zero;

        //# 아이템 크기 설정
        SetItemSize();

        //# 행 개수 설정
        SetRowCount();

        //# 열 개수 설정
        SetColumnCount();

        //# 폭 맞춤(옵션) - 열 수·아이템 폭을 뷰포트 기준으로 재계산. 풀 개수도 새 열 수 기준으로 다시 잡는다.
        if (UseFitWidth)
        {
            ApplyFitWidth();
            _poolItemCount = _poolItemCountInit;
        }

        //# 아이템 풀 개수 설정
        SetPoolItemCount();

        //# Content 오브젝트 설정
        SetContentTransform();

        //# 풀링 오브젝트 생성
        CreatePoolingObject();

        //# 아이템 초기화
        InitItem();
    }

    private IEnumerator LerpAnchorPos(RectTransform rt, Vector2 target, float duration, Action onComplete)
    {
        Vector2 start = rt.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }
        rt.anchoredPosition = target;
        onComplete?.Invoke();
    }

    public void SetScrollPosition(int index, Action completeCallback = null, float duration = 0f)
    {
        //# 빈 풀 가드 — 데이터/풀 미구성 시 위치 계산·rebind 모두 무의미하므로 즉시 종료.
        if (liPoolItem.Count == 0)
            return;

        Vector2 pos = GetItemPosition(index);

        float x = _contentRectTransform.anchoredPosition.x;
        float y = _contentRectTransform.anchoredPosition.y;

        //# 현재 위치에서 목표 인덱스 위치로 단일 이동 (lastIndex 경유 2단계 점프 제거).
        switch (_scrollDirection)
        {
            case PoolingScrollViewDirection.Vertical:
                {
                    float tempY = Mathf.Min(ContentHeight - _viewPortRectTransform.rect.height, -pos.y);
                    tempY = tempY < 0 ? 0 : tempY;
                    y = tempY;
                }
                break;
            case PoolingScrollViewDirection.Horizontal:
                {
                    float tempX = Mathf.Min(ContentHeight - _viewPortRectTransform.rect.width, -pos.x);
                    tempX = tempX < 0 ? 0 : tempX;
                    x = tempX;
                }
                break;
        }

        //# 이동 완료 후 InitItem() 으로 도착 지점 셀을 강제 rebind — 증분 OnScroll 의존을 끊는다.
        //# duration 0 이면 LerpAnchorPos 는 yield 없이 동기 완료라 onComplete 도 동기 발화한다.
        StartCoroutine(LerpAnchorPos(_contentRectTransform, new Vector2(x, y), duration, () =>
        {
            InitItem();
            completeCallback?.Invoke();
        }));
    }

    public void Refresh()
    {
        SetItemList(liData.ToList());
    }

    public void Clear()
    {
        liData.Clear();
        liPoolItem.Clear();
        if (_contentObject)
        {
            int childCount = _contentObject.transform.childCount;
            GameObject[] children = new GameObject[childCount];

            for (int i = 0; i < childCount; i++)
            {
                children[i] = _contentObject.transform.GetChild(i).gameObject;
            }

            foreach (var child in children)
            {
                child.SetActive(false);
            }
        }
    }

    private void SetItemSize()
    {
        RectTransform rectTransform = _origin.GetComponent<RectTransform>();
        if (rectTransform == null)
            throw new NullReferenceException();

        _itemSize.x = rectTransform.rect.width * rectTransform.localScale.x;
        _itemSize.y = rectTransform.rect.height * rectTransform.localScale.y;
    }

    private bool UseFitWidth => _fitWidth && _scrollDirection == PoolingScrollViewDirection.Vertical;

    private void ApplyFitWidth()
    {
        //# 뷰포트 폭이 아직 0(레이아웃 전)이면 맞춤을 건너뛰고 기본 계산을 유지한다 - 음수 폭·열 수 폭주 방지.
        if (_viewPortRectTransform.rect.width <= 0f)
            return;
        float originWidth = _itemSize.x;
        float originHeight = _itemSize.y;
        PoolingGridFit.Calc(_viewPortRectTransform.rect.width, _padding.left + _padding.right, _itemGap.x, originWidth, _fitMaxItemWidth,
            _fitMinColumns, _fitMaxColumns, _columnCount, out int columns, out float itemWidth);
        _calcColumnCount = columns;
        _itemSize.x = itemWidth;
        if (_fitKeepAspect && originWidth > 0f)
        {
            _itemSize.y = originHeight * itemWidth / originWidth;
        }
    }

    private void SetColumnCount()
    {
        if (_columnCount <= 0)
        {
            float width = _viewPortRectTransform.rect.width - (_padding.left + _padding.right);
            _calcColumnCount = Mathf.Max(1, Mathf.FloorToInt(width / (_itemSize.x + _itemGap.x)));
        }
        else
        {
            _calcColumnCount = _columnCount;
        }
    }

    private void SetRowCount()
    {
        if (_rowCount <= 0)
        {
            float width = _viewPortRectTransform.rect.height - (_padding.top + _padding.bottom);
            _calcRowCount = Mathf.Max(1, Mathf.FloorToInt(width / (_itemSize.y + _itemGap.y)));
        }
        else
        {
            _calcRowCount = _rowCount;
        }
    }

    private void SetPoolItemCount()
    {
        if (_poolItemCount <= 0)
        {
            switch (_scrollDirection)
            {
                case PoolingScrollViewDirection.Vertical:
                    {
                        int line = Mathf.RoundToInt(_viewPortRectTransform.rect.size.y / _itemSize.y);
                        line += 2;
                        _poolItemCount = line * _calcColumnCount;
                    }
                    break;
                case PoolingScrollViewDirection.Horizontal:
                    {
                        int line = Mathf.RoundToInt(_viewPortRectTransform.rect.size.x / _itemSize.x);
                        line += 2;
                        _poolItemCount = line * _calcRowCount;
                    }
                    break;
            }
        }
    }

    private void SetContentTransform()
    {
        switch (_scrollDirection)
        {
            case PoolingScrollViewDirection.Vertical:
                {
                    //# 스트레치 앵커링 설정
                    _contentRectTransform.anchorMax = new Vector2(.5f, 1f);
                    _contentRectTransform.anchorMin = new Vector2(.5f, 1f);
                    _contentRectTransform.pivot = new Vector2(.5f, 1f);

                    //# 사이즈 재설정
                    _contentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _viewPortRectTransform.rect.width);
                    _contentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ScrollViewHeight);

                    //# 최상단으로 이동
                    _contentRectTransform.anchoredPosition = Vector2.zero;
                }
                break;
            case PoolingScrollViewDirection.Horizontal:
                {
                    //# 스트레치 앵커링 설정
                    _contentRectTransform.anchorMax = new Vector2(0f, .5f);
                    _contentRectTransform.anchorMin = new Vector2(0f, .5f);
                    _contentRectTransform.pivot = new Vector2(0f, .5f);

                    //# 사이즈 재설정
                    _contentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ScrollViewWidth);
                    _contentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _viewPortRectTransform.rect.height);

                    //# 최상단으로 이동
                    _contentRectTransform.anchoredPosition = Vector2.zero;
                }
                break;
        }
    }

    private Vector2 GetItemPosition(int index)
    {
        Vector2 pos = Vector2.zero;

        switch (_scrollDirection)
        {
            case PoolingScrollViewDirection.Horizontal:
                pos = GetItemHorizontalPosition(index);
                break;
            case PoolingScrollViewDirection.Vertical:
                pos = GetItemVerticalPosition(index);
                break;
        }

        return pos;
    }

    private Vector2 GetItemHorizontalPosition(int index)
    {
        int rowIndex = index % _calcRowCount;

        float y = rowIndex * _itemSize.y;
        y += rowIndex * _itemGap.y;

        float diff = ContentHeight - ItemsHeight;

        switch (_align)
        {
            case PoolingScrollViewAlign.LeftOrTop:
                {
                    y += 0;
                    y += _padding.top;
                }
                break;
            case PoolingScrollViewAlign.Center:
                {
                    y += (diff) * 0.5f;
                    y += _padding.top;
                }
                break;
            case PoolingScrollViewAlign.RightOrBottom:
                {
                    y += diff;
                    y -= _padding.bottom;
                }
                break;
        }

        y *= -1;

        int colIndex = index / _calcRowCount;

        float x = colIndex * _itemSize.x;
        x += colIndex * _itemGap.x;
        x += _padding.left;

        return new Vector2(x, y);
    }

    private Vector2 GetItemVerticalPosition(int index)
    {
        int colIndex = index % _calcColumnCount;

        float x = colIndex * _itemSize.x;
        x += colIndex * _itemGap.x;

        float diff = ContentWidth - ItemsWidth;

        switch (_align)
        {
            case PoolingScrollViewAlign.LeftOrTop:
                {
                    x += 0;
                    x += _padding.left;
                }
                break;
            case PoolingScrollViewAlign.Center:
                {
                    x += (diff) * 0.5f;
                    x += _padding.left;
                }
                break;
            case PoolingScrollViewAlign.RightOrBottom:
                {
                    x += diff;
                    x -= _padding.right;
                }
                break;
        }

        int rowIndex = index / _calcColumnCount;

        float y = rowIndex * _itemSize.y;
        y += rowIndex * _itemGap.y;
        y += _padding.top;
        y *= -1;

        return new Vector2(x, y);
    }

    public Rect GetViewConentRect()
    {
        Rect rect = new Rect();

        switch (_scrollDirection)
        {
            case PoolingScrollViewDirection.Vertical:
                {
                    float scrollRectY = _viewPortRectTransform.rect.size.y;
                    rect.Set(_contentRectTransform.anchoredPosition.x, -1 * (_contentRectTransform.anchoredPosition.y + scrollRectY), ContentWidth, scrollRectY + _itemSize.y);
                }
                break;
            case PoolingScrollViewDirection.Horizontal:
                {
                    float scrollRectX = _viewPortRectTransform.rect.size.x;
                    float scrollRectY = _viewPortRectTransform.rect.size.y;
                    rect.Set(-1 * _contentRectTransform.anchoredPosition.x, -1 * (_contentRectTransform.anchoredPosition.y + scrollRectY), scrollRectX + _itemSize.x, ContentHeight);
                }
                break;
        }

        return rect;
    }

    private void OnScroll(Vector2 scrollPosition)
    {
        if (liPoolItem.Count <= 0)
            return;

        Vector2 delta = scrollPosition - _prevScrollPosition;
        _prevScrollPosition = scrollPosition;

        switch (_scrollDirection)
        {
            case PoolingScrollViewDirection.Vertical:
                {
                    //# 이전 위치와 달라졌다면
                    if (delta.y != 0)
                    {
                        UpdateContent(delta.y > 0 ? PoolingScrollViewScrollingDirection.UpOrLeft : PoolingScrollViewScrollingDirection.DownOrRight);
                    }
                }
                break;
            case PoolingScrollViewDirection.Horizontal:
                {
                    //# 이전 위치와 달라졌다면
                    if (delta.x != 0)
                    {
                        UpdateContent(delta.x < 0 ? PoolingScrollViewScrollingDirection.UpOrLeft : PoolingScrollViewScrollingDirection.DownOrRight);
                    }
                }
                break;
        }
    }

    private void UpdateContent(PoolingScrollViewScrollingDirection direction)
    {
        if (liPoolItem.Count <= 0)
            return;

        Rect contentRect = GetViewConentRect();
        Rect itemRect = new Rect();

        switch (direction)
        {
            case PoolingScrollViewScrollingDirection.UpOrLeft:
                {
                    int firstIndex = liPoolItem.First.Value.index;
                    bool wasOverlapping = false;
                    for (int i = firstIndex - 1; i >= 0; --i)
                    {
                        Vector2 itemPosition = GetItemPosition(i);
                        itemRect.Set(itemPosition.x, itemPosition.y, _itemSize.x, _itemSize.y);

                        if (contentRect.Overlaps(itemRect))
                        {
                            wasOverlapping = true;
                            var node = liPoolItem.Last;
                            liPoolItem.Remove(node);

                            InitItem(node.Value.item, i);

                            node.Value.index = i;
                            liPoolItem.AddFirst(node);
                        }
                        else if (wasOverlapping)
                        {
                            // 한 번 viewport와 겹친 후 다시 벗어났음 — 그 너머 인덱스는 모두 viewport 밖
                            break;
                        }
                    }
                }
                break;
            case PoolingScrollViewScrollingDirection.DownOrRight:
                {
                    int lastIndex = liPoolItem.Last.Value.index;
                    bool wasOverlapping = false;
                    for (int i = lastIndex + 1; i < liData.Count; ++i)
                    {
                        Vector2 itemPosition = GetItemPosition(i);
                        itemRect.Set(itemPosition.x, itemPosition.y, _itemSize.x, _itemSize.y);

                        if (contentRect.Overlaps(itemRect))
                        {
                            wasOverlapping = true;
                            var node = liPoolItem.First;
                            liPoolItem.Remove(node);

                            InitItem(node.Value.item, i);

                            node.Value.index = i;
                            liPoolItem.AddLast(node);
                        }
                        else if (wasOverlapping)
                        {
                            // 한 번 viewport와 겹친 후 다시 벗어났음 — 그 너머 인덱스는 모두 viewport 밖
                            break;
                        }
                    }
                }
                break;
        }
    }

    private void InitItem()
    {
        liPoolItem.Clear();

        //# origin 은 항상 비활성 템플릿 — Awake 시점에 _origin 미할당이었던 경로 대비 재확인.
        if (_origin != null)
            _origin.SetActive(false);

        //# 기존 풀링 오브젝트 가져오기 — origin 은 순수 템플릿이라 데이터 셀 목록에서 제외
        List<GameObject> liChildObj = GetContentChildrenExcludingOrigin();

        Rect contentRect = GetViewConentRect();
        Rect itemRect = new Rect();
        int firstIndex = Enumerable.Range(0, liData.Count).FirstOrDefault(_ =>
        {
            Vector2 itemPosition = GetItemPosition(_);
            itemRect.Set(itemPosition.x, itemPosition.y, _itemSize.x, _itemSize.y);

            //# Content와 겹치는 경우 반환
            return contentRect.Overlaps(itemRect);
        });

        for (int i = 0; i < liChildObj.Count; ++i)
        {
            liChildObj[i].SetActive(true);
        }

        for (int i = 0; i < liChildObj.Count; ++i)
        {
            int index = i + firstIndex;
            TItem item = liChildObj[i].GetComponent<TItem>();

            InitItem(item, index);
            PoolingScrollViewItem<TItem> poolItem = new PoolingScrollViewItem<TItem>() { index = index, item = item };
            liPoolItem.AddLast(poolItem);
        }
    }

    private void InitItem(TItem item, int index)
    {
        TData info = liData.ElementAtOrDefault(index);

        bool isNotNull = info != null;
        item.gameObject.SetActive(isNotNull);

        if (isNotNull)
        {
            InitItem(item, info, index);
        }

        InitItemTransform(item.gameObject, index);
        item.gameObject.name = $"{_origin.name} {index}";
    }

    private void InitItemTransform(GameObject item, int index)
    {
        var rectTransform = item.GetComponent<RectTransform>();

        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);

        if (UseFitWidth)
        {
            rectTransform.sizeDelta = new Vector2(_itemSize.x, _itemSize.y);
        }

        rectTransform.anchoredPosition = GetItemPosition(index);
        rectTransform.SetSiblingIndex(index);
    }

    //# origin 은 Content 의 실제 자식이지만 복제 소스일 뿐 데이터 셀이 아니므로 목록에서 제외.
    //# CreatePoolingObject 의 카운트 계산과 InitItem() 의 순회가 이 하나의 소스를 공유한다.
    private List<GameObject> GetContentChildrenExcludingOrigin()
    {
        int childCount = _contentObject.transform.childCount;
        List<GameObject> liChildObj = new List<GameObject>(childCount);

        for (int i = 0; i < childCount; ++i)
        {
            GameObject child = _contentObject.transform.GetChild(i).gameObject;
            if (child == _origin)
                continue;

            liChildObj.Add(child);
        }

        return liChildObj;
    }

    private void CreatePoolingObject()
    {
        int diff = _poolItemCount - GetContentChildrenExcludingOrigin().Count;
        diff = Mathf.Min(diff, liData.Count); //# 필요한 만큼만 풀링 생성
        if (diff > 0)
        {
            for (int i = 0; i < diff; ++i)
            {
                GameObject obj = Instantiate(_origin, _contentObject.transform);
                InitPoolingObject(obj.GetComponent<TItem>());
            }
        }
    }
}

}
