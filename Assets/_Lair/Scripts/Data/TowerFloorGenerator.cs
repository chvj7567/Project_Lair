using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 구간 하나(5층)의 구성 — 영웅 5명 + 일반 템플릿 4개 + 보스 템플릿 1개 (infinite-tower §3.5).
    public sealed class TowerBand
    {
        public int Band;
        public string[] Heroes;
        public TowerTemplateKey[] Normals;
        public TowerTemplateKey Boss;
    }

    //# 스테이지 규칙 묶음 템플릿의 키 — (chapterId, stageIndex) 오름차순이 정렬 기준.
    public readonly struct TowerTemplateKey : IEquatable<TowerTemplateKey>
    {
        public readonly int ChapterId;
        public readonly int StageIndex;

        public TowerTemplateKey(int chapterId, int stageIndex)
        {
            ChapterId = chapterId;
            StageIndex = stageIndex;
        }

        public bool Equals(TowerTemplateKey other)
        {
            return ChapterId == other.ChapterId && StageIndex == other.StageIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is TowerTemplateKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ChapterId * 31 + StageIndex;
        }
    }

    //# 난수 SplitMix64 — System.Random / UnityEngine.Random 은 구현별 시퀀스가 달라 랭킹 공정성을 깨므로 쓰지 않는다 (infinite-tower §3.5).
    public struct TowerRandom
    {
        private ulong _state;

        public TowerRandom(ulong state)
        {
            _state = state;
        }

        public ulong Next()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }

    //# 층 구성 결정 함수 — (시드, 구간) 만으로 정해지는 순수 정적 함수. Unity 비의존, 골든 테스트 대상 (infinite-tower §3.5).
    public static class TowerFloorGenerator
    {
        public const int FloorsPerBand = 5;
        private const int NormalPerBand = FloorsPerBand - 1;
        private const ulong SaltHero = 1UL;
        private const ulong SaltNormal = 2UL;
        private const ulong SaltBoss = 3UL;

        //# 층 → 구간(1~) / 구간 내 위치(1~5). floor < 1 은 1 로 취급.
        public static int BandOf(int floor)
        {
            return (Math.Max(1, floor) - 1) / FloorsPerBand + 1;
        }

        public static int PositionOf(int floor)
        {
            return (Math.Max(1, floor) - 1) % FloorsPerBand + 1;
        }

        public static bool IsBossFloor(int floor)
        {
            return PositionOf(floor) == FloorsPerBand;
        }

        //# 구간 첫 층 = 5(b-1)+1.
        public static int FirstFloorOfBand(int band)
        {
            return FloorsPerBand * (band - 1) + 1;
        }

        //# 구간 band 의 구성. prev 는 구간 band-1 의 결과(band == 1 이면 null). 입력 목록은 이 함수가 ordinal 정렬하므로 JSON 행 순서와 무관.
        //# heroIds: 탑 등장 영웅(5명 이상), templates: (키, minFloor) 전부.
        public static TowerBand ComputeBand(int seed, int band, TowerBand prev, IReadOnlyList<string> heroIds, IReadOnlyList<KeyValuePair<TowerTemplateKey, int>> templates)
        {
            List<string> heroes = new List<string>(heroIds);
            heroes.Sort(string.CompareOrdinal);
            Shuffle(heroes, Stream(seed, band, SaltHero));
            string[] picked = heroes.GetRange(0, FloorsPerBand).ToArray();
            if (prev != null && picked[0] == prev.Heroes[FloorsPerBand - 1])
            {
                string swap = picked[0];
                picked[0] = picked[1];
                picked[1] = swap;
            }

            int firstFloor = FirstFloorOfBand(band);
            int lastFloor = firstFloor + FloorsPerBand - 1;
            List<TowerTemplateKey> normalPool = new List<TowerTemplateKey>();
            List<TowerTemplateKey> bossPool = new List<TowerTemplateKey>();
            foreach (KeyValuePair<TowerTemplateKey, int> template in templates)
            {
                bool isBoss = template.Key.StageIndex == FloorsPerBand;
                if (isBoss)
                {
                    if (template.Value <= lastFloor)
                    {
                        bossPool.Add(template.Key);
                    }
                }
                else if (template.Value <= firstFloor)
                {
                    normalPool.Add(template.Key);
                }
            }
            normalPool.Sort(CompareKey);
            bossPool.Sort(CompareKey);

            Shuffle(normalPool, Stream(seed, band, SaltNormal));
            Shuffle(bossPool, Stream(seed, band, SaltBoss));

            TowerTemplateKey boss = bossPool[0];
            if (prev != null && boss.Equals(prev.Boss) && bossPool.Count >= 2)
            {
                boss = bossPool[1];
            }
            return new TowerBand
            {
                Band = band,
                Heroes = picked,
                Normals = normalPool.GetRange(0, NormalPerBand).ToArray(),
                Boss = boss,
            };
        }

        //# 스트림 초기 state = (ulong)(uint)seed ^ ((ulong)band << 32) ^ salt.
        private static TowerRandom Stream(int seed, int band, ulong salt)
        {
            return new TowerRandom((ulong)(uint)seed ^ ((ulong)band << 32) ^ salt);
        }

        //# Fisher-Yates, i = n-1 → 1, j = next() % (i+1).
        private static void Shuffle<T>(List<T> list, TowerRandom rng)
        {
            for (int i = list.Count - 1; i >= 1; --i)
            {
                int j = (int)(rng.Next() % (ulong)(i + 1));
                T swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }

        private static int CompareKey(TowerTemplateKey a, TowerTemplateKey b)
        {
            int byChapter = a.ChapterId.CompareTo(b.ChapterId);
            return byChapter != 0 ? byChapter : a.StageIndex.CompareTo(b.StageIndex);
        }

        //# 층 배율 floorHp / floorPower (infinite-tower §3.2) — 복리를 decimal 로 누적해 float pow 드리프트를 피한다. multiplierCap 으로 절단.
        public static decimal FloorMultiplier(float baseValue, float growthPerFloor, float bossMul, float cap, int floor, bool isBoss)
        {
            decimal capValue = (decimal)cap;
            decimal value = (decimal)baseValue;
            decimal growth = 1m + (decimal)growthPerFloor;
            for (int i = 1; i < floor && growth > 1m; ++i)
            {
                value *= growth;
                if (value >= capValue)
                {
                    value = capValue;
                    break;
                }
            }
            if (isBoss)
            {
                value *= (decimal)bossMul;
            }
            return value > capValue ? capValue : value;
        }
    }
}
