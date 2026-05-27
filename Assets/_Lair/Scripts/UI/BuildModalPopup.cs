using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class BuildModalPopupArg : UIArg
    {
        public BattleViewModel ViewModel;
    }

    //# 화면 중앙 모달 — 픽한 모든 카드 표시. 좌(패시브) : 우(액티브) 50:50.
    //# 패시브 섹션은 카테고리 그룹(Enhance→Spawn→Replace→Environment), 액티브는 픽 시간 순 (기획서 §2.7.4).
    //# 모달은 최대 17 셀 — Rule 11 의 CHPoolingScrollView 우선 원칙 예외, 단순 VerticalLayoutGroup 사용.
    public class BuildModalPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;         //# 전체 화면 dim (#000 α=0.6) CHButton — 클릭 시 닫힘
        [SerializeField] private CHButton _closeButton;       //# 우상단 X
        [SerializeField] private Transform _passiveContent;   //# 좌 섹션 ScrollRect.content
        [SerializeField] private Transform _activeContent;    //# 우 섹션 ScrollRect.content
        [SerializeField] private GameObject _cellPrefab;      //# BuildModalCardCell.prefab — 모달 카드 셀
        [SerializeField] private CHText _passiveEmptyText;    //# 빈 상태 라벨 (패시브)
        [SerializeField] private CHText _activeEmptyText;     //# 빈 상태 라벨 (액티브)

        //# 셀 풀로 반환할 때 추적용 (Push) — 재사용 안전 (Rule 12).
        private readonly List<GameObject> _spawnedCells = new();

        public override void InitUI(UIArg arg)
        {
            if (arg is BuildModalPopupArg ma && ma.ViewModel != null)
                Build(ma.ViewModel);

            //# 배경 dim 클릭 / X 버튼 클릭 → 닫힘.
            if (_dimButton != null)
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            if (_closeButton != null)
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
        }

        public override void Close(bool reuse = true)
        {
            //# 셀 풀 반환.
            foreach (var go in _spawnedCells)
            {
                if (go == null) continue;
                var poolable = go.GetComponent<CHPoolable>();
                if (poolable != null) CHMPool.Instance.Push(poolable);
                else                  Destroy(go);
            }
            _spawnedCells.Clear();
            base.Close(reuse);
        }

        private void Build(BattleViewModel vm)
        {
            var entries = vm.Build;
            //# 패시브 / 액티브 분리.
            var passive = new List<BattleViewModel.BuildEntry>();
            var active  = new List<BattleViewModel.BuildEntry>();
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e == null || e.Card == null) continue;
                    if (e.IsPassive) passive.Add(e);
                    else             active.Add(e);
                }
            }

            //# 패시브 — 카테고리 그룹화 (Enhance → Spawn → Replace → Environment), 그룹 내 픽 시간 순.
            passive.Sort((a, b) =>
            {
                int oa = CategoryOrder(a.Card.Category);
                int ob = CategoryOrder(b.Card.Category);
                return oa.CompareTo(ob);
            });

            //# 액티브 — 픽 시간 순 (리스트 추가 순서 그대로 유지). 별도 정렬 안 함.

            //# 빈 상태 라벨.
            if (_passiveEmptyText != null) _passiveEmptyText.gameObject.SetActive(passive.Count == 0);
            if (_activeEmptyText  != null) _activeEmptyText.gameObject.SetActive(active.Count == 0);

            //# 셀 인스턴스화.
            FillSection(_passiveContent, passive);
            FillSection(_activeContent,  active);
        }

        private void FillSection(Transform content, List<BattleViewModel.BuildEntry> entries)
        {
            if (content == null || _cellPrefab == null) return;
            foreach (var e in entries)
            {
                var poolable = CHMPool.Instance.Pop(_cellPrefab, content);
                if (poolable == null) continue;
                _spawnedCells.Add(poolable.gameObject);
                var cell = poolable.GetComponent<BuildModalCardCell>();
                if (cell != null) cell.Bind(e.Card, e.Count);
            }
        }

        private static int CategoryOrder(ECardCategory c) => c switch
        {
            ECardCategory.Enhance     => 0,
            ECardCategory.Spawn       => 1,
            ECardCategory.Replace     => 2,
            ECardCategory.Environment => 3,
            _                         => 99,
        };
    }

    //# 모달 카드 셀 — 프레임 + 이름 + ×N + 설명 한 줄.
    //# 모달 외부에서도 같은 구조를 안 쓰니 같은 파일에 둠 (Rule 13 의 관련 코드 응집 정신).
    public class BuildModalCardCell : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _countText;
        [SerializeField] private CHText _descText;

        //# ×N 노랑 (#FBBF24).
        private static readonly Color CountColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        //# 설명 회색 (#D1D5DB).
        private static readonly Color DescColor  = new Color(0.820f, 0.835f, 0.859f, 1f);

        //# 풀 재사용 시 상태 리셋 (Rule 12).
        private void OnEnable()
        {
            if (_countText != null) _countText.gameObject.SetActive(false);
        }

        public void Bind(CardData card, int count)
        {
            if (card == null) return;
            if (_frame != null) _frame.color = CardView.CategoryColor(card.Category);
            if (_nameText != null) _nameText.SetText(card.DisplayName);
            if (_descText != null)
            {
                _descText.SetText(card.Description);
                _descText.SetColor(DescColor);
            }
            if (_countText != null)
            {
                bool show = count >= 2;
                _countText.gameObject.SetActive(show);
                if (show)
                {
                    _countText.SetText($"×{count}");
                    _countText.SetColor(CountColor);
                }
            }
        }
    }
}
