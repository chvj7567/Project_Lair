using System.Collections.Generic;
using Lair.Character;
using Lair.Data;

namespace Lair.Village
{
    //# 등급 이펙트 슬롯 한 세트 확보/반환 경계(monster-codex §5.4) — 테스트 더블 교체용. 구현 = GradeFxBudgetSlots.
    public interface ICodexFxSlots
    {
        bool TryAcquire(int grade);
        void Release(int grade);
    }

    //# 칸 이펙트 애니 상한 배분기 — 등급 높은 칸 → 같으면 티어 낮은 칸 순으로 liveCap 칸까지 슬롯을 준다. 상세 미리보기는 여기 포함하지 않는다(프리팹이 먼저 자기 슬롯을 확보).
    public sealed class CodexFxAllocator
    {
        public readonly struct Candidate
        {
            public readonly int Key;
            public readonly int Grade;
            public readonly int Tier;

            public Candidate(int key, int grade, int tier)
            {
                Key = key;
                Grade = grade;
                Tier = tier;
            }
        }

        private readonly ICodexFxSlots _slots;
        private readonly int _liveCap;
        private readonly List<int> _heldGrades = new List<int>();

        public CodexFxAllocator(ICodexFxSlots slots, int liveCap)
        {
            _slots = slots;
            _liveCap = liveCap;
        }

        public int LiveCount => _heldGrades.Count;

        //# 후보(눈에 보이는 칸만)로 슬롯을 다시 배분 — 이전 슬롯을 전부 반환하고 우선순위대로 재확보. 반환값 = 애니 칸 key 집합. 2등급 미만은 후보 아님.
        public HashSet<int> Rebalance(IReadOnlyList<Candidate> candidates)
        {
            ReleaseAll();
            HashSet<int> live = new HashSet<int>();
            if (_slots == null || candidates == null)
                return live;
            List<Candidate> sorted = new List<Candidate>();
            foreach (Candidate candidate in candidates)
            {
                if (candidate.Grade > MonsterGrades.MinGrade)
                {
                    sorted.Add(candidate);
                }
            }
            sorted.Sort((a, b) =>
            {
                int byGrade = b.Grade.CompareTo(a.Grade);
                if (byGrade != 0)
                    return byGrade;
                int byTier = a.Tier.CompareTo(b.Tier);
                return byTier != 0 ? byTier : a.Key.CompareTo(b.Key);
            });
            foreach (Candidate candidate in sorted)
            {
                if (live.Count >= _liveCap)
                    break;
                if (_slots.TryAcquire(candidate.Grade))
                {
                    live.Add(candidate.Key);
                    _heldGrades.Add(candidate.Grade);
                }
            }
            return live;
        }

        //# 팝업 닫힘·탭 전환·종족 전환 — 전부 반환(GradeFxBudget 잔량이 열기 전으로 복귀).
        public void ReleaseAll()
        {
            foreach (int grade in _heldGrades)
            {
                _slots?.Release(grade);
            }
            _heldGrades.Clear();
        }
    }

    //# GradeFxBudget 어댑터 — 그 등급 몬스터 한 마리가 쓰는 만큼(오라 1 + 불꽃 기둥 n + 후광 m)을 전부 얻거나 전부 못 얻는다. 쇼케이스와 같은 정적 카운터를 공유한다.
    public sealed class GradeFxBudgetSlots : ICodexFxSlots
    {
        private static void CostOf(int grade, out int aura, out int tornado, out int sunburst)
        {
            aura = 0;
            tornado = 0;
            sunburst = 0;
            GradeFxDatabase db = GradeFxDatabase.Current;
            if (db == null)
                return;
            GradeFxSpec spec = db.SpecOf(EMonster.Wisp, grade);
            aura = spec.HasAura ? 1 : 0;
            foreach (Grade_FxRow row in spec.Fx)
            {
                if (row.kind == GradeFxDatabase.KindTornado)
                {
                    tornado++;
                }
                else
                {
                    sunburst++;
                }
            }
        }

        public bool TryAcquire(int grade)
        {
            CostOf(grade, out int aura, out int tornado, out int sunburst);
            if (aura + tornado + sunburst == 0)
                return false;
            int gotAura = 0;
            int gotTornado = 0;
            int gotSunburst = 0;
            bool ok = true;
            for (; gotAura < aura && ok; ++gotAura)
            {
                ok = GradeFxBudget.TryAcquireAura();
                if (ok == false)
                    break;
            }
            for (; ok && gotTornado < tornado; ++gotTornado)
            {
                ok = GradeFxBudget.TryAcquireTornado();
                if (ok == false)
                    break;
            }
            for (; ok && gotSunburst < sunburst; ++gotSunburst)
            {
                ok = GradeFxBudget.TryAcquireSunburst();
                if (ok == false)
                    break;
            }
            if (ok)
                return true;
            for (int i = 0; i < gotAura; ++i)
            {
                GradeFxBudget.ReleaseAura();
            }
            for (int i = 0; i < gotTornado; ++i)
            {
                GradeFxBudget.ReleaseTornado();
            }
            for (int i = 0; i < gotSunburst; ++i)
            {
                GradeFxBudget.ReleaseSunburst();
            }
            return false;
        }

        public void Release(int grade)
        {
            CostOf(grade, out int aura, out int tornado, out int sunburst);
            for (int i = 0; i < aura; ++i)
            {
                GradeFxBudget.ReleaseAura();
            }
            for (int i = 0; i < tornado; ++i)
            {
                GradeFxBudget.ReleaseTornado();
            }
            for (int i = 0; i < sunburst; ++i)
            {
                GradeFxBudget.ReleaseSunburst();
            }
        }
    }
}
