using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# Play 모드 전 화면 버튼 스위프. EditorApplication.update 가 코루틴 스택을 한 틱씩 진행한다(static 상태 — 씬 전환에도 유지).
    //# 진입점(인자 없음): StartSweep / SweepStatus / StopSweep / SweepReport. 결과는 Temp/PlayDriverReport.txt.
    public static partial class PlayDriver
    {
        //# 네트워크·파괴·출격 동작 deny-list — 키 = "소속 팝업 타입:버튼 오브젝트명". 근거는 각 줄 주석.
        private static readonly string[] DenyKeys =
        {
            "CloudPopup:RestoreButton",         //# 클라우드 복원 — 네트워크 + 로컬 세이브 덮어쓰기
            "CloudPopup:ConflictRestoreButton", //# 충돌 복원 — 위와 동일
            "CloudPopup:ConflictLaterButton",   //# 충돌 보류 — 클라우드 동기 상태 변경
            "CloudPopup:NameConfirmButton",     //# 표시명 변경 — Firestore 쓰기
            "ConfirmPopup:ConfirmButton",       //# 복원/전투 포기 확정 — 되돌릴 수 없음 (Cancel 로 닫는다)
            "DailyPopup:StartButton",           //# 데일리 판 시작 — 전투 진입(출격은 3단계에서만) + 데일리 기록 오염
            "TowerPopup:ChallengeButton",       //# 탑 도전 시작 — 전투 진입 + 탑 기록 오염
            "VillageHud:SortieButton",          //# 출격 — 3단계에서 의도적으로 수행
            "FormationPopup:SortieButton",      //# 편성 출격 — 3단계에서 의도적으로 수행
        };

        private static readonly string[] HudTypes = { "VillageHud", "BattleHud", "ToastView", "SkillUnlockBanner" };
        private static readonly string[] PriorityPopups = { "CardSelectionPopup", "ResultPopup" };
        private static readonly string[] CloseNames = { "CloseButton", "Dim", "CancelButton" };

        private const string SceneLoading = "Loading";
        private const string SceneVillage = "Village";
        private const string SceneBattle = "Battle";
        private const string ReportFile = "Temp/PlayDriverReport.txt";
        private const string BackupDir = "Temp/PlayDriverSaveBackup";
        private const string SaveFileName = "meta_profile.json"; //# MetaProfileStore.FileName 과 동일
        private const double TimeLimitSec = 720;
        private const double BattleLimitSec = 300;
        private const int MaxDepth = 3;
        private const int MaxClicksPerPopup = 60;
        private const float BattleSpeed = 10f;

        private static bool _running;
        private static bool _done;
        private static bool _abort;
        private static bool _sceneLost;
        private static bool _preempt;
        private static bool _waitOk;
        private static bool _hudTested;
        private static string _phase = "idle";
        private static string _step = string.Empty;
        private static string _endReason = string.Empty;
        private static string _expectScene = SceneLoading;
        private static double _startTime;
        private static double _endTime;
        private static float _targetScale = 1f;
        private static int _pass;
        private static int _fail;
        private static int _skip;
        private static int _errCount;
        private static int _errSeen;
        private static int _pickCounter;
        private static readonly object ErrLock = new object();
        private static readonly List<string> Lines = new List<string>();
        private static readonly List<string> Fails = new List<string>();
        private static readonly List<string> ErrMsgs = new List<string>();
        private static readonly HashSet<string> BattleDone = new HashSet<string>();
        private static readonly Stack<IEnumerator> Routine = new Stack<IEnumerator>();

        private static double Now => EditorApplication.timeSinceStartup;

        //# ---------------- 진입점 ----------------

        public static string StartSweep()
        {
            if (_running)
                return "ALREADY RUNNING";

            if (Application.isPlaying == false)
                return NotPlaying;

            Lines.Clear();
            Fails.Clear();
            ErrMsgs.Clear();
            BattleDone.Clear();
            Routine.Clear();
            _pass = _fail = _skip = _errCount = _errSeen = _pickCounter = 0;
            _done = _abort = _sceneLost = _preempt = _hudTested = false;
            _phase = "starting";
            _step = string.Empty;
            _endReason = string.Empty;
            _expectScene = SceneManager.GetActiveScene().name;
            _targetScale = 1f;
            _startTime = Now;
            BackupSave();
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
            Routine.Push(Sweep());
            _running = true;
            WriteReport();
            return "STARTED scene=" + _expectScene;
        }

        public static string SweepStatus()
        {
            int total = _pass + _fail + _skip;
            double elapsed = (_running ? Now : _endTime) - _startTime;
            return $"running={_running} done={_done} phase={_phase} step={_step} scene={SceneManager.GetActiveScene().name} "
                + $"clicks={total} PASS={_pass} FAIL={_fail} SKIP={_skip} elapsed={elapsed:F0}s reason={_endReason}";
        }

        public static string StopSweep()
        {
            if (_running == false)
            {
                RestoreSave();
                return "NOT RUNNING (save restored if pending)";
            }

            Record("SKIP", "(사용자 StopSweep 요청) -> 중단");
            Finish("StopSweep", true);
            return "STOPPED";
        }

        public static string SweepReport()
        {
            if (File.Exists(ReportFile) == false)
                return "NO REPORT";

            string[] all = File.ReadAllLines(ReportFile);
            int count = Math.Min(all.Length, 45);
            return string.Join("\n", all, 0, count);
        }

        //# ---------------- 메인 시나리오 ----------------

        private static IEnumerator Sweep()
        {
            yield return LoadingPhase();
            if (_abort)
                yield break;

            yield return VillagePhase();
            if (_abort)
                yield break;

            yield return SortiePhase();
            if (_abort)
                yield break;

            yield return BattlePhase();
        }

        private static IEnumerator LoadingPhase()
        {
            _phase = "Loading";
            if (SceneManager.GetActiveScene().name != SceneLoading)
            {
                Record("SKIP", $"Loading > (시작 씬 {SceneManager.GetActiveScene().name}) -> Loading 단계 생략");
                yield break;
            }

            _expectScene = SceneLoading;
            int attempts = 0;
            string label = "Loading > ClickArea";
            double end = Now + 180;
            while (SceneManager.GetActiveScene().name == SceneLoading && Now < end)
            {
                List<Button> buttons = HudButtons();
                if (buttons.Count > 0)
                {
                    label = "Loading > " + PathOf(buttons[0].transform);
                    buttons[0].onClick.Invoke();
                    attempts++;
                }

                yield return Wait(1f);
            }

            if (SceneManager.GetActiveScene().name == SceneLoading)
            {
                Record("FAIL", $"{label} -> 180초 내 Village 전환 안 됨 (클릭 {attempts}회)");
                Abort("로딩 완료/전환 실패");
                yield break;
            }

            Record("PASS", $"{label} -> 클릭 {attempts}회 후 씬 {SceneManager.GetActiveScene().name} 전환");
        }

        private static IEnumerator VillagePhase()
        {
            _phase = "Village";
            _expectScene = SceneVillage;
            HashSet<string> done = new HashSet<string>();
            yield return EnsureVillage();
            if (_abort)
                yield break;

            yield return CloseStrays("Village/HUD", false);
            for (int guard = 0; guard < 200; guard++)
            {
                if (_abort)
                    yield break;

                yield return EnsureVillage();
                if (_abort)
                    yield break;

                _sceneLost = false;
                _preempt = false;
                Button next = null;
                foreach (Button b in HudButtons())
                {
                    string path = PathOf(b.transform);
                    if (done.Contains(path))
                        continue;

                    done.Add(path);
                    next = b;
                    break;
                }

                if (next == null)
                    break;

                _step = "Village/HUD/" + next.name;
                float wait = next.name.StartsWith("Stage", StringComparison.Ordinal) ? 0.4f : 2f;
                yield return ClickStep(next, "Village/HUD", "VillageHud", null, 0, wait);
                if (_sceneLost || _abort)
                    continue;

                yield return CloseStrays("Village/HUD", true);
            }
        }

        private static IEnumerator SortiePhase()
        {
            _phase = "Sortie";
            _expectScene = SceneVillage;
            GameObject hud = FindUI("VillageHud");
            Button sortie = hud == null ? null : FindButtonNamed(hud, "SortieButton");
            if (sortie == null)
            {
                Abort("출격: SortieButton 없음/비활성");
                yield break;
            }

            FlushBackgroundErrors();
            Record("PASS", $"Village/HUD > SortieButton \"{TextOf(sortie)}\" -> 클릭 (편성 팝업 또는 바로 출격)");
            sortie.onClick.Invoke();
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == SceneBattle || FindUI("FormationPopup") != null, 5f);
            GameObject formation = FindUI("FormationPopup");
            if (formation != null && SceneManager.GetActiveScene().name != SceneBattle)
            {
                Button go = FindButtonNamed(formation, "SortieButton");
                if (go == null)
                {
                    Abort("출격: FormationPopup/SortieButton 없음");
                    yield break;
                }

                Record("PASS", $"Village/FormationPopup > SortieButton \"{TextOf(go)}\" -> 출격");
                go.onClick.Invoke();
            }

            yield return WaitBattleReady();
            if (_waitOk == false)
                Abort("출격 후 Battle 진입 실패 (60초)");
        }

        private static IEnumerator BattlePhase()
        {
            for (int round = 0; round < 2; round++)
            {
                _phase = "Battle#" + (round + 1);
                _expectScene = SceneBattle;
                yield return RunBattleUntilResult();
                if (_abort)
                    yield break;

                yield return ResultStep(round);
                if (_abort)
                    yield break;

                if (SceneManager.GetActiveScene().name == SceneVillage)
                    break;
            }

            _phase = "Village(end)";
            if (SceneManager.GetActiveScene().name != SceneVillage)
                Abort("종료 시 마을 복귀 실패");
        }

        //# ---------------- 전투 ----------------

        private static IEnumerator RunBattleUntilResult()
        {
            double battleStart = Now;
            int pickFailStreak = 0;
            _targetScale = BattleSpeed;
            while (true)
            {
                if (_abort)
                    yield break;

                if (SceneManager.GetActiveScene().name != SceneBattle)
                {
                    Record("FAIL", $"Battle > (진행 중) -> 예상 못한 씬 이탈: {SceneManager.GetActiveScene().name}");
                    Abort("전투 중 씬 이탈");
                    yield break;
                }

                if (FindUI("ResultPopup") != null)
                    yield break;

                GameObject card = FindUI("CardSelectionPopup");
                if (card != null)
                {
                    yield return PickCard(card);
                    pickFailStreak = _waitOk ? 0 : pickFailStreak + 1;
                    if (pickFailStreak > 5)
                    {
                        Abort("카드 선택 5회 연속 실패");
                        yield break;
                    }

                    continue;
                }

                if (Time.timeScale > 0f && Mathf.Approximately(Time.timeScale, _targetScale) == false)
                    Time.timeScale = _targetScale;

                if (_hudTested == false && Now - battleStart > 3 && Popups().Count == 0)
                {
                    yield return BattleHudTest();
                    continue;
                }

                if (Now - battleStart > BattleLimitSec)
                {
                    Record("FAIL", $"Battle > (진행) -> {BattleLimitSec:F0}초 내 결과 팝업 없음");
                    Abort("전투 시간 상한");
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator PickCard(GameObject card)
        {
            yield return Wait(0.3f);
            List<Button> picks = new List<Button>();
            double end = Now + 5;
            while (picks.Count == 0 && Now < end && IsClosed(card) == false)
            {
                foreach (Button b in ButtonsUnder(card.transform))
                {
                    if (b.name == "PickButton")
                        picks.Add(b);
                }

                if (picks.Count == 0)
                    yield return null;
            }

            _waitOk = false;
            if (IsClosed(card))
            {
                _waitOk = true;
                yield break;
            }

            if (picks.Count == 0)
            {
                Record("FAIL", "Battle/CardSelectionPopup > PickButton -> 5초 내 interactable 버튼 없음");
                yield return Wait(0.5f);
                yield break;
            }

            FlushBackgroundErrors();
            int idx = _pickCounter % picks.Count;
            _pickCounter++;
            Button pick = picks[idx];
            string label = $"Battle/CardSelectionPopup > {RelPath(pick.transform, card.transform)} \"{TextOf(pick)}\"";
            int errBefore = ErrCount();
            pick.onClick.Invoke();
            yield return WaitUntil(() => IsClosed(card), 4f);
            bool closed = _waitOk;
            int errDelta = ErrCount() - errBefore;
            bool ok = closed && errDelta == 0;
            Record(ok ? "PASS" : "FAIL", $"{label} -> {idx + 1}/{picks.Count}번째 선택, 팝업 {(closed ? "닫힘" : "안 닫힘")}; err={errDelta}{FirstError(errBefore, errDelta)}");
            _errSeen = ErrCount();
            _waitOk = closed;
        }

        private static IEnumerator BattleHudTest()
        {
            _hudTested = true;
            _targetScale = 1f;
            if (Time.timeScale > 0f)
                Time.timeScale = 1f;

            _step = "Battle/HUD";
            for (int guard = 0; guard < 40; guard++)
            {
                if (_abort)
                    yield break;

                _sceneLost = false;
                _preempt = false;
                Button next = null;
                foreach (Button b in HudButtons())
                {
                    string path = PathOf(b.transform);
                    if (BattleDone.Contains(path))
                        continue;

                    BattleDone.Add(path);
                    next = b;
                    break;
                }

                if (next == null)
                    break;

                yield return ClickStep(next, "Battle/HUD", "BattleHud", null, 0, 1f);
                if (_preempt)
                {
                    //# 카드/결과 팝업이 끼어듦 — 메인 루프가 처리하고 남은 버튼은 이어서 테스트.
                    _preempt = false;
                    _hudTested = false;
                    break;
                }

                if (_sceneLost)
                {
                    _sceneLost = false;
                    break;
                }

                yield return CloseStrays("Battle/HUD", true);
            }

            _targetScale = BattleSpeed;
        }

        private static IEnumerator ResultStep(int round)
        {
            _step = "Battle/ResultPopup";
            Time.timeScale = 1f;
            _targetScale = 1f;
            yield return Wait(1f);
            GameObject res = FindUI("ResultPopup");
            if (res == null)
            {
                Abort("결과 팝업 사라짐");
                yield break;
            }

            yield return WaitUntil(() => FindButtonNamed(res, "VillageButton") != null, 20f);
            if (round == 0)
            {
                Button retry = FindButtonNamed(res, "RetryButton");
                if (retry == null)
                {
                    Record("SKIP", "Battle/ResultPopup > RetryButton -> 비활성/없음, 다시 도전 생략");
                }
                else
                {
                    int handle = SceneManager.GetActiveScene().handle;
                    Record("PASS", $"Battle/ResultPopup > RetryButton \"{TextOf(retry)}\" -> 클릭");
                    retry.onClick.Invoke();
                    yield return WaitUntil(() => SceneManager.GetActiveScene().handle != handle, 30f);
                    if (_waitOk)
                    {
                        yield return WaitBattleReady();
                        if (_waitOk)
                        {
                            Record("PASS", "Battle > 다시 도전 -> Battle 재진입 확인");
                            yield break;
                        }
                    }

                    Record("FAIL", "Battle > 다시 도전 -> Battle 재진입 실패, 마을로 복구 시도");
                    SceneManager.LoadScene(SceneVillage);
                    yield return EnsureVillage();
                    yield break;
                }
            }

            Button village = FindButtonNamed(res, "VillageButton");
            if (village == null)
            {
                Abort("VillageButton 없음/비활성");
                yield break;
            }

            _expectScene = SceneVillage;
            Record("PASS", $"Battle/ResultPopup > VillageButton \"{TextOf(village)}\" -> 클릭");
            village.onClick.Invoke();
            yield return EnsureVillage();
            if (_abort == false)
                Record("PASS", "Village > 마을로 -> 마을 복귀 확인");
        }

        //# ---------------- 클릭/팝업 탐색 ----------------

        private static IEnumerator ClickStep(Button b, string screen, string owner, Transform scope, int depth, float popupWait)
        {
            if (b == null)
                yield break;

            FlushBackgroundErrors();
            string label = $"{screen} > {RelPath(b.transform, scope)} \"{TextOf(b)}\"";
            if (Array.IndexOf(DenyKeys, owner + ":" + b.name) >= 0)
            {
                Record("SKIP", label + " -> deny-list");
                yield break;
            }

            bool hadScope = scope != null;
            HashSet<int> before = PopupIds();
            int errBefore = ErrCount();
            int handleBefore = SceneManager.GetActiveScene().handle;
            b.onClick.Invoke();

            double end = Now + popupWait;
            List<GameObject> opened = new List<GameObject>();
            while (true)
            {
                yield return null;
                opened = NewPopups(before);
                if (opened.Count > 0 || Now >= end || SceneLost(handleBefore))
                    break;
            }

            if (opened.Count > 0)
                yield return Wait(0.3f);

            if (SceneLost(handleBefore))
            {
                Record("FAIL", $"{label} -> 예상 못한 씬 이탈: {SceneManager.GetActiveScene().name}");
                _sceneLost = true;
                _errSeen = ErrCount();
                yield break;
            }

            int errDelta = ErrCount() - errBefore;
            string note = opened.Count > 0 ? "새 팝업 " + NamesOf(opened) : "팝업 변화 없음";
            if (hadScope && (scope == null || scope.gameObject.activeInHierarchy == false))
                note += " (소속 팝업 닫힘)";

            Record(errDelta > 0 ? "FAIL" : "PASS", $"{label} -> {note}; err={errDelta}{FirstError(errBefore, errDelta)}");
            _errSeen = ErrCount();

            foreach (GameObject p in opened)
            {
                if (p == null)
                    continue;

                string pn = TypeNameOf(p);
                if (Array.IndexOf(PriorityPopups, pn) >= 0)
                {
                    _preempt = true;
                    yield break;
                }

                string sub = screen + ">" + pn;
                _step = sub;
                if (depth + 1 <= MaxDepth)
                {
                    yield return ExplorePopup(p, pn, sub, depth + 1);
                }
                else
                {
                    Record("SKIP", $"{sub} -> 깊이 {MaxDepth} 초과, 내부 탐색 생략");
                }

                if (_sceneLost || _abort || _preempt)
                    yield break;

                yield return EnsureClosed(p, pn, sub);
                if (_sceneLost || _abort)
                    yield break;
            }
        }

        private static IEnumerator ExplorePopup(GameObject root, string pn, string screen, int depth)
        {
            HashSet<int> done = new HashSet<int>();
            yield return Wait(0.2f);
            for (int n = 0; n < MaxClicksPerPopup; n++)
            {
                if (IsClosed(root))
                    yield break;

                Button next = null;
                foreach (Button b in ButtonsUnder(root.transform))
                {
                    if (done.Contains(b.GetInstanceID()) || IsCloseButton(b))
                        continue;

                    next = b;
                    break;
                }

                if (next == null)
                    yield break;

                done.Add(next.GetInstanceID());
                yield return ClickStep(next, screen, pn, root.transform, depth, 0.4f);
                if (_sceneLost || _abort || _preempt)
                    yield break;
            }

            Record("SKIP", $"{screen} -> 클릭 상한 {MaxClicksPerPopup} 도달, 나머지 생략");
        }

        private static IEnumerator EnsureClosed(GameObject root, string pn, string screen)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (IsClosed(root))
                    yield break;

                Button close = FindClose(root);
                if (close == null)
                {
                    Record("FAIL", $"{screen} > (닫기 버튼 없음) -> 열린 채 남음");
                    break;
                }

                string label = $"{screen} > {close.name} \"{TextOf(close)}\"";
                int errBefore = ErrCount();
                close.onClick.Invoke();
                yield return WaitUntil(() => IsClosed(root), 1.5f);
                int errDelta = ErrCount() - errBefore;
                if (_waitOk)
                {
                    string retryNote = attempt > 1 ? $" (재시도 {attempt})" : string.Empty;
                    Record(errDelta > 0 ? "FAIL" : "PASS", $"{label} -> 닫힘{retryNote}; err={errDelta}{FirstError(errBefore, errDelta)}");
                    _errSeen = ErrCount();
                    yield break;
                }

                Record("FAIL", $"{label} -> 닫히지 않음 (시도 {attempt}/3)");
            }

            if (IsClosed(root))
                yield break;

            Record("FAIL", $"{screen} -> 닫기 실패, 씬 재로드로 복구");
            yield return ReloadScene();
        }

        private static IEnumerator CloseStrays(string screen, bool strict)
        {
            foreach (GameObject p in Popups())
            {
                string pn = TypeNameOf(p);
                if (Array.IndexOf(PriorityPopups, pn) >= 0)
                    continue;

                if (strict)
                    Record("FAIL", $"{screen} -> 기준 상태 미복귀, 남은 팝업 {pn}");

                yield return EnsureClosed(p, pn, screen + ">(잔류)" + pn);
                if (_abort || _sceneLost)
                    yield break;
            }
        }

        private static IEnumerator ReloadScene()
        {
            string name = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(name);
            _sceneLost = true;
            yield return Wait(1.5f);
        }

        private static IEnumerator EnsureVillage()
        {
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == SceneVillage && FindUI("VillageHud") != null, 40f);
            if (_waitOk)
                yield break;

            if (SceneManager.GetActiveScene().name != SceneVillage)
            {
                Record("FAIL", $"Village > (복귀 대기) -> 현재 씬 {SceneManager.GetActiveScene().name}, Village 로 강제 로드");
                SceneManager.LoadScene(SceneVillage);
                yield return WaitUntil(() => SceneManager.GetActiveScene().name == SceneVillage && FindUI("VillageHud") != null, 40f);
            }

            if (_waitOk == false)
                Abort("Village HUD 복구 실패");
        }

        private static IEnumerator WaitBattleReady()
        {
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == SceneBattle && FindUI("BattleHud") != null, 60f);
        }

        //# ---------------- 조회 헬퍼 ----------------

        private static List<GameObject> Popups()
        {
            List<GameObject> list = new List<GameObject>();
            foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb == null || mb.gameObject.activeInHierarchy == false || IsUIBase(mb.GetType()) == false)
                    continue;

                if (Array.IndexOf(HudTypes, mb.GetType().Name) >= 0 || list.Contains(mb.gameObject))
                    continue;

                list.Add(mb.gameObject);
            }

            return list;
        }

        private static HashSet<int> PopupIds()
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (GameObject p in Popups())
            {
                ids.Add(p.GetInstanceID());
            }

            return ids;
        }

        private static List<GameObject> NewPopups(HashSet<int> before)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject p in Popups())
            {
                if (before.Contains(p.GetInstanceID()) == false)
                    result.Add(p);
            }

            return result;
        }

        private static GameObject FindUI(string typeName)
        {
            foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb != null && mb.gameObject.activeInHierarchy && mb.GetType().Name == typeName && IsUIBase(mb.GetType()))
                    return mb.gameObject;
            }

            return null;
        }

        private static string TypeNameOf(GameObject go)
        {
            foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb != null && IsUIBase(mb.GetType()))
                    return mb.GetType().Name;
            }

            return go.name;
        }

        private static string NamesOf(List<GameObject> list)
        {
            List<string> names = new List<string>();
            foreach (GameObject g in list)
            {
                names.Add(TypeNameOf(g));
            }

            return string.Join(",", names);
        }

        private static bool IsClosed(GameObject root) => root == null || root.activeInHierarchy == false;

        private static bool SceneLost(int handleBefore)
        {
            Scene s = SceneManager.GetActiveScene();
            return s.handle != handleBefore || s.name != _expectScene;
        }

        private static bool IsCloseButton(Button b) => Array.IndexOf(CloseNames, b.name) >= 0;

        private static Button FindClose(GameObject root)
        {
            List<Button> buttons = ButtonsUnder(root.transform);
            foreach (string name in CloseNames)
            {
                foreach (Button b in buttons)
                {
                    if (b.name == name)
                        return b;
                }
            }

            return null;
        }

        private static Button FindButtonNamed(GameObject root, string name)
        {
            foreach (Button b in ButtonsUnder(root.transform))
            {
                if (b.name == name)
                    return b;
            }

            return null;
        }

        private static List<Button> ButtonsUnder(Transform root)
        {
            List<Button> result = new List<Button>();
            foreach (Button b in root.GetComponentsInChildren<Button>(false))
            {
                if (b.interactable)
                    result.Add(b);
            }

            result.Sort((x, y) => string.CompareOrdinal(SortKey(x.transform), SortKey(y.transform)));
            return result;
        }

        //# 팝업 루트 밖(HUD 레벨)의 활성+interactable 버튼.
        private static List<Button> HudButtons()
        {
            List<Transform> roots = new List<Transform>();
            foreach (GameObject p in Popups())
            {
                roots.Add(p.transform);
            }

            List<Button> result = new List<Button>();
            foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (b.gameObject.activeInHierarchy == false || b.interactable == false)
                    continue;

                bool inside = false;
                foreach (Transform r in roots)
                {
                    if (b.transform.IsChildOf(r))
                    {
                        inside = true;
                        break;
                    }
                }

                if (inside == false)
                    result.Add(b);
            }

            result.Sort((x, y) => string.CompareOrdinal(SortKey(x.transform), SortKey(y.transform)));
            return result;
        }

        private static string RelPath(Transform t, Transform scope)
        {
            List<string> names = new List<string>();
            for (Transform c = t; c != null && c != scope; c = c.parent)
            {
                names.Insert(0, c.name);
                if (scope == null && names.Count >= 3)
                    break;
            }

            return string.Join("/", names);
        }

        //# ---------------- 대기/기록/에러 ----------------

        private static IEnumerator Wait(float sec)
        {
            double end = Now + sec;
            while (Now < end)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitUntil(Func<bool> cond, float timeout)
        {
            double end = Now + timeout;
            _waitOk = false;
            while (Now < end)
            {
                if (cond())
                {
                    _waitOk = true;
                    yield break;
                }

                yield return null;
            }

            _waitOk = cond();
        }

        private static void Abort(string reason)
        {
            Record("FAIL", "(중단) " + reason);
            _abort = true;
        }

        private static void Record(string status, string text)
        {
            if (status == "PASS")
            {
                _pass++;
            }
            else if (status == "FAIL")
            {
                _fail++;
                Fails.Add(text);
            }
            else
            {
                _skip++;
            }

            Lines.Add($"[{status}] {text}");
            WriteReport();
        }

        private static int ErrCount()
        {
            lock (ErrLock)
            {
                return _errCount;
            }
        }

        private static string FirstError(int errBefore, int delta)
        {
            if (delta <= 0)
                return string.Empty;

            lock (ErrLock)
            {
                return errBefore < ErrMsgs.Count ? " [" + ErrMsgs[errBefore] + "]" : string.Empty;
            }
        }

        private static void FlushBackgroundErrors()
        {
            int now = ErrCount();
            if (now <= _errSeen)
                return;

            Record("FAIL", $"(클릭 외) 에러 {now - _errSeen}건{FirstError(_errSeen, now - _errSeen)}");
            _errSeen = now;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            lock (ErrLock)
            {
                _errCount++;
                if (ErrMsgs.Count < 200)
                {
                    string first = condition.Split('\n')[0];
                    ErrMsgs.Add(first.Length > 140 ? first.Substring(0, 140) : first);
                }
            }
        }

        //# ---------------- 틱/종료 ----------------

        private static void Tick()
        {
            if (_running == false)
                return;

            if (Application.isPlaying == false)
            {
                Finish("Play 종료됨", true);
                return;
            }

            if (Now - _startTime > TimeLimitSec)
            {
                Record("FAIL", $"전체 소요 상한 {TimeLimitSec:F0}초 초과 (phase={_phase})");
                Finish("시간 상한", true);
                return;
            }

            if (Routine.Count == 0)
            {
                Finish(_abort ? "FAIL 로 중단" : "완료", _abort);
                return;
            }

            try
            {
                IEnumerator top = Routine.Peek();
                if (top.MoveNext() == false)
                    Routine.Pop();
                else if (top.Current is IEnumerator sub)
                    Routine.Push(sub);
            }
            catch (Exception e)
            {
                Record("FAIL", "스위프 내부 예외: " + e.Message);
                Finish("내부 예외", true);
            }
        }

        private static void Finish(string reason, bool abort)
        {
            if (_running == false)
                return;

            _running = false;
            _done = true;
            _endReason = reason;
            _endTime = Now;
            _phase = abort ? "aborted" : "finished";
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            Routine.Clear();
            if (Application.isPlaying)
            {
                Time.timeScale = 1f;
                EditorApplication.isPlaying = false;
            }

            WriteReport();
        }

        private static void WriteReport()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                int total = _pass + _fail + _skip;
                double elapsed = (_running ? Now : _endTime) - _startTime;
                sb.AppendLine("=== PlayDriver Sweep Report ===");
                sb.AppendLine($"state={(_running ? "RUNNING" : "DONE")} phase={_phase} reason={_endReason} elapsed={elapsed:F0}s");
                sb.AppendLine($"총 클릭 {total} / PASS {_pass} / FAIL {_fail} / SKIP {_skip}");
                sb.AppendLine("save: 시작 시 Temp/PlayDriverSaveBackup 백업, 종료(EnteredEditMode) 시 복원");
                sb.AppendLine($"FAIL 목록 ({Fails.Count}):");
                foreach (string f in Fails)
                {
                    sb.AppendLine("  - " + f);
                }

                sb.AppendLine("--- 클릭 로그 ---");
                foreach (string line in Lines)
                {
                    sb.AppendLine(line);
                }

                Directory.CreateDirectory("Temp");
                File.WriteAllText(ReportFile, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PlayDriver] 리포트 기록 실패: " + e.Message);
            }
        }

        //# ---------------- 세이브 백업/복원 ----------------

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private static void BackupSave()
        {
            string marker = Path.Combine(BackupDir, "pending.txt");
            if (File.Exists(marker))
                return;

            Directory.CreateDirectory(BackupDir);
            bool exists = File.Exists(SavePath);
            if (exists)
                File.Copy(SavePath, Path.Combine(BackupDir, SaveFileName), true);

            File.WriteAllText(marker, exists ? "exists" : "absent");
        }

        private static void RestoreSave()
        {
            string marker = Path.Combine(BackupDir, "pending.txt");
            if (File.Exists(marker) == false)
                return;

            try
            {
                string saved = Path.Combine(BackupDir, SaveFileName);
                if (File.ReadAllText(marker) == "exists" && File.Exists(saved))
                    File.Copy(saved, SavePath, true);
                else if (File.Exists(SavePath))
                    File.Delete(SavePath);

                Directory.Delete(BackupDir, true);
                ResetMetaSession();
                Debug.Log("[PlayDriver] 세이브 복원 완료: " + SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PlayDriver] 세이브 복원 실패 (Temp/PlayDriverSaveBackup 수동 복사 필요): " + e.Message);
            }
        }

        //# 도메인 리로드 비활성 시 정적 MetaSession 이 복원 전 프로필을 들고 있지 않게 비운다 (리플렉션 — 에디터 어셈블리 비참조).
        private static void ResetMetaSession()
        {
            Type t = Type.GetType("Lair.Meta.MetaSession, Lair");
            if (t == null)
                return;

            System.Reflection.FieldInfo profile = t.GetField("Profile");
            System.Reflection.FieldInfo store = t.GetField("Store");
            profile?.SetValue(null, null);
            store?.SetValue(null, null);
        }

        [InitializeOnLoadMethod]
        private static void SweepBoot()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
                RestoreSave();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && _running)
                Finish("Play 종료됨", true);

            if (state == PlayModeStateChange.EnteredEditMode)
                RestoreSave();
        }
    }
}
