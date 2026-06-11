#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Lair.Data;

namespace Lair.Card
{
    //# 에디터 전용 디버그 — 덱 종류별 픽 순번에 지정 카드를 3택1 선택지에 강제 포함 (선택지 등장만, 자동 적용 아님).
    //# 빌드 미포함 (#if UNITY_EDITOR). 토글은 LairBalanceWindow — 기본 OFF.
    public class ForcedCardDraw
    {
        private const string EnabledKey = "Lair.ForcedCardDraw.Enabled";

        //# SessionState 백업 — 플레이 진입 도메인 리로드 생존, 에디터 재시작 시 기본 OFF.
        public static bool Enabled
        {
            get => UnityEditor.SessionState.GetBool(EnabledKey, false);
            set => UnityEditor.SessionState.SetBool(EnabledKey, value);
        }

        //# 스크립트 — (패시브 여부, 픽 순번 1-base) → 강제 등장 카드. 테스트용 3건 하드코딩 (YAGNI — 설정화 불요).
        private static readonly (bool isPassive, int pickIndex, ECardId id)[] Script =
        {
            (false, 1, ECardId.TimeStop),
            (false, 2, ECardId.Fear),
            (true,  1, ECardId.HeroPoisonAura),
        };

        private int _activeDrawCount;
        private int _passiveDrawCount;

        //# LairBalanceWindow 상태 표시용 스크립트 요약.
        public static string DescribeScript()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach ((bool isPassive, int pickIndex, ECardId id) entry in Script)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append($"{(entry.isPassive ? "패시브" : "액티브")} {entry.pickIndex}픽: {entry.id}");
            }
            return sb.ToString();
        }

        //# 정상 드로우 결과에 스크립트 카드 주입. 호출마다 덱 종류별 픽 순번 1 증가 (토글 OFF 여도 순번은 진행).
        //# 무변경 반환: 토글 OFF / 스크립트 없음·소진 / 캡 도달 / 이미 포함 / 풀 미발견. 치환은 마지막 슬롯 1장.
        public IReadOnlyList<CardData> Apply(
            IReadOnlyList<CardData> drawn,
            bool isPassive,
            IReadOnlyList<CardData> allCards,
            Func<ECardId, bool> isCapped)
        {
            int pickIndex = isPassive ? ++_passiveDrawCount : ++_activeDrawCount;
            if (Enabled == false)
                return drawn;
            if (drawn == null || drawn.Count == 0)
                return drawn;

            if (TryGetForcedId(isPassive, pickIndex, out ECardId forcedId) == false)
                return drawn;

            //# 3픽 캡 도달 카드는 강제 등장 스킵 — 정상 드로우 유지.
            if (isCapped != null && isCapped(forcedId))
                return drawn;

            //# 이미 정상 드로우에 포함 — 치환 불요 (중복 카드 2장 노출 금지).
            for (int i = 0; i < drawn.Count; ++i)
            {
                if (drawn[i] != null && drawn[i].Id == forcedId)
                    return drawn;
            }

            CardData forced = FindCard(allCards, forcedId);
            if (forced == null)
                return drawn;

            List<CardData> result = new List<CardData>(drawn);
            result[result.Count - 1] = forced;
            return result;
        }

        private static bool TryGetForcedId(bool isPassive, int pickIndex, out ECardId id)
        {
            foreach ((bool isPassive, int pickIndex, ECardId id) entry in Script)
            {
                if (entry.isPassive == isPassive && entry.pickIndex == pickIndex)
                {
                    id = entry.id;
                    return true;
                }
            }
            id = default;
            return false;
        }

        private static CardData FindCard(IReadOnlyList<CardData> allCards, ECardId id)
        {
            if (allCards == null)
                return null;
            for (int i = 0; i < allCards.Count; ++i)
            {
                if (allCards[i] != null && allCards[i].Id == id)
                    return allCards[i];
            }
            return null;
        }
    }
}
#endif
