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

    private static readonly string[] ValidBoards =
    {
        "match3",
        "snake_infinite", "snake_level1", "snake_level2",
        "cubedash", "shooter",
        "minigolf", "minigolf_level1", "minigolf_level2", "minigolf_level3"
    };

    // Boards where a LOWER score is better (e.g. minigolf strokes).
    private static readonly string[] AscendingBoards =
    {
        "minigolf", "minigolf_level1", "minigolf_level2", "minigolf_level3"
    };

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

    public static string GetBoardKey(string gameKey, string stageId)
    {
        string game = gameKey.ToLowerInvariant();
        string stage = string.IsNullOrEmpty(stageId) ? "" : stageId.ToLowerInvariant();
        return stage.Length == 0 ? game : $"{game}_{stage}";
    }

    public bool IsValidBoard(string boardKey)
    {
        return Array.IndexOf(ValidBoards, boardKey) >= 0;
    }

    public bool IsAscendingBoard(string boardKey)
    {
        return Array.IndexOf(AscendingBoards, boardKey) >= 0;
    }

    private string BaseUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/{DatabaseId}";

    #endregion

    #region Public API

    /// <summary>
    /// Submit a score as a brand-new leaderboard entry. Every call creates a new
    /// document, so the same machine/name can hold many rows (arcade style).
    /// </summary>
    public async System.Threading.Tasks.Task SubmitScoreAndWaitAsync(string boardKey, string name, int score, string breakdown = null)
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
        if (score <= 0 || string.IsNullOrWhiteSpace(name))
            return;

        await SubmitNewEntryAsync(boardKey, name.Trim(), score, breakdown);
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

    private async Task SubmitNewEntryAsync(string boardKey, string name, int score, string breakdown)
    {
        try
        {
            string docId = Guid.NewGuid().ToString("N");
            bool ok = await WriteScoreAsync(boardKey, name, score, docId, breakdown);
            if (ok)
            {
                Debug.Log($"Leaderboard: submitted {score} for '{boardKey}' as '{name}'.");
            }
            else
            {
                Debug.LogWarning($"Leaderboard: submit for '{boardKey}' failed (server rejected the write).");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Leaderboard: submit failed ({e.Message})");
        }
    }

    private async Task<bool> WriteScoreAsync(string boardId, string name, int score, string docId, string breakdown)
    {
        string[] parts = boardId.Split('_');
        string game = parts[0];
        string scene = parts.Length > 1 ? boardId.Substring(boardId.IndexOf('_') + 1) : "";

        var fields = new Dictionary<string, object>
        {
            { "Score", new Dictionary<string, object> { { "integerValue", score.ToString() } } },
            { "Name", new Dictionary<string, object> { { "stringValue", name } } },
            { "Game", new Dictionary<string, object> { { "stringValue", game } } },
            { "Scene", new Dictionary<string, object> { { "stringValue", scene } } },
            { "Timestamp", new Dictionary<string, object> { { "timestampValue", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } }
        };
        if (!string.IsNullOrWhiteSpace(breakdown))
        {
            fields["Breakdown"] = new Dictionary<string, object> { { "stringValue", breakdown } };
        }
        var document = new Dictionary<string, object> { { "fields", fields } };



        string url = $"{BaseUrl}/documents/{boardId}/{docId}?key={ApiKey}&currentDocument.exists=false";
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

    private static Task<bool> AwaitRequest(UnityWebRequest req)
    {
        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.TrySetResult(req.result == UnityWebRequest.Result.Success);
        return tcs.Task;
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
                                    { "direction", IsAscendingBoard(boardId) ? "ASCENDING" : "DESCENDING" }
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
                                        { "op", IsAscendingBoard(boardId) ? "LESS_THAN" : "GREATER_THAN" },
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
            string breakdown = "";
            if (fields.TryGetValue("Score", out object s) && s is Dictionary<string, object> sv &&
                sv.TryGetValue("integerValue", out object iv))
                int.TryParse((string)iv, out score);

            if (fields.TryGetValue("Breakdown", out object b) && b is Dictionary<string, object> bv &&
                bv.TryGetValue("stringValue", out object breakdownValue))
                breakdown = breakdownValue?.ToString() ?? "";


            if (fields.TryGetValue("Name", out object n) && n is Dictionary<string, object> nv &&
                nv.TryGetValue("stringValue", out object str))
                name = str?.ToString() ?? "";

            LeaderboardEntry entry = new LeaderboardEntry(name, score, 0);
            entry.Breakdown = breakdown;
            entries.Add(entry);
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

    #endregion
}