using System;
using Lair.Data;
using Lair.Meta;

namespace Lair.Village
{
    //# 교환 버튼 하나 표시 데이터 — View 는 그대로 표시만 한다.
    public class ShardExchangeButtonData
    {
        public ECurrency To;
        public string LabelText;     //# "3개 → 각성석 1개"
        public string ColorHex;      //# 목표 재화 색 (교환 가능 테두리)
        public bool CanExchange;
        public bool IsLocked;
    }

    //# 잔해 교환 줄 표시 데이터 (monster-growth-currencies §8.3).
    public class ShardExchangeRowData
    {
        public bool Visible;          //# 잔해를 한 번이라도 얻은 뒤부터
        public string HeaderText;     //# "잔해 교환"
        public string ShardLabelText; //# "유물 잔해"
        public string ShardCountText;
        public ShardExchangeButtonData[] Buttons = new ShardExchangeButtonData[0];
    }

    //# 유물 도감 잔해 교환 줄 ViewModel — 교환 가능·잠김 판단과 토스트 문구를 소유하고 View 는 표시·입력만 (Rule 02 §6).
    public class ShardExchangeViewModel
    {
        private static readonly ECurrency[] Targets = { ECurrency.AwakenStone, ECurrency.MoonSeal, ECurrency.AbyssCrystal };

        private readonly MetaProfile _profile;
        private readonly GrowthDatabase _growth;
        private readonly Func<ECurrency, bool> _isUnlocked;
        private readonly Func<int, string> _text;

        public event Action OnChanged;

        public ShardExchangeViewModel(MetaProfile profile, GrowthDatabase growth, Func<ECurrency, bool> isUnlocked, Func<int, string> text)
        {
            _profile = profile;
            _growth = growth;
            _isUnlocked = isUnlocked;
            _text = text;
        }

        public ShardExchangeRowData Build()
        {
            ShardExchangeRowData row = new ShardExchangeRowData();
            if (_profile == null || _growth == null || _profile.GetCurrencyTotalEarned(ECurrency.RelicShard) < 1)
                return row;
            row.Visible = true;
            row.HeaderText = Text(GrowthStrings.ExchangeHeader);
            row.ShardLabelText = GrowthText.CurrencyName(ECurrency.RelicShard, _growth, _text);
            row.ShardCountText = _profile.GetCurrency(ECurrency.RelicShard).ToString("N0");
            row.Buttons = new ShardExchangeButtonData[Targets.Length];
            for (int i = 0; i < Targets.Length; ++i)
            {
                ECurrency to = Targets[i];
                Currency_ExchangeRow ex = _growth.FindExchange(ECurrency.RelicShard, to);
                EExchangeResult check = CurrencyExchange.Check(_profile, _growth, to, _isUnlocked);
                row.Buttons[i] = new ShardExchangeButtonData
                {
                    To = to,
                    LabelText = ex != null ? string.Format(Text(GrowthStrings.ExchangeButton), ex.fromAmount, GrowthText.CurrencyName(to, _growth, _text)) : string.Empty,
                    ColorHex = GrowthText.CurrencyColorHex(to, _growth),
                    CanExchange = check == EExchangeResult.Ok,
                    IsLocked = check == EExchangeResult.Locked || check == EExchangeResult.Unavailable,
                };
            }
            return row;
        }

        //# 1탭 1회 교환(E-12). 실패면 토스트 문구, 성공이면 null (숫자 변화가 피드백 — 토스트 없음).
        public string Exchange(ECurrency to)
        {
            EExchangeResult result = CurrencyExchange.Exchange(_profile, _growth, to, _isUnlocked);
            switch (result)
            {
                case EExchangeResult.Ok:
                    OnChanged?.Invoke();
                    return null;
                case EExchangeResult.Insufficient:
                    return Text(GrowthStrings.ShardShort);
                case EExchangeResult.Locked:
                    return string.Format(Text(GrowthStrings.ExchangeLocked), GrowthText.CurrencyName(to, _growth, _text));
                default:
                    return null;
            }
        }

        private string Text(int id) => _text != null ? _text(id) : string.Empty;
    }
}
