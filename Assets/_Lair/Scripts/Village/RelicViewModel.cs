using System;
using System.Collections.Generic;
using System.Text;
using Lair.Data;
using Lair.Meta;
using UnityEngine;

namespace Lair.Village
{
    //# 도감 그리드 셀 한 칸의 확정 표시값 — View(RelicCell)는 이 값을 그대로 표시만 한다 (Rule 02 §6).
    public class RelicCellData
    {
        public string RelicId;
        public Sprite Icon;
        public bool IsOwned;
        public int Stack;                 //# 보유 중첩(maxStack 클램프 후). 미보유 0
        public int MaxStack;
        public string NameText;           //# 미보유면 "???"
        public string StackText;          //# "n/m" (미보유면 빈 문자열)
        public Color BorderColor;         //# 보유=등급 색, 미보유=#232834
        public bool IsMax;                //# 최대 중첩 — "MAX" 금색 라벨
        public bool IsSelected;
        public Action<string> OnSelect;
    }

    //# 상세 패널 확정 표시값 (relics §7.2).
    public class RelicDetailData
    {
        public string RelicId;
        public Sprite Icon;
        public bool IsOwned;
        public string NameText;
        public string RarityText;
        public Color RarityColor;
        public string CategoryText;
        public string StackText;          //# "중첩 n / m" (보유 시)
        public string CurrentLabelText;   //# "현재 효과"
        public string CurrentEffectText;  //# 현재 중첩의 효과 설명 (보유 시)
        public bool ShowNext;             //# 최대가 아닐 때 "다음 중첩" 블록 표시
        public string NextLabelText;
        public string NextEffectText;
        public bool ShowMaxLabel;         //# 최대일 때 "최대 중첩"
        public string MaxLabelText;
        public string DropHeroText;       //# "드랍 영웅: 기사, 궁수"
        public string LockedText;         //# 미획득 안내 (미보유 시 설명 대신)
    }

    //# 유물 도감 ViewModel — 필터·셀 데이터·상세 텍스트 계산. View 를 모르고(테스트 가능) 프로필은 읽기만 한다.
    //# 유물 DB 가 null(로드 실패)이면 모든 목록이 비어 있다(relics §3.5-7).
    public class RelicViewModel
    {
        public const int StrTitle = 1338;
        public const int StrCollected = 1339;
        public const int StrAll = 1340;
        public const int StrUnknownName = 1347;
        public const int StrStack = 1348;
        public const int StrDropHeroes = 1353;
        public const int StrCurrent = 1354;
        public const int StrNext = 1355;
        public const int StrMax = 1356;
        public const int StrLocked = 1357;
        public const int StrNoResult = 1359;

        //# 필터 칩 인덱스 — 0 = 전체, 1~ = 해당 enum 값 + 1.
        public const int FilterAll = 0;

        private static readonly Color UnownedBorder = new Color32(0x23, 0x28, 0x34, 0xFF);
        private static readonly Color CommonFallback = new Color32(0x9A, 0xA5, 0xBD, 0xFF);

        private readonly RelicDatabase _db;
        private readonly MetaProfile _profile;
        private readonly Func<int, string> _text;
        private readonly Func<string, string> _heroNameOf;
        private readonly Func<string, Sprite> _iconOf;

        private int _categoryFilter;
        private int _rarityFilter;
        private string _selectedId;

        public event Action OnChanged;

        public RelicViewModel(RelicDatabase db, MetaProfile profile, Func<int, string> text, Func<string, string> heroNameOf, Func<string, Sprite> iconOf)
        {
            _db = db;
            _profile = profile;
            _text = text ?? (id => string.Empty);
            _heroNameOf = heroNameOf ?? (id => id);
            _iconOf = iconOf ?? (id => null);
        }

        public int CategoryFilter => _categoryFilter;
        public int RarityFilter => _rarityFilter;
        //# 실효 선택 — 명시 선택이 현재 필터 목록 안에 있으면 그것, 아니면 목록 첫 유물(없으면 null). 목업 relics.html 과 같은 기본 선택.
        public string SelectedRelicId
        {
            get
            {
                List<RelicData> list = FilteredRelics();
                if (list.Count == 0)
                    return null;
                foreach (RelicData data in list)
                {
                    if (data.RelicId == _selectedId)
                        return _selectedId;
                }
                return list[0].RelicId;
            }
        }

        //# 필터 변경 시 선택을 해제한다 (relics §7.2).
        public void SetCategoryFilter(int index)
        {
            if (index == _categoryFilter)
                return;
            _categoryFilter = index;
            _selectedId = null;
            OnChanged?.Invoke();
        }

        public void SetRarityFilter(int index)
        {
            if (index == _rarityFilter)
                return;
            _rarityFilter = index;
            _selectedId = null;
            OnChanged?.Invoke();
        }

        public void Select(string relicId)
        {
            if (relicId == SelectedRelicId)
                return;
            _selectedId = relicId;
            OnChanged?.Invoke();
        }

        //# 보유 중첩 — DB 에 없는 id 는 0(무시), maxStack 초과는 maxStack 으로 클램프. 프로필은 수정하지 않는다(relics §3.6).
        public int OwnedStack(RelicData data)
        {
            if (data == null || _profile == null)
                return 0;
            return Math.Max(0, Math.Min(_profile.GetRelicStack(data.RelicId), data.MaxStack));
        }

        public int TotalKinds => _db != null ? _db.Relics.Count : 0;

        public int OwnedKinds
        {
            get
            {
                int count = 0;
                if (_db == null)
                    return count;
                foreach (RelicData data in _db.Relics)
                {
                    if (OwnedStack(data) >= 1)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public int OwnedStackSum
        {
            get
            {
                int sum = 0;
                if (_db == null)
                    return sum;
                foreach (RelicData data in _db.Relics)
                {
                    sum += OwnedStack(data);
                }
                return sum;
            }
        }

        public int TotalStackSlots
        {
            get
            {
                int sum = 0;
                if (_db == null)
                    return sum;
                foreach (RelicData data in _db.Relics)
                {
                    sum += data.MaxStack;
                }
                return sum;
            }
        }

        //# "수집 {보유 종류} / {전체 종류}"
        public string CollectedText => string.Format(Lookup(StrCollected, "수집 {0} / {1}"), OwnedKinds, TotalKinds);

        //# 오른쪽 "{현재 중첩 합} / {전체 슬롯}"
        public string StackSumText => $"{OwnedStackSum} / {TotalStackSlots}";

        public string NoResultText => Lookup(StrNoResult, "해당 조건의 유물이 없습니다");

        //# 필터(AND)를 통과한 유물을 JSON 순서(종류 묶음) 그대로.
        public List<RelicData> FilteredRelics()
        {
            List<RelicData> list = new List<RelicData>();
            if (_db == null)
                return list;
            foreach (RelicData data in _db.Relics)
            {
                if (_categoryFilter != FilterAll && (int)data.Category + 1 != _categoryFilter)
                    continue;
                if (_rarityFilter != FilterAll && (int)data.Rarity + 1 != _rarityFilter)
                    continue;
                list.Add(data);
            }
            return list;
        }

        public bool IsEmptyResult => FilteredRelics().Count == 0;

        public List<RelicCellData> BuildCells()
        {
            List<RelicCellData> cells = new List<RelicCellData>();
            string selected = SelectedRelicId;
            foreach (RelicData data in FilteredRelics())
            {
                int stack = OwnedStack(data);
                bool owned = stack >= 1;
                cells.Add(new RelicCellData
                {
                    RelicId = data.RelicId,
                    Icon = _iconOf(data.RelicId),
                    IsOwned = owned,
                    Stack = stack,
                    MaxStack = data.MaxStack,
                    NameText = owned ? Lookup(data.NameKey, data.RelicId) : Lookup(StrUnknownName, "???"),
                    StackText = owned ? $"{stack}/{data.MaxStack}" : string.Empty,
                    BorderColor = owned ? RarityColorOf(data.Rarity) : UnownedBorder,
                    IsMax = owned && stack >= data.MaxStack,
                    IsSelected = data.RelicId == selected,
                    OnSelect = Select,
                });
            }
            return cells;
        }

        //# 선택된 유물 상세 — 선택이 없거나 DB 에 없으면 null (View 가 상세 패널을 숨긴다).
        public RelicDetailData BuildDetail()
        {
            RelicData data = _db != null ? _db.Get(SelectedRelicId) : null;
            if (data == null)
                return null;

            int stack = OwnedStack(data);
            bool owned = stack >= 1;
            RelicRarityRow rarityRow = _db.RarityRow(data.Rarity);
            RelicDetailData detail = new RelicDetailData
            {
                RelicId = data.RelicId,
                Icon = _iconOf(data.RelicId),
                IsOwned = owned,
                NameText = owned ? Lookup(data.NameKey, data.RelicId) : Lookup(StrUnknownName, "???"),
                RarityText = rarityRow != null ? Lookup(rarityRow.nameKey, data.Rarity.ToString()) : data.Rarity.ToString(),
                RarityColor = RarityColorOf(data.Rarity),
                CategoryText = CategoryText(data.Category),
                CurrentLabelText = Lookup(StrCurrent, "현재 효과"),
                NextLabelText = Lookup(StrNext, "다음 중첩"),
                MaxLabelText = Lookup(StrMax, "최대 중첩"),
                DropHeroText = DropHeroText(data),
                LockedText = Lookup(StrLocked, "아직 획득하지 못한 유물입니다"),
            };
            if (owned == false)
                return detail;

            detail.StackText = string.Format(Lookup(StrStack, "중첩 {0} / {1}"), stack, data.MaxStack);
            detail.CurrentEffectText = RelicText.Describe(_db, data, stack, _text);
            detail.ShowMaxLabel = stack >= data.MaxStack;
            detail.ShowNext = stack < data.MaxStack;
            if (detail.ShowNext)
            {
                detail.NextEffectText = RelicText.Describe(_db, data, stack + 1, _text);
            }
            return detail;
        }

        //# "드랍 영웅: 기사, 궁수" — 이 유물을 드랍하는 영웅들을 챕터 순서로.
        private string DropHeroText(RelicData data)
        {
            StringBuilder names = new StringBuilder();
            ChapterDatabase chapters = ChapterDatabase.Current;
            IReadOnlyList<ChapterRow> order = chapters != null ? chapters.Chapters : null;
            if (order == null)
                return string.Empty;
            foreach (ChapterRow chapter in order)
            {
                foreach (Relic_DropRow drop in _db.DropsOf(chapter.heroId))
                {
                    if (drop.relicId != data.RelicId)
                        continue;
                    if (names.Length > 0)
                    {
                        names.Append(", ");
                    }
                    names.Append(_heroNameOf(chapter.heroId));
                    break;
                }
            }
            return string.Format(Lookup(StrDropHeroes, "드랍 영웅: {0}"), names);
        }

        //# 종류 라벨 — 필터 칩과 같은 문구 id(능력치 1341 / 전투 효과 1342 / 편의 1343).
        public string CategoryText(ERelicCategory category)
        {
            switch (category)
            {
                case ERelicCategory.Stat:
                    return Lookup(1341, "능력치");
                case ERelicCategory.Combat:
                    return Lookup(1342, "전투 효과");
                default:
                    return Lookup(1343, "편의");
            }
        }

        public Color RarityColorOf(ERelicRarity rarity)
        {
            RelicRarityRow row = _db != null ? _db.RarityRow(rarity) : null;
            if (row != null && ColorUtility.TryParseHtmlString(row.colorHex, out Color color))
                return color;
            return CommonFallback;
        }

        private string Lookup(int id, string fallback)
        {
            string text = _text(id);
            return string.IsNullOrEmpty(text) ? fallback : text;
        }
    }
}
