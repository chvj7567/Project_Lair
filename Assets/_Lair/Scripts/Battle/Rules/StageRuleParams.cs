using System;
using System.Collections.Generic;

namespace Lair.Battle
{
    //# Stage_Rule_Param 의 key-value 읽기 전용 뷰 (chapter-system §3.1). 키 누락은 기본값 보정 없이 예외 — 데이터 오타 조기 발견.
    //# ChapterDatabase 가 필수 키 집합을 이미 검증하므로 정상 데이터에서는 던지지 않는다.
    public readonly struct StageRuleParams
    {
        private readonly IReadOnlyDictionary<string, float> _map;

        public StageRuleParams(IReadOnlyDictionary<string, float> map)
        {
            _map = map;
        }

        public float Get(string key)
        {
            if (_map != null && _map.TryGetValue(key, out float value))
                return value;
            throw new KeyNotFoundException($"스테이지 규칙 파라미터 누락: {key}");
        }

        public int GetInt(string key)
        {
            return (int)Math.Round(Get(key));
        }
    }
}
