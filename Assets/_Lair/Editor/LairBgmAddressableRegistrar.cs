using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Bgm.mp3 를 Addressables 'Sound' 그룹에 등록 (address="Bgm", label="Resource").
    //# 메인 오케스트레이터가 UnityMCP 로 메뉴 실행. CHMResource.Init 라벨("Resource")과 정확히 일치해야 로드됨.
    public static class LairBgmAddressableRegistrar
    {
        public const string SoundGroupName = "Sound";
        //# 스키마 템플릿 소스 — 기존 'Resource' 그룹의 로컬 번들 설정을 복제(silent clip=null 방지).
        public const string TemplateGroupName = "Resource";
        //# CHMResource.DefaultLabel 과 동일. SaveLocationInfo 가 이 라벨로만 키 인덱싱.
        public const string ResourceLabel = "Resource";
        public const string BgmAssetPath = "Assets/_Lair/Art/Sound/Bgm.mp3";
        public const string BgmAddress = "Bgm";

        [MenuItem("Lair/Register Bgm Addressable")]
        public static void RegisterBgm()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairBgmAddressableRegistrar] Addressables 미설정 — Window > Asset Management > Addressables Groups 로 초기화 필요");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(BgmAssetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[LairBgmAddressableRegistrar] 에셋 미발견(guid 없음): {BgmAssetPath} — Bgm.mp3 임포트 후 재실행");
                return;
            }

            //# Sound 그룹 확보 — 없으면 Resource 그룹 스키마를 복제해 생성(빌드 포함 보장).
            AddressableAssetGroup soundGroup = settings.FindGroup(SoundGroupName);
            if (soundGroup == null)
            {
                soundGroup = CreateSoundGroup(settings);
                if (soundGroup == null)
                    return;
            }

            //# 엔트리 생성/이동 + 주소·라벨 — 기존 캐릭터 빌더와 동일 패턴.
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, soundGroup, readOnly: false, postEvent: false);
            entry.address = BgmAddress;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LairBgmAddressableRegistrar] 등록 완료 (group={SoundGroupName}, address={entry.address}, label={ResourceLabel})");
        }

        //# 'Resource' 그룹의 스키마를 그대로 복제해 'Sound' 그룹 생성.
        //# 스키마 없는 그룹은 콘텐츠 빌드에서 제외되어 런타임 Load 가 조용히 null 이 됨 → 명시적 복제로 회피.
        private static AddressableAssetGroup CreateSoundGroup(AddressableAssetSettings settings)
        {
            AddressableAssetGroup template = settings.FindGroup(TemplateGroupName);
            if (template == null)
            {
                Debug.LogError($"[LairBgmAddressableRegistrar] 스키마 템플릿 그룹 '{TemplateGroupName}' 미발견 — 'Sound' 그룹을 안전하게 생성할 수 없음");
                return null;
            }

            //# template.Schemas 를 넘기면 CreateGroup 이 각 스키마를 Instantiate 복제(공유 아님) → 로컬 번들 설정 그대로.
            AddressableAssetGroup group = settings.CreateGroup(
                SoundGroupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: template.Schemas,
                types: new System.Type[0]);

            if (group == null)
            {
                Debug.LogError("[LairBgmAddressableRegistrar] 'Sound' 그룹 생성 실패");
            }

            return group;
        }
    }
}
