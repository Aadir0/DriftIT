#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;

[CustomEditor(typeof(LeaderboardManager))]
public class LeaderboardManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector fields
        DrawDefaultInspector();

        LeaderboardManager manager = (LeaderboardManager)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Leaderboard Administrative Controls", EditorStyles.boldLabel);
        
        GUI.backgroundColor = new Color(0.95f, 0.25f, 0.25f, 1.0f);
        if (GUILayout.Button("🗑 CLEAR ALL LEADERBOARD DATA (LOCAL + CLOUD)", GUILayout.Height(38)))
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Confirm Clear All Leaderboard Data",
                "Are you sure you want to PERMANENTLY ERASE all leaderboard data?\n\nThis will clear:\n• In-Memory Leaderboard records\n• PlayerPrefs stored scores\n• Local JSON file (Documents/DriftIT/leaderboard.json)\n• Website & Cloud Firebase Database records\n\nThis action cannot be undone.",
                "Yes, Clear Everything",
                "Cancel"
            );

            if (confirmed)
            {
                PerformFullLeaderboardWipe(manager);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox(
            "Clicking the button above clears both local storage (Documents/DriftIT/leaderboard.json, PlayerPrefs) and the Firebase Cloud Database immediately.",
            MessageType.Info
        );
    }

    private static void PerformFullLeaderboardWipe(LeaderboardManager manager)
    {
        // 1. Clear PlayerPrefs
        PlayerPrefs.DeleteKey("LeaderboardData");
        PlayerPrefs.Save();

        // 2. Clear Documents JSON file
        try
        {
            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(docsPath, "DriftIT");
            string filePath = Path.Combine(folderPath, "leaderboard.json");
            if (File.Exists(filePath))
            {
                File.WriteAllText(filePath, "{\"leaderboard\":[],\"lastUpdated\":\"\"}", System.Text.Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LeaderboardEditor] Error clearing local JSON file: {ex.Message}");
        }

        // 3. Clear in Play Mode via manager if running
        if (Application.isPlaying && manager != null)
        {
            manager.ClearAllLeaderboardData((success, msg) =>
            {
                EditorUtility.DisplayDialog("Leaderboard Reset Result", msg, "OK");
            });
            return;
        }

        // 4. In Edit Mode, perform direct HTTP DELETE to Firebase
        string cloudEndpoint = "https://driftit-6dd08-default-rtdb.asia-southeast1.firebasedatabase.app/leaderboard.json";
        
        try
        {
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(cloudEndpoint);
            request.Method = "DELETE";
            request.Timeout = 10000;

            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                int statusCode = (int)response.StatusCode;
                if (statusCode >= 200 && statusCode < 300)
                {
                    Debug.Log("[LeaderboardEditor] Local and Cloud Leaderboards successfully cleared via Editor!");
                    EditorUtility.DisplayDialog(
                        "Leaderboard Cleared",
                        "Successfully cleared all local records and Firebase cloud database!",
                        "OK"
                    );
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardEditor] Cloud returned status: {statusCode}");
                    EditorUtility.DisplayDialog(
                        "Leaderboard Cleared (Local Only)",
                        $"Local records cleared, but cloud returned status: {statusCode}",
                        "OK"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LeaderboardEditor] Failed to delete cloud records: {ex.Message}");
            EditorUtility.DisplayDialog(
                "Leaderboard Cleared (Local Only)",
                $"Local records cleared, but Cloud wipe had an error:\n{ex.Message}",
                "OK"
            );
        }
    }
}
#endif

