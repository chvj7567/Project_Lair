using System;
using System.IO;
using UnityEngine;

namespace Lair.Meta
{
    //# 로컬 JSON 세이브 — 기본 경로 Application.persistentDataPath (Android dataPath 는 읽기 전용 — 과거 사고 재발 방지).
    //# 테스트는 임시 폴더 주입. 모든 IO 는 try-catch — 세이브 실패가 게임 흐름을 끊지 않는다.
    public class MetaProfileStore
    {
        public const string FileName = "meta_profile.json";

        private readonly string _directory;

        public MetaProfileStore(string directory = null)
        {
            _directory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        private string FilePath => Path.Combine(_directory, FileName);

        //# 파일 없음 / 파싱 실패 → new MetaProfile() 폴백.
        public MetaProfile Load()
        {
            try
            {
                if (File.Exists(FilePath) == false)
                    return new MetaProfile();

                MetaProfile profile = JsonUtility.FromJson<MetaProfile>(File.ReadAllText(FilePath));
                return profile ?? new MetaProfile();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MetaProfileStore] 로드 실패 — 새 프로필로 폴백: {e.Message}");
                return new MetaProfile();
            }
        }

        public void Save(MetaProfile profile)
        {
            if (profile == null)
                return;
            try
            {
                Directory.CreateDirectory(_directory);
                File.WriteAllText(FilePath, JsonUtility.ToJson(profile, prettyPrint: true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MetaProfileStore] 저장 실패: {e.Message}");
            }
        }
    }
}
