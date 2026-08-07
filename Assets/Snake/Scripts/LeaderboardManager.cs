using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Google.MiniJSON;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    /// <summary>
    /// Optional runtime prefab for the leaderboard UI. When set, games
    /// instantiate this prefab instead of a procedural panel, so on-screen
    /// design always matches the prefab. Assign via the inspector on a scene
    /// object, or call <see cref="SetLeaderboardPrefab"/>.
    /// </summary>
    [SerializeField] private GameObject leaderboardPrefab;

    public GameObject LeaderboardPrefab => leaderboardPrefab;

    public void SetLeaderboardPrefab(GameObject prefab)
    {
        leaderboardPrefab = prefab;
    }

    #region Configuration

    // Fill these two values from your Firebase console.
    //  - Project ID:  Project settings > General > Project ID
    //  - API key:     Project settings > General > Web API Key
    private const string ProjectId = "doan-b85f9";
    private const string ApiKey = "AIzaSyCrlf9nxmD9yqWeo4IcwGYkL0UPPjBpCzU";

    private const string DatabaseId = "(default)";
    private const string NameKey = "PlayerName";
    private const string DeviceKey = "LeaderboardDeviceId";

    private static readonly string[] ValidBoards =
    {
        "snake_infinite", "snake_level1", "snake_level2",
        "cubedash", "shooter", "minigolf"
    };

    #endregion

    #region Private Fields

    private string deviceId;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        deviceId = PlayerPrefs.GetString(DeviceKey, "");
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(DeviceKey, deviceId);
            PlayerPrefs.Save();
        }
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("LeaderboardManager");
        go.AddComponent<LeaderboardManager>();
    }

    #endregion

    #region Read-Only Helpers

    public bool IsInitialized => !string.IsNullOrEmpty(ProjectId) && !string.IsNullOrEmpty(ApiKey);

    public string PlayerName
    {
        get => PlayerPrefs.GetString(NameKey, "");
        set
        {
            PlayerPrefs.SetString(NameKey, value);
            PlayerPrefs.Save();
        }
    }

    public bool HasPlayerName => !string.IsNullOrEmpty(PlayerName);

    public static string GetBoardKey(string gameKey, string stageId)
    {
        string stage = string.IsNullOrEmpty(stageId) ? "" : stageId.ToLowerInvariant();
        return $"{gameKey.ToLowerInvariant()}_{stage}";
    }

    public bool IsValidBoard(string boardKey)
    {
        return Array.IndexOf(ValidBoards, boardKey) >= 0;
    }

    private string BaseUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/{DatabaseId}";

    #endregion

    #region Public API

    /// <summary>
    /// Submit the player's best score for a board. Keeps the highest score per device.
    /// </summary>
    public void SubmitScore(string boardKey, int score)
    {
        if (!IsValidBoard(boardKey))
        {
            Debug.LogWarning($"LeaderboardManager: unknown board '{boardKey}', score not submitted.");
            return;
        }
        if (!IsInitialized)
        {
            Debug.LogWarning("LeaderboardManager: not configured (set ProjectId/ApiKey), score not submitted.");
            return;
        }
        if (score <= 0) return;

        string name = string.IsNullOrEmpty(PlayerName) ? "Anonymous" : PlayerName;
        _ = SubmitScoreAsync(boardKey, name, score);
    }

    /// <summary>
    /// Submit the score for a board and await completion, so callers can read
    /// back fresh data (list / rank) that reflects the submitted score.
    /// </summary>
    public async System.Threading.Tasks.Task SubmitAndWaitAsync(string boardKey, int score)
    {
        if (!IsValidBoard(boardKey) || !IsInitialized || score <= 0)
            return;
        string name = string.IsNullOrEmpty(PlayerName) ? "Anonymous" : PlayerName;
        await SubmitScoreAsync(boardKey, name, score);
    }

    /// <summary>
    /// Fetch the top N entries for a board.
    /// </summary>
    public void FetchTop(string boardKey, int limit, Action<List<LeaderboardEntry>> onResult, Action onError = null)
    {
        if (!IsInitialized)
        {
            onError?.Invoke();
            return;
        }
        _ = FetchTopAsync(boardKey, limit, onResult, onError);
    }

    /// <summary>
    /// Compute the player's rank (1-based) for a board given their score.
    /// </summary>
    public void GetPlayerRank(string boardKey, int score, Action<int> onResult, Action onError = null)
    {
        if (!IsInitialized)
        {
            onError?.Invoke();
            return;
        }
        if (score <= 0)
        {
            onResult?.Invoke(0);
            return;
        }
        _ = GetRankAsync(boardKey, score, onResult, onError);
    }

    #endregion

    #region Firestore REST Requests

    private async Task SubmitScoreAsync(string boardKey, string name, int score)
    {
        try
        {
            (bool exists, int existing) = await GetExistingScoreAsync(boardKey);

            if (exists && existing >= score)
            {
                Debug.Log($"Leaderboard: existing {existing} >= {score}, not overwriting.");
                return;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                bool ok = await WriteScoreAsync(boardKey, name, score, exists);
                if (ok)
                {
                    Debug.Log($"Leaderboard: submitted {score} for '{boardKey}'.");
                    return;
                }
                // Precondition failed (document state changed) - re-read and try again.
                (exists, existing) = await GetExistingScoreAsync(boardKey);
                if (exists && existing >= score) return;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Leaderboard: submit failed ({e.Message})");
        }
    }

    private async Task<(bool exists, int score)> GetExistingScoreAsync(string boardId)
    {
        string url = $"{BaseUrl}/documents/{boardId}/{deviceId}?key={ApiKey}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            await AwaitRequest(req);
            if (req.result == UnityWebRequest.Result.Success)
            {
                int score = ParseScore(req.downloadHandler.text);
                return (true, score);
            }
            // 404 = document not found yet.
            return (false, 0);
        }
    }

    private static Task<bool> AwaitRequest(UnityWebRequest req)
    {
        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.TrySetResult(req.result == UnityWebRequest.Result.Success);
        return tcs.Task;
    }

    private async Task<bool> WriteScoreAsync(string boardId, string name, int score, bool exists)
    {
        var fields = new Dictionary<string, object>
        {
            { "Score", new Dictionary<string, object> { { "integerValue", score.ToString() } } },
            { "Name", new Dictionary<string, object> { { "stringValue", name } } },
            { "Timestamp", new Dictionary<string, object> { { "timestampValue", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } }
        };
        var document = new Dictionary<string, object> { { "fields", fields } };

        string url = $"{BaseUrl}/documents/{boardId}/{deviceId}?key={ApiKey}&currentDocument.exists={(exists ? "true" : "false")}";
        string body = Json.Serialize(document);

        using (UnityWebRequest req = new UnityWebRequest(url, "PATCH"))
        {
            byte[] payload = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(payload);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            await AwaitRequest(req);
            return req.result == UnityWebRequest.Result.Success;
        }
    }

    private async Task FetchTopAsync(string boardId, int limit, Action<List<LeaderboardEntry>> onResult, Action onError)
    {
        try
        {
            var query = new Dictionary<string, object>
            {
                { "structuredQuery", new Dictionary<string, object>
                    {
                        { "from", new List<object> { new Dictionary<string, object> { { "collectionId", boardId } } } },
                        { "orderBy", new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    { "field", new Dictionary<string, object> { { "fieldPath", "Score" } } },
                                    { "direction", "DESCENDING" }
                                }
                            }
                        },
                        { "limit", limit }
                    }
                }
            };

            string body = Json.Serialize(query);
            string result = await RunQueryAsync(body);

            List<LeaderboardEntry> entries = ParseRunQuery(result);
            onResult?.Invoke(entries);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Leaderboard: fetch failed ({e.Message})");
            onError?.Invoke();
        }
    }

    private async Task GetRankAsync(string boardId, int score, Action<int> onResult, Action onError)
    {
        try
        {
            var query = new Dictionary<string, object>
            {
                { "structuredQuery", new Dictionary<string, object>
                    {
                        { "from", new List<object> { new Dictionary<string, object> { { "collectionId", boardId } } } },
                        { "where", new Dictionary<string, object>
                            {
                                { "fieldFilter", new Dictionary<string, object>
                                    {
                                        { "field", new Dictionary<string, object> { { "fieldPath", "Score" } } },
                                        { "op", "GREATER_THAN" },
                                        { "value", new Dictionary<string, object> { { "integerValue", score.ToString() } } }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            string result = await RunQueryAsync(Json.Serialize(query));
            int count = ParseJsonDocumentsCount(result);
            onResult?.Invoke(count + 1);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Leaderboard: rank failed ({e.Message})");
            onError?.Invoke();
        }
    }

    private async Task<string> RunQueryAsync(string body)
    {
        string url = $"{BaseUrl}/documents:runQuery?key={ApiKey}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            await AwaitRequest(req);
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"HTTP {(int)req.responseCode}: {req.error}");
            return req.downloadHandler.text;
        }
    }

    #endregion

    #region JSON Parsing

    private static int ParseScore(string json)
    {
        if (!TryParseObject(json, out Dictionary<string, object> root)) return 0;
        if (!root.TryGetValue("fields", out object f) || !(f is Dictionary<string, object> fields)) return 0;
        if (!fields.TryGetValue("Score", out object s) || !(s is Dictionary<string, object> sv)) return 0;
        return (sv.TryGetValue("integerValue", out object iv) && int.TryParse((string)iv, out int val)) ? val : 0;
    }

    private static List<LeaderboardEntry> ParseRunQuery(string json)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        if (string.IsNullOrEmpty(json)) return entries;

        if (!(Json.Deserialize(json) is List<object> list)) return entries;

        foreach (object item in list)
        {
            if (!(item is Dictionary<string, object> wrapper)) continue;
            if (!wrapper.TryGetValue("document", out object docObj)) continue;
            if (!(docObj is Dictionary<string, object> doc)) continue;
            if (!doc.TryGetValue("fields", out object f) || !(f is Dictionary<string, object> fields)) continue;

            int score = 0;
            string name = "";
            if (fields.TryGetValue("Score", out object s) && s is Dictionary<string, object> sv &&
                sv.TryGetValue("integerValue", out object iv))
                int.TryParse((string)iv, out score);

            if (fields.TryGetValue("Name", out object n) && n is Dictionary<string, object> nv &&
                nv.TryGetValue("stringValue", out object str))
                name = str?.ToString() ?? "";

            entries.Add(new LeaderboardEntry(name, score, 0));
        }
        return entries;
    }

    private static int ParseJsonDocumentsCount(string json)
    {
        int count = 0;
        if (string.IsNullOrEmpty(json)) return count;
        if (!(Json.Deserialize(json) is List<object> list)) return count;
        foreach (object item in list)
        {
            if (item is Dictionary<string, object> wrapper && wrapper.ContainsKey("document"))
                count++;
        }
        return count;
    }

    private static bool TryParseObject(string json, out Dictionary<string, object> result)
    {
        result = null;
        if (string.IsNullOrEmpty(json)) return false;
        if (Json.Deserialize(json) is Dictionary<string, object> obj)
        {
            result = obj;
            return true;
        }
        return false;
    }

    #endregion
}