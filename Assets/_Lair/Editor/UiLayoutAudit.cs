using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lair.Card;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# UI 텍스트 레이아웃 점검 일회용 툴 (Rule 04 §3, 결과 확인 후 삭제). Art/UI 프리팹 + DamagePopup 을 1280x720 기준 비활성 홀더 아래에 인스턴스화해
    //# TMP 텍스트의 세로/가로 넘침과 텍스트끼리 겹침을 "최악 케이스 샘플 텍스트"로 측정하고 Library/ui-layout-audit.txt 에 기록한다.
    //# 인스턴스는 비활성 홀더 아래에 있어 Awake/OnEnable 이 돌지 않는다 → 레이아웃 그룹이 만드는 위치는 반영되지 않고 프리팹에 저장된 값을 쓴다.
    //# 측정은 프리팹 텍스트의 설정을 복사한 별도 TMP 객체로 하며(활성 오브젝트), 끝나면 모두 파괴한다.
    public static class UiLayoutAudit
    {
        private const string OutputPath = "Library/ui-layout-audit.txt";
        private const string UiDir = "Assets/_Lair/Art/UI";
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;
        private const float OverflowTolerance = 0.5f;
        private const float OverlapMinArea = 1f;

        //# 카드 데이터에서 가장 긴 이름/설명 — 샘플에 반영(없으면 고정 샘플).
        private static string _longestCardName = "도깨비불 대군";
        private static string _longestCardDesc = "도깨비불·망령 받는 데미지 -50% (15초) 및 처치 시 주변 회복";

        private class Issue
        {
            public string Prefab;
            public string Path;
            public string Kind;        //# 겹침 / 세로 넘침 / 가로 넘침 / 자동 축소 넘침
            public float Severity;     //# 정렬용 — 겹침 높이 또는 초과량(ref)
            public string Detail;
        }

        private class TextInfo
        {
            public TMP_Text Text;
            public string Path;
            public bool DefaultActive;
            public string Worst;
            public Vector2 WorstPreferred;
            public Rect Extent;
        }

        [MenuItem("Lair/UI/Audit Layout")]
        public static void Run()
        {
            LoadCardSamples();
            List<string> paths = CollectPrefabPaths();
            List<Issue> issues = new List<Issue>();
            StringBuilder detail = new StringBuilder();

            GameObject holder = new GameObject("__UiLayoutAuditHolder", typeof(RectTransform));
            holder.hideFlags = HideFlags.HideAndDontSave;
            holder.SetActive(false);
            RectTransform holderRect = (RectTransform)holder.transform;
            holderRect.anchorMin = Vector2.zero;
            holderRect.anchorMax = Vector2.zero;
            holderRect.pivot = new Vector2(0.5f, 0.5f);
            holderRect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);

            GameObject measureObject = new GameObject("__UiLayoutAuditMeasure", typeof(RectTransform));
            measureObject.hideFlags = HideFlags.HideAndDontSave;
            TextMeshProUGUI measure = measureObject.AddComponent<TextMeshProUGUI>();

            int textCount = 0;
            try
            {
                for (int i = 0; i < paths.Count; ++i)
                {
                    EditorUtility.DisplayProgressBar("UI 레이아웃 점검", paths[i], (float)i / paths.Count);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                    if (prefab == null)
                        continue;

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
                    try
                    {
                        textCount += AuditPrefab(prefab.name, instance, measure, issues, detail);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                UnityEngine.Object.DestroyImmediate(measureObject);
                UnityEngine.Object.DestroyImmediate(holder);
            }

            WriteReport(paths.Count, textCount, issues, detail);
        }

        //# ---------------- 수집 ----------------

        private static List<string> CollectPrefabPaths()
        {
            List<string> paths = new List<string>();
            string[] uiGuids = AssetDatabase.FindAssets("t:Prefab", new[] { UiDir });
            for (int i = 0; i < uiGuids.Length; ++i)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(uiGuids[i]));
            }

            string[] fxGuids = AssetDatabase.FindAssets("DamagePopup t:Prefab", new[] { "Assets/_Lair/Art/FX" });
            for (int i = 0; i < fxGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(fxGuids[i]);
                if (Path.GetFileNameWithoutExtension(path) == "DamagePopup")
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        private static void LoadCardSamples()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            for (int i = 0; i < guids.Length; ++i)
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (card == null)
                    continue;
                if (string.IsNullOrEmpty(card.DisplayName) == false && card.DisplayName.Length > _longestCardName.Length)
                {
                    _longestCardName = card.DisplayName;
                }

                if (string.IsNullOrEmpty(card.Description) == false && card.Description.Length > _longestCardDesc.Length)
                {
                    _longestCardDesc = card.Description;
                }
            }
        }

        //# ---------------- 프리팹 점검 ----------------

        private static int AuditPrefab(string prefabName, GameObject instance, TextMeshProUGUI measure, List<Issue> issues, StringBuilder detail)
        {
            TMP_Text[] texts = instance.GetComponentsInChildren<TMP_Text>(true);
            List<TextInfo> infos = new List<TextInfo>();
            detail.AppendLine($"## {prefabName}  (TMP {texts.Length}개)");

            for (int i = 0; i < texts.Length; ++i)
            {
                TMP_Text text = texts[i];
                string path = PathOf(text.transform, instance.transform);
                bool defaultActive = IsActiveWithin(text.transform, instance.transform);
                List<string> samples = SamplesFor(prefabName, text.gameObject.name, text.text);

                TextInfo info = new TextInfo { Text = text, Path = path, DefaultActive = defaultActive, Worst = text.text };
                float worstHeight = -1f;
                for (int s = 0; s < samples.Count; ++s)
                {
                    Vector2 preferred = MeasureText(measure, text, samples[s]);
                    CheckOverflow(prefabName, path, text, samples[s], preferred, issues);
                    if (preferred.y > worstHeight)
                    {
                        worstHeight = preferred.y;
                        info.Worst = samples[s];
                        info.WorstPreferred = preferred;
                    }
                }

                if (samples.Count > 0)
                {
                    info.Extent = ExtentWorld(text, info.WorstPreferred);
                    infos.Add(info);
                }

                detail.AppendLine($"  - {path}{(defaultActive ? string.Empty : " [기본 꺼짐]")}  \"{Short(text.text)}\"  {Describe(text)}  "
                    + $"rect={text.rectTransform.rect.width:0.#}x{text.rectTransform.rect.height:0.#}  worst=\"{Short(info.Worst)}\" pref={info.WorstPreferred.x:0.#}x{info.WorstPreferred.y:0.#}");
            }

            CheckOverlaps(prefabName, infos, issues);
            detail.AppendLine();
            return texts.Length;
        }

        private static string Describe(TMP_Text t)
        {
#pragma warning disable 618
            bool wrap = t.enableWordWrapping;
#pragma warning restore 618
            string size = t.enableAutoSizing ? $"auto {t.fontSizeMin:0.#}~{t.fontSizeMax:0.#}" : $"{t.fontSize:0.#}";
            return $"size={size} lineSp={t.lineSpacing:0.#} margin=({t.margin.x:0.#},{t.margin.y:0.#},{t.margin.z:0.#},{t.margin.w:0.#}) wrap={wrap} overflow={t.overflowMode} align={t.alignment}";
        }

        //# 프리팹 텍스트 설정을 복사한 측정용 TMP 로 (폭 제한 있는) 선호 크기를 잰다. 자동 축소는 min 크기로 잰다.
        private static Vector2 MeasureText(TextMeshProUGUI measure, TMP_Text source, string text)
        {
            measure.font = source.font;
            measure.fontSharedMaterial = source.fontSharedMaterial;
            measure.enableAutoSizing = false;
            measure.fontSize = source.enableAutoSizing ? source.fontSizeMin : source.fontSize;
            measure.fontStyle = source.fontStyle;
            measure.lineSpacing = source.lineSpacing;
            measure.characterSpacing = source.characterSpacing;
            measure.wordSpacing = source.wordSpacing;
            measure.paragraphSpacing = source.paragraphSpacing;
            measure.margin = source.margin;
            measure.richText = source.richText;
            measure.overflowMode = TextOverflowModes.Overflow;
#pragma warning disable 618
            measure.enableWordWrapping = source.enableWordWrapping;
#pragma warning restore 618
            float width = source.rectTransform.rect.width;
            measure.rectTransform.sizeDelta = new Vector2(width, source.rectTransform.rect.height);
            return measure.GetPreferredValues(text, width, 0f);
        }

        private static void CheckOverflow(string prefabName, string path, TMP_Text text, string sample, Vector2 preferred, List<Issue> issues)
        {
            Rect rect = text.rectTransform.rect;
#pragma warning disable 618
            bool wrap = text.enableWordWrapping;
#pragma warning restore 618
            string context = $"샘플 \"{Short(sample)}\" → 선호 {preferred.x:0.#}x{preferred.y:0.#} / rect {rect.width:0.#}x{rect.height:0.#}  {Describe(text)}";

            if (preferred.y > rect.height + OverflowTolerance)
            {
                string kind = text.enableAutoSizing ? "자동 축소 넘침(min 크기에서도 세로 초과)" : "세로 넘침";
                issues.Add(new Issue { Prefab = prefabName, Path = path, Kind = kind, Severity = preferred.y - rect.height, Detail = context });
            }

            if (wrap == false && preferred.x > rect.width + OverflowTolerance)
            {
                issues.Add(new Issue { Prefab = prefabName, Path = path, Kind = "가로 넘침(wrap 꺼짐)", Severity = preferred.x - rect.width, Detail = context });
            }
        }

        //# 텍스트 실제 범위(최악 샘플 선호 크기를 정렬 기준으로 배치)끼리 겹치는 쌍 — 기본 꺼짐 텍스트는 상태 전환용이라 별도 표기.
        private static void CheckOverlaps(string prefabName, List<TextInfo> infos, List<Issue> issues)
        {
            for (int a = 0; a < infos.Count; ++a)
            {
                for (int b = a + 1; b < infos.Count; ++b)
                {
                    float width = Mathf.Min(infos[a].Extent.xMax, infos[b].Extent.xMax) - Mathf.Max(infos[a].Extent.xMin, infos[b].Extent.xMin);
                    float height = Mathf.Min(infos[a].Extent.yMax, infos[b].Extent.yMax) - Mathf.Max(infos[a].Extent.yMin, infos[b].Extent.yMin);
                    if (width <= 0f || height <= 0f || width * height < OverlapMinArea)
                        continue;

                    bool bothActive = infos[a].DefaultActive && infos[b].DefaultActive;
                    issues.Add(new Issue
                    {
                        Prefab = prefabName,
                        Path = infos[a].Path + "  <->  " + infos[b].Path,
                        Kind = bothActive ? "겹침" : "겹침(기본 꺼짐 포함 — 상태 전환용 가능)",
                        Severity = bothActive ? height : height * 0.01f,
                        Detail = $"겹침 {width:0.#}x{height:0.#} (\"{Short(infos[a].Worst)}\" vs \"{Short(infos[b].Worst)}\")",
                    });
                }
            }
        }

        //# 월드 좌표 텍스트 범위 — 폭은 rect 로 제한(줄바꿈), 높이는 선호 높이 그대로(넘치면 rect 밖으로 그려짐). 정렬 기준으로 배치.
        private static Rect ExtentWorld(TMP_Text text, Vector2 preferred)
        {
            Vector3[] corners = new Vector3[4];
            text.rectTransform.GetWorldCorners(corners);
            Rect rect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            float scaleX = text.transform.lossyScale.x;
            float scaleY = text.transform.lossyScale.y;
            float width = Mathf.Min(preferred.x * scaleX, rect.width);
            float height = preferred.y * scaleY;

            int alignment = (int)text.alignment;
            float x = rect.center.x - width * 0.5f;
            if ((alignment & (int)HorizontalAlignmentOptions.Left) != 0)
            {
                x = rect.xMin;
            }
            else if ((alignment & (int)HorizontalAlignmentOptions.Right) != 0)
            {
                x = rect.xMax - width;
            }

            float y = rect.center.y - height * 0.5f;
            if ((alignment & (int)VerticalAlignmentOptions.Top) != 0)
            {
                y = rect.yMax - height;
            }
            else if ((alignment & (int)VerticalAlignmentOptions.Bottom) != 0)
            {
                y = rect.yMin;
            }

            return new Rect(x, y, width, height);
        }

        //# ---------------- 샘플 텍스트 ----------------

        private static List<string> SamplesFor(string prefabName, string objectName, string stored)
        {
            List<string> samples = new List<string>();
            string[] fixedSamples = FixedSamples(prefabName, objectName);
            if (fixedSamples != null)
            {
                samples.AddRange(fixedSamples);
            }

            if (string.IsNullOrEmpty(stored) == false)
            {
                samples.Add(stored);
                samples.Add(stored + " " + stored);
                samples.Add(stored + "\n" + stored);
            }

            return samples;
        }

        private static string[] FixedSamples(string prefab, string name)
        {
            string key = prefab + "/" + name;
            switch (key)
            {
                case "CardSelectionPopup/NameText":
                case "BuildModalCardCell/NameText":
                    return new[] { _longestCardName };
                case "CardSelectionPopup/DescText":
                case "BuildModalCardCell/DescText":
                    return new[] { _longestCardDesc };
                case "CardSelectionPopup/KindLabel":
                    return new[] { "액티브" };
                case "CardSelectionPopup/CountBadge":
                    return new[] { "2/3" };
                case "CardSelectionPopup/Subtitle":
                    return new[] { "패시브 · 영웅 HP 100% 도달", "액티브 · 150초 주기" };
                case "SynergyModalCell/DescText":
                    return new[] { "도깨비불·망령 받는 데미지 ×0.5 · 사신·저주술사 공격력 ×1.3" };
                case "SynergyModalCell/NextText":
                    return new[] { "다음: 사신·저주술사 공격력 ×1.3 · 공속 +30%" };
                case "SynergyModalCell/Label":
                    return new[] { "DEBUFF (7장)" };
                case "SynergyModalCell/TierBadge":
                    return new[] { "3/3" };
                case "BuildSynergyCell/Text":
                    return new[] { "DEBUFF", "SWARM" };
                case "SpawnerStatusCell/txtSpawnTime":
                    return new[] { "19.9s" };
                case "SpawnerStatusCell/CountText":
                    return new[] { "×12" };
                case "SpawnerStatusCell/SpeciesText":
                    return new[] { "도깨비불", "저주술사" };
                case "SpawnerStatusCell/BadgeText":
                    return new[] { "Lv 5" };
                case "BuildIconCell/CountText":
                case "BuildModalCardCell/CountText":
                    return new[] { "×9" };
                case "HpBar/txtHp":
                    return new[] { "12345/12345" };
                case "BattleHud/TimerText":
                    return new[] { "5:00" };
                case "VillageHud/DisplayNameText":
                    return new[] { "영주 #A12C", "가나다라마바사아자차카타" };
                case "VillageHud/LordLevelText":
                    return new[] { "영주 Lv 99" };
                case "VillageHud/SoulText":
                    return new[] { "1,234,567 소울" };
                case "VillageHud/RecordIntruderText":
                    return new[] { "기사 · 5단계" };
                case "VillageHud/RecordBestText":
                    return new[] { "12:34.5" };
                case "VillageHud/RecordWinsText":
                    return new[] { "123승 · 456판" };
                case "VillageHud/StageIndicatorText":
                    return new[] { "STAGE 5" };
                case "VillageHud/StageThreatText":
                    return new[] { "★★★★★" };
                case "VillageHud/StageLockHintText":
                    return new[] { "스테이지 4 클리어 필요" };
                case "ShopItemCell/NameText":
                    return new[] { "몬스터 공격력 강화" };
                case "ShopItemCell/DescText":
                    return new[] { "모든 몬스터의 공격력이 5% 증가합니다 (최대 5단계)" };
                case "ShopItemCell/PriceText":
                    return new[] { "12,345 소울" };
                case "ShopItemCell/LevelText":
                    return new[] { "Lv 5/5" };
                case "ShopItemCell/Label":
                    return new[] { "소울 부족", "만렙" };
                case "QuestCell/NameText":
                    return new[] { "망령 군단을 소환하라의 시간" };
                case "QuestCell/DescText":
                    return new[] { "한 판에 망령을 30마리 이상 소환하고 영웅을 처치한다" };
                case "QuestCell/RewardText":
                    return new[] { "+1,000 소울" };
                case "QuestCell/ProgressText":
                    return new[] { "1000/1000" };
                case "LordRewardCell/NameText":
                    return new[] { "??? — 추후 해금", "저주술사 합류 지하 창고 확장" };
                case "LordRewardCell/SubText":
                    return new[] { "12,345 XP 남음" };
                case "LordRewardCell/RewardText":
                    return new[] { "+1,000 소울" };
                case "CodexCell/NameText":
                    return new[] { "저주술사", "??? — 추후 해금" };
                case "CodexCell/LevelBadge":
                    return new[] { "Lv 5" };
                case "RecordsStageCell/BestText":
                    return new[] { "최단 12:34" };
                case "RecordsStageCell/WinText":
                    return new[] { "123승" };
                case "RecordsStageCell/RunRateText":
                    return new[] { "456판 · 100%" };
                case "RecordsStageCell/LockHintText":
                    return new[] { "스테이지 4 클리어 필요" };
                case "RecordsStageCell/ThreatText":
                    return new[] { "★★★★★" };
                case "RankingCell/RankText":
                    return new[] { "랭킹 없음" };
                case "RankingCell/NameText":
                    return new[] { "가나다라마바사아자차카타… (나)" };
                case "RankingCell/TimeText":
                    return new[] { "12:34.5" };
                case "RankingCell/HeroText":
                    return new[] { "Knight" };
                case "CloudPopup/DisplayNameText":
                    return new[] { "표시명: 가나다라마바사아자차카타" };
                case "CloudPopup/ConflictText":
                    return new[] { "다른 기기의 진행이 클라우드에 더 최신으로 저장되어 있습니다. 클라우드 데이터로 복원하면 지금 기기의 진행은 사라질 수 있습니다." };
                case "ConfirmPopup/Message":
                    return new[] { "클라우드 저장 데이터로 현재 진행을 덮어씁니다. 지금 기기의 진행이 사라질 수 있습니다. 복원할까요?" };
                case "ConfirmPopup/Title":
                    return new[] { "클라우드에서 복원" };
                case "ToastView/Message":
                    return new[] { "오프라인 상태입니다. 클라우드 기능을 사용할 수 없습니다." };
                case "ResultPopup/RewardText":
                    return new[] { "보상  소울 +12345 · XP +1234\n영주 레벨 업!  Lv 10  +1000 소울\n도전과제 달성!  망령 군단  +120 소울\n도전과제 달성!  시간의 지배자  +200 소울\n도전과제 달성!  저주 수집가  +80 소울\n외 2건 달성" };
                case "ResultPopup/ClearTimeText":
                    return new[] { "12:34.5" };
                case "ResultPopup/HeroHpText":
                    return new[] { "100%" };
                case "ResultPopup/SoulsText":
                    return new[] { "+12,345" };
                case "ResultPopup/XpText":
                    return new[] { "+1,234" };
                case "ResultPopup/SubText":
                    return new[] { "영웅이 던전을 돌파했다" };
                case "SkillUnlockBanner/Label":
                    return new[] { "영웅의 '용맹한 돌진' 스킬 해제" };
                case "HeroSelectCell/NameText":
                    return new[] { "스테이지 5 — 잠금" };
                case "HeroSelectCell/SubText":
                    return new[] { "5단계" };
                case "RecordsPopup/Value":
                    return new[] { "12:34", "시간 정지 ×31" };
                case "DamagePopup/Text":
                    return new[] { "-12345" };
                default:
                    return null;
            }
        }

        //# ---------------- 보고서 ----------------

        private static void WriteReport(int prefabCount, int textCount, List<Issue> issues, StringBuilder detail)
        {
            issues.Sort((a, b) =>
            {
                int rankA = KindRank(a.Kind);
                int rankB = KindRank(b.Kind);
                if (rankA != rankB)
                    return rankA.CompareTo(rankB);
                return b.Severity.CompareTo(a.Severity);
            });

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# UI 레이아웃 점검 (1280x720 기준, 비활성 홀더 인스턴스 — 레이아웃 그룹 위치는 프리팹 저장값)");
            sb.AppendLine($"프리팹 {prefabCount}개, TMP 텍스트 {textCount}개, 이슈 {issues.Count}건");
            sb.AppendLine($"샘플 카드 최장 이름: \"{_longestCardName}\" / 최장 설명: \"{_longestCardDesc}\"");
            sb.AppendLine("정렬: 겹침 > 세로 넘침(자동 축소 포함) > 가로 넘침 > 겹침(기본 꺼짐 포함), 같은 종류는 심각도(ref) 큰 순");
            sb.AppendLine();
            sb.AppendLine("## 이슈 표  (프리팹 | 종류 | 경로 | 심각도 | 상세)");
            for (int i = 0; i < issues.Count; ++i)
            {
                Issue issue = issues[i];
                sb.AppendLine($"{issue.Prefab} | {issue.Kind} | {issue.Path} | {issue.Severity:0.#} | {issue.Detail}");
            }

            sb.AppendLine();
            sb.AppendLine("## 프리팹별 TMP 목록");
            sb.Append(detail);

            File.WriteAllText(OutputPath, sb.ToString());

            int overlap = 0;
            int vertical = 0;
            int horizontal = 0;
            for (int i = 0; i < issues.Count; ++i)
            {
                int rank = KindRank(issues[i].Kind);
                if (rank == 0)
                {
                    overlap++;
                }
                else if (rank == 1)
                {
                    vertical++;
                }
                else if (rank == 2)
                {
                    horizontal++;
                }
            }

            Debug.Log($"[UiLayoutAudit] 완료 — 프리팹 {prefabCount}, TMP {textCount}, 이슈 {issues.Count}건 (겹침 {overlap} / 세로 넘침 {vertical} / 가로 넘침 {horizontal}). 상세: {OutputPath}");
        }

        private static int KindRank(string kind)
        {
            if (kind == "겹침")
                return 0;
            if (kind.StartsWith("세로") || kind.StartsWith("자동"))
                return 1;
            if (kind.StartsWith("가로"))
                return 2;
            return 3;
        }

        //# ---------------- 헬퍼 ----------------

        private static string PathOf(Transform target, Transform root)
        {
            StringBuilder sb = new StringBuilder(target.name);
            Transform current = target.parent;
            while (current != null && current != root.parent)
            {
                sb.Insert(0, current.name + "/");
                if (current == root)
                    break;
                current = current.parent;
            }

            return sb.ToString();
        }

        //# 프리팹 루트까지 activeSelf 가 모두 켜져 있으면 기본 표시 텍스트.
        private static bool IsActiveWithin(Transform target, Transform root)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.gameObject.activeSelf == false)
                    return false;
                if (current == root)
                    break;
                current = current.parent;
            }

            return true;
        }

        private static string Short(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            string flat = text.Replace("\n", "⏎");
            return flat.Length <= 30 ? flat : flat.Substring(0, 30) + "…";
        }
    }
}
