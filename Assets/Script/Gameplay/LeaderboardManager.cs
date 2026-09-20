using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LevelStatEntry
{
    public string levelName;
    public float timeSeconds;
    public int deaths;
    public bool isTimeout;
}

[System.Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public float totalTimeSeconds;
    public int totalDeaths;
    public int totalTimeouts;
    public float score;
    public string grade;
    public string dateString;
}

[System.Serializable]
public class LeaderboardDataWrapper
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

[System.Serializable]
public class GlobalLeaderboardStageItem
{
    public string levelName;
    public float timeSeconds;
    public string formattedTime;
    public int deaths;
}

[System.Serializable]
public class GlobalLeaderboardRunItem
{
    public int rank;
    public string playerName;
    public float totalTimeSeconds;
    public string formattedTime;
    public int totalDeaths;
    public string grade;
    public string dateTime;
    public List<GlobalLeaderboardStageItem> stages = new List<GlobalLeaderboardStageItem>();
}

[System.Serializable]
public class GlobalLeaderboardFileRoot
{
    public string lastUpdated;
    public List<GlobalLeaderboardRunItem> leaderboard = new List<GlobalLeaderboardRunItem>();
}

public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager _instance;
    public static LeaderboardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<LeaderboardManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("LeaderboardManager");
                    _instance = go.AddComponent<LeaderboardManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private const string PREFS_KEY = "GameLeaderboardData";
    private const int MAX_LEADERBOARD_ENTRIES = 10;
    private const float DEATH_PENALTY_SECONDS = 5.0f;
    private const float TIMEOUT_PENALTY_SECONDS = 15.0f;

    [Header("Current Run Live Stats")]
    [SerializeField] private float totalRunTime = 0f;
    [SerializeField] private int totalRunDeaths = 0;
    [SerializeField] private int totalRunTimeouts = 0;
    [SerializeField] private List<LevelStatEntry> levelStats = new List<LevelStatEntry>();

    private LeaderboardDataWrapper leaderboardData = new LeaderboardDataWrapper();
    private bool hasSavedCurrentRun = false;

    public float TotalRunTime => totalRunTime;
    public int TotalRunDeaths => totalRunDeaths;
    public int TotalRunTimeouts => totalRunTimeouts;
    public IReadOnlyList<LevelStatEntry> LevelStats => levelStats;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        CheckAndPerformInitialReset();
        LoadLeaderboardFromPrefs();
    }

    public void ResetRun()
    {
        totalRunTime = 0f;
        totalRunDeaths = 0;
        totalRunTimeouts = 0;
        levelStats.Clear();
        hasSavedCurrentRun = false;
    }

    public void RecordLevelCompletion(string levelName, float timeSeconds, int deaths, bool isTimeout = false)
    {
        if (isTimeout)
        {
            timeSeconds = 0f;
            deaths = 0;
        }

        LevelStatEntry existing = levelStats.Find(x => string.Equals(x.levelName, levelName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.timeSeconds = isTimeout ? 0f : timeSeconds;
            existing.deaths = isTimeout ? 0 : deaths;
            existing.isTimeout = isTimeout;
        }
        else
        {
            levelStats.Add(new LevelStatEntry
            {
                levelName = levelName,
                timeSeconds = isTimeout ? 0f : timeSeconds,
                deaths = isTimeout ? 0 : deaths,
                isTimeout = isTimeout
            });
        }

        RecalculateTotals();
    }

    public const float DEFAULT_RUN_DURATION_SECONDS = 300f; // 5 minutes total

    public float GetOverallRunBudgetSeconds()
    {
        if (LevelTimer.Instance != null)
        {
            return LevelTimer.Instance.OverallRunDurationSeconds;
        }
        return DEFAULT_RUN_DURATION_SECONDS;
    }

    public void DistributeTimeoutLevelTimes()
    {
        // Timed-out levels are strictly 00:00 time and 0 deaths per user specification
        foreach (var stat in levelStats)
        {
            if (stat.isTimeout)
            {
                stat.timeSeconds = 0f;
                stat.deaths = 0;
            }
        }
    }

    private void RecalculateTotals()
    {
        DistributeTimeoutLevelTimes();

        totalRunTime = 0f;
        totalRunDeaths = 0;
        totalRunTimeouts = 0;
        foreach (var stat in levelStats)
        {
            if (stat.isTimeout)
            {
                totalRunTimeouts++;
            }
            else
            {
                totalRunTime += stat.timeSeconds;
                totalRunDeaths += stat.deaths;
            }
        }

        float totalBudget = GetOverallRunBudgetSeconds();
        totalRunTime = Mathf.Clamp(totalRunTime, 0f, totalBudget);
    }

    public float CalculatePerformanceScore(float timeSeconds, int deaths, int timeouts = 0)
    {
        return timeSeconds + (deaths * DEATH_PENALTY_SECONDS) + (timeouts * TIMEOUT_PENALTY_SECONDS);
    }

    public string CalculateGrade(float timeSeconds, int deaths, int timeouts = 0)
    {
        float score = timeSeconds + (deaths * DEATH_PENALTY_SECONDS);
        string baseGrade;
        if (score <= 130f) baseGrade = "S";
        else if (score <= 220f) baseGrade = "A";
        else if (score <= 330f) baseGrade = "B";
        else baseGrade = "C";

        // Rule: Each timeout level reduces 1 rank (e.g. S with 1 timeout becomes A, with 2 timeouts becomes B)
        string[] rankOrder = { "S", "A", "B", "C", "D", "E", "F" };
        int baseIndex = Array.IndexOf(rankOrder, baseGrade);
        if (baseIndex < 0) baseIndex = 0;
        int finalIndex = Mathf.Clamp(baseIndex + timeouts, 0, rankOrder.Length - 1);
        return rankOrder[finalIndex];
    }

    public void EnsureAllLevelsRecorded()
    {
        List<string> expectedLevels = new List<string>();
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (!string.IsNullOrEmpty(sceneName) &&
                !sceneName.Equals("MainMenu", StringComparison.OrdinalIgnoreCase) &&
                !sceneName.Equals("Ending", StringComparison.OrdinalIgnoreCase) &&
                !expectedLevels.Contains(sceneName))
            {
                expectedLevels.Add(sceneName);
            }
        }

        if (expectedLevels.Count == 0)
        {
            expectedLevels.AddRange(new[] { "Level 1", "Level 2", "Level 3", "Level 4", "Level 5", "Level 6" });
        }

        foreach (string lvl in expectedLevels)
        {
            bool exists = false;
            foreach (var st in levelStats)
            {
                if (string.Equals(st.levelName, lvl, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                levelStats.Add(new LevelStatEntry
                {
                    levelName = lvl,
                    timeSeconds = 0f,
                    deaths = 0,
                    isTimeout = true
                });
            }
        }
        RecalculateTotals();
    }

    public bool SaveCurrentRun(string playerName = "")
    {
        if (hasSavedCurrentRun) return false;
        // If any player has a single timeout level, they cannot be on the global leaderboard
        if (totalRunTimeouts > 0) return false;
        if (levelStats.Count == 0 && totalRunTime <= 0f) return false;

        if (string.IsNullOrWhiteSpace(playerName) || playerName == "Player 1")
        {
            playerName = PlayerPrefs.GetString("PlayerName", "Player");
        }
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        float score = CalculatePerformanceScore(totalRunTime, totalRunDeaths, totalRunTimeouts);
        string grade = CalculateGrade(totalRunTime, totalRunDeaths, totalRunTimeouts);
        string currentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        LeaderboardEntry entry = new LeaderboardEntry
        {
            playerName = playerName,
            totalTimeSeconds = totalRunTime,
            totalDeaths = totalRunDeaths,
            totalTimeouts = totalRunTimeouts,
            score = score,
            grade = grade,
            dateString = currentDate
        };

        leaderboardData.entries.Add(entry);
        leaderboardData.entries.Sort((a, b) => a.score.CompareTo(b.score));

        if (leaderboardData.entries.Count > MAX_LEADERBOARD_ENTRIES)
        {
            leaderboardData.entries.RemoveRange(MAX_LEADERBOARD_ENTRIES, leaderboardData.entries.Count - MAX_LEADERBOARD_ENTRIES);
        }

        SaveLeaderboardToPrefs();
        SaveLeaderboardToJsonFile(playerName, totalRunTime, totalRunDeaths, grade);
        hasSavedCurrentRun = true;
        return true;
    }

    public static string GetLeaderboardJsonFilePath()
    {
        string docsPath = null;
        try
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                string directDocs = System.IO.Path.Combine(userProfile, "Documents");
                if (System.IO.Directory.Exists(directDocs))
                {
                    docsPath = directDocs;
                }
            }
        }
        catch { }

        if (string.IsNullOrEmpty(docsPath))
        {
            docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        if (string.IsNullOrEmpty(docsPath))
        {
            docsPath = @"C:\Users\Anuj Chauhan\Documents";
        }

        string dir = System.IO.Path.Combine(docsPath, "DriftIT");
        if (!System.IO.Directory.Exists(dir))
        {
            try
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardManager] Failed creating folder {dir}: {ex.Message}");
            }
        }
        return System.IO.Path.Combine(dir, "leaderboard.json");
    }

    private void SaveLeaderboardToJsonFile(string playerName, float runTime, int runDeaths, string runGrade)
    {
        try
        {
            string filePath = GetLeaderboardJsonFilePath();
            GlobalLeaderboardFileRoot root = new GlobalLeaderboardFileRoot();

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string existingJson = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        var loaded = JsonUtility.FromJson<GlobalLeaderboardFileRoot>(existingJson);
                        if (loaded != null && loaded.leaderboard != null)
                        {
                            root = loaded;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LeaderboardManager] Failed reading existing leaderboard JSON: {ex.Message}");
                }
            }

            TimeSpan runSpan = TimeSpan.FromSeconds(runTime);
            string formattedRunTime = string.Format("{0:D2}:{1:D2}", (int)runSpan.TotalMinutes, runSpan.Seconds);
            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            GlobalLeaderboardRunItem newRun = new GlobalLeaderboardRunItem
            {
                playerName = playerName,
                totalTimeSeconds = runTime,
                formattedTime = formattedRunTime,
                totalDeaths = runDeaths,
                grade = runGrade,
                dateTime = currentDateTime,
                stages = new List<GlobalLeaderboardStageItem>()
            };

            if (levelStats != null)
            {
                foreach (var st in levelStats)
                {
                    TimeSpan stSpan = TimeSpan.FromSeconds(st.timeSeconds);
                    newRun.stages.Add(new GlobalLeaderboardStageItem
                    {
                        levelName = st.levelName,
                        timeSeconds = st.timeSeconds,
                        formattedTime = string.Format("{0:D2}:{1:D2}", (int)stSpan.TotalMinutes, stSpan.Seconds),
                        deaths = st.deaths
                    });
                }
            }

            if (root.leaderboard == null)
            {
                root.leaderboard = new List<GlobalLeaderboardRunItem>();
            }

            root.leaderboard.Add(newRun);

            // Sort by total time ascending, then total deaths ascending
            root.leaderboard.Sort((a, b) =>
            {
                int cmpTime = a.totalTimeSeconds.CompareTo(b.totalTimeSeconds);
                if (cmpTime != 0) return cmpTime;
                return a.totalDeaths.CompareTo(b.totalDeaths);
            });

            // Re-assign ranks 1..N
            for (int i = 0; i < root.leaderboard.Count; i++)
            {
                root.leaderboard[i].rank = i + 1;
            }

            root.lastUpdated = currentDateTime;

            string jsonOutput = JsonUtility.ToJson(root, true);
            string parentDir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(parentDir) && !System.IO.Directory.Exists(parentDir))
            {
                System.IO.Directory.CreateDirectory(parentDir);
            }
            System.IO.File.WriteAllText(filePath, jsonOutput, System.Text.Encoding.UTF8);
            Debug.Log($"[LeaderboardManager] Successfully updated global leaderboard JSON at: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LeaderboardManager] Error saving global leaderboard JSON: {ex.Message}");
        }
    }

    public List<LeaderboardEntry> GetTopEntries()
    {
        if (leaderboardData.entries.Count > MAX_LEADERBOARD_ENTRIES)
        {
            return leaderboardData.entries.GetRange(0, MAX_LEADERBOARD_ENTRIES);
        }
        return new List<LeaderboardEntry>(leaderboardData.entries);
    }

    private void LoadLeaderboardFromPrefs()
    {
        if (PlayerPrefs.HasKey(PREFS_KEY))
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    leaderboardData = JsonUtility.FromJson<LeaderboardDataWrapper>(json);
                    if (leaderboardData == null) leaderboardData = new LeaderboardDataWrapper();
                }
                catch
                {
                    leaderboardData = new LeaderboardDataWrapper();
                }
            }
        }
    }

    private const string LEADERBOARD_RESET_VERSION_KEY = "Leaderboard_CleanReset_v1";

    public void ClearLeaderboardData()
    {
        leaderboardData = new LeaderboardDataWrapper();
        if (PlayerPrefs.HasKey(PREFS_KEY))
        {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();
        }
    }

    private void CheckAndPerformInitialReset()
    {
        if (!PlayerPrefs.HasKey(LEADERBOARD_RESET_VERSION_KEY))
        {
            ClearLeaderboardData();
            PlayerPrefs.SetInt(LEADERBOARD_RESET_VERSION_KEY, 1);
            PlayerPrefs.Save();
        }
    }

    private void SaveLeaderboardToPrefs()
    {
        try
        {
            string json = JsonUtility.ToJson(leaderboardData);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }
        catch
        {
        }
    }
}
