using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class CloudStageEntry
{
    public string levelName;
    public float timeSeconds;
    public string formattedTime;
    public int deaths;
}

[Serializable]
public class CloudRunPayload
{
    public string runId;
    public string playerName;
    public float totalTimeSeconds;
    public string formattedTime;
    public int totalDeaths;
    public int totalTimeouts;
    public float score;
    public string grade;
    public string dateTime;
    public List<CloudStageEntry> stages = new List<CloudStageEntry>();
}

[Serializable]
public class CloudLeaderboardFetchWrapper
{
    // Helper to deserialize JSON dictionaries or arrays from cloud endpoints
    public List<CloudRunPayload> runs = new List<CloudRunPayload>();
}

public class CloudLeaderboardService : MonoBehaviour
{
    private static CloudLeaderboardService _instance;
    public static CloudLeaderboardService Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("CloudLeaderboardService");
                _instance = obj.AddComponent<CloudLeaderboardService>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    [Header("Cloud Endpoint Configuration")]
    [Tooltip("Base URL for the leaderboard REST API or Firebase Realtime DB endpoint.")]
    [SerializeField] private string endpointUrl = "https://driftit-6dd08-default-rtdb.asia-southeast1.firebasedatabase.app/leaderboard";
    [SerializeField] private float requestTimeoutSeconds = 8.0f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetEndpointUrl(string newUrl)
    {
        if (!string.IsNullOrWhiteSpace(newUrl))
        {
            endpointUrl = newUrl.Trim().TrimEnd('/');
        }
    }

    public string GetEndpointUrl()
    {
        return endpointUrl;
    }

    private string BuildItemUrl(string runId)
    {
        string baseUri = endpointUrl.Trim().TrimEnd('/');
        if (baseUri.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            baseUri = baseUri.Substring(0, baseUri.Length - 5);
        }
        return $"{baseUri}/{runId}.json";
    }

    private string BuildCollectionUrl()
    {
        string baseUri = endpointUrl.Trim().TrimEnd('/');
        if (baseUri.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return baseUri;
        }
        return $"{baseUri}.json";
    }

    /// <summary>
    /// Asynchronously submits a run to the global cloud database.
    /// </summary>
    public void SubmitRun(CloudRunPayload run, Action<bool, string> onComplete = null)
    {
        if (run == null)
        {
            onComplete?.Invoke(false, "Run payload was null.");
            return;
        }

        StartCoroutine(SubmitRunRoutine(run, onComplete));
    }

    private IEnumerator SubmitRunRoutine(CloudRunPayload run, Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(run.runId))
        {
            run.runId = Guid.NewGuid().ToString("N");
        }

        string jsonPayload = JsonUtility.ToJson(run);
        string targetUrl = BuildItemUrl(run.runId);

        using (UnityWebRequest req = new UnityWebRequest(targetUrl, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[CloudLeaderboard] Run successfully submitted: {run.playerName} ({run.formattedTime}) -> {targetUrl}");
                onComplete?.Invoke(true, req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"[CloudLeaderboard] Failed to submit run ({req.responseCode}) to {targetUrl}: {req.error}");
                onComplete?.Invoke(false, req.error);
            }
        }
    }

    /// <summary>
    /// Asynchronously fetches the top N runs from the cloud.
    /// </summary>
    public void FetchTopRuns(int limit, Action<bool, List<CloudRunPayload>> onComplete)
    {
        StartCoroutine(FetchTopRunsRoutine(limit, onComplete));
    }

    private IEnumerator FetchTopRunsRoutine(int limit, Action<bool, List<CloudRunPayload>> onComplete)
    {
        string targetUrl = BuildCollectionUrl();

        using (UnityWebRequest req = UnityWebRequest.Get(targetUrl))
        {
            req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonText = req.downloadHandler.text;
                List<CloudRunPayload> parsedRuns = ParseCloudRunsJson(jsonText);

                // Sort: fastest time ascending, then lowest deaths ascending
                parsedRuns.Sort((a, b) =>
                {
                    int cmpTime = a.totalTimeSeconds.CompareTo(b.totalTimeSeconds);
                    if (cmpTime != 0) return cmpTime;
                    return a.totalDeaths.CompareTo(b.totalDeaths);
                });

                if (limit > 0 && parsedRuns.Count > limit)
                {
                    parsedRuns = parsedRuns.GetRange(0, limit);
                }

                onComplete?.Invoke(true, parsedRuns);
            }
            else
            {
                Debug.LogWarning($"[CloudLeaderboard] Failed fetching runs from {targetUrl}: {req.error}");
                onComplete?.Invoke(false, new List<CloudRunPayload>());
            }
        }
    }

    /// <summary>
    /// Parses JSON response that could be either a JSON object map of { runId: { ... } } or an array of runs.
    /// </summary>
    private List<CloudRunPayload> ParseCloudRunsJson(string rawJson)
    {
        List<CloudRunPayload> result = new List<CloudRunPayload>();
        if (string.IsNullOrWhiteSpace(rawJson) || rawJson == "null") return result;

        rawJson = rawJson.Trim();

        // If it's a JSON array
        if (rawJson.StartsWith("["))
        {
            string wrapped = $"{{\"runs\":{rawJson}}}";
            try
            {
                CloudLeaderboardFetchWrapper wrapper = JsonUtility.FromJson<CloudLeaderboardFetchWrapper>(wrapped);
                if (wrapper != null && wrapper.runs != null)
                {
                    result.AddRange(wrapper.runs);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudLeaderboard] Array parse error: {ex.Message}");
            }
            return result;
        }

        // If it's a Firebase Key-Value Map of Objects { "runId1": { ... }, "runId2": { ... } }
        if (rawJson.StartsWith("{"))
        {
            try
            {
                // Extract individual object JSONs
                int depth = 0;
                int objectStart = -1;
                for (int i = 0; i < rawJson.Length; i++)
                {
                    char c = rawJson[i];
                    if (c == '{')
                    {
                        depth++;
                        if (depth == 2)
                        {
                            objectStart = i;
                        }
                    }
                    else if (c == '}')
                    {
                        if (depth == 2 && objectStart != -1)
                        {
                            string itemJson = rawJson.Substring(objectStart, i - objectStart + 1);
                            try
                            {
                                CloudRunPayload payload = JsonUtility.FromJson<CloudRunPayload>(itemJson);
                                if (payload != null && !string.IsNullOrEmpty(payload.playerName) && payload.totalTimeSeconds > 0)
                                {
                                    result.Add(payload);
                                }
                            }
                            catch { }
                            objectStart = -1;
                        }
                        depth--;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudLeaderboard] Object map parse error: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Asynchronously wipes all runs from the cloud database.
    /// </summary>
    public void ClearCloudLeaderboard(Action<bool, string> onComplete = null)
    {
        StartCoroutine(ClearCloudLeaderboardRoutine(onComplete));
    }

    private IEnumerator ClearCloudLeaderboardRoutine(Action<bool, string> onComplete)
    {
        string targetUrl = BuildCollectionUrl();

        using (UnityWebRequest req = UnityWebRequest.Delete(targetUrl))
        {
            req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[CloudLeaderboard] Cloud database wiped successfully at: {targetUrl}");
                onComplete?.Invoke(true, "Cloud leaderboard cleared.");
            }
            else
            {
                Debug.LogWarning($"[CloudLeaderboard] Failed to clear cloud leaderboard ({req.responseCode}): {req.error}");
                onComplete?.Invoke(false, req.error);
            }
        }
    }
}
