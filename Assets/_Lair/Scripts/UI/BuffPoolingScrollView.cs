using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# 툴팁 본문의 강화 줄 리스트 — CHPoolingScrollView<BuffLine, AppliedBuff>.
    //# Rule 11 v0.8 — `ScrollRect + 수동 풀링 → CHPoolingScrollView` 완전 적용.
    //# 호출부(SpawnerStatusTooltip) 가 SetItemList 전에 SetContext(type, balance) 로
    //# 종 + balance 를 주입 — InitItem 안에서 BuffLine.Bind 호출에 그대로 전달.
    public class BuffPoolingScrollView
        : CHPoolingScrollView<BuffLine, BattleViewModel.AppliedBuff>
    {
        //# Bind 컨텍스트 — 매 SetItemList 호출 직전 SpawnerStatusTooltip 이 setter 호출.
        private EMonster _currentType;
        private BalanceConfig _balance;

        //# 호출부 — SetItemList 전에 1회 호출하여 종 + balance 주입.
        public void SetContext(EMonster type, BalanceConfig balance)
        {
            _currentType = type;
            _balance = balance;
        }

        public override void InitItem(BuffLine item, BattleViewModel.AppliedBuff data, int index)
        {
            if (item == null) return;
            item.Bind(data, _currentType, _balance);
        }

        //# 풀 인스턴스 1회 초기화. BuffLine 의 OnEnable 이 ×N 배지를 reset.
        public override void InitPoolingObject(BuffLine item)
        {
        }
    }
}
