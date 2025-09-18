using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WebGL-specific JSON serialization helper to handle edge cases and ensure compatibility
/// between Unity C# and JavaScript JSON parsing
/// </summary>
public static class WebGLJsonHelper
{
    /// <summary>
    /// Serialize GameData to JSON with WebGL-specific handling
    /// </summary>
    /// <param name="gameData">GameData object to serialize</param>
    /// <returns>JSON string safe for JavaScript consumption</returns>
    public static string SerializeGameData(GameData gameData)
    {
        try
        {
            // Ensure all required fields are properly initialized
            if (gameData == null)
            {
                Debug.LogError("[WebGLJsonHelper] Cannot serialize null GameData");
                return "{}";
            }

            // Initialize data list if null
            if (gameData.data == null)
            {
                gameData.data = new List<GameMetric>();
            }

            // Clean up each GameMetric to ensure proper serialization
            foreach (var metric in gameData.data)
            {
                CleanupGameMetric(metric);
            }

            // Use Unity's JsonUtility for serialization
            string json = JsonUtility.ToJson(gameData, true);
            
            // Validate the JSON is not empty
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                Debug.LogWarning("[WebGLJsonHelper] GameData serialized to empty JSON, creating minimal structure");
                return CreateMinimalGameDataJson(gameData);
            }

            return json;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLJsonHelper] Error serializing GameData: {e.Message}");
            return CreateMinimalGameDataJson(gameData);
        }
    }

    /// <summary>
    /// Deserialize SocketIOResponse from JSON with WebGL-specific handling
    /// </summary>
    /// <param name="json">JSON string from JavaScript</param>
    /// <returns>SocketIOResponse object or null if parsing failed</returns>
    public static SocketIOResponse DeserializeSocketIOResponse(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[WebGLJsonHelper] Cannot deserialize empty JSON string");
                return null;
            }

            // Handle JavaScript null/undefined values
            json = CleanupJsonFromJavaScript(json);

            SocketIOResponse response = JsonUtility.FromJson<SocketIOResponse>(json);
            
            // Validate the response
            if (response == null)
            {
                Debug.LogWarning("[WebGLJsonHelper] Failed to deserialize SocketIOResponse");
                return null;
            }

            // Ensure messageId is not null
            if (string.IsNullOrEmpty(response.messageId))
            {
                response.messageId = "unknown";
            }

            return response;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLJsonHelper] Error deserializing SocketIOResponse: {e.Message}");
            Debug.LogError($"[WebGLJsonHelper] JSON was: {json}");
            return null;
        }
    }

    /// <summary>
    /// Serialize any object to JSON with WebGL-specific error handling
    /// </summary>
    /// <param name="obj">Object to serialize</param>
    /// <returns>JSON string or empty object if serialization fails</returns>
    public static string SerializeObject(object obj)
    {
        try
        {
            if (obj == null)
            {
                return "null";
            }

            string json = JsonUtility.ToJson(obj, true);
            return string.IsNullOrEmpty(json) ? "{}" : json;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLJsonHelper] Error serializing object: {e.Message}");
            return "{}";
        }
    }

    /// <summary>
    /// Clean up GameMetric to ensure proper serialization
    /// </summary>
    /// <param name="metric">GameMetric to clean up</param>
    private static void CleanupGameMetric(GameMetric metric)
    {
        if (metric == null) return;

        // Initialize targetClicks list if null to avoid serialization issues
        if (metric.targetClicks == null)
        {
            metric.targetClicks = new List<string>();
        }

        // Ensure string fields are not null
        if (metric.type == null)
        {
            metric.type = "";
        }

        if (metric.question == null)
        {
            metric.question = "";
        }

        if (metric.answer == null)
        {
            metric.answer = "";
        }

        // Validate numeric values for JavaScript compatibility
        if (float.IsNaN(metric.score) || float.IsInfinity(metric.score))
        {
            metric.score = 0f;
        }

        if (float.IsNaN(metric.precision) || float.IsInfinity(metric.precision))
        {
            metric.precision = 0f;
        }

        if (float.IsNaN(metric.value) || float.IsInfinity(metric.value))
        {
            metric.value = 0f;
        }

        if (float.IsNaN(metric.timeElapsed) || float.IsInfinity(metric.timeElapsed))
        {
            metric.timeElapsed = 0f;
        }
    }

    /// <summary>
    /// Create minimal GameData JSON structure as fallback
    /// </summary>
    /// <param name="gameData">Original GameData (may be null)</param>
    /// <returns>Minimal JSON string</returns>
    private static string CreateMinimalGameDataJson(GameData gameData)
    {
        string game = gameData?.game ?? "unity-demo";
        string messageId = gameData?.messageId ?? "";
        float timeElapsed = gameData?.timeElapsed ?? 0f;

        return $@"{{
            ""game"": ""{game}"",
            ""data"": [],
            ""messageId"": ""{messageId}"",
            ""timeElapsed"": {timeElapsed}
        }}";
    }

    /// <summary>
    /// Clean up JSON string from JavaScript to handle common issues
    /// </summary>
    /// <param name="json">Raw JSON from JavaScript</param>
    /// <returns>Cleaned JSON string</returns>
    private static string CleanupJsonFromJavaScript(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return json;
        }

        // Replace JavaScript undefined with null
        json = json.Replace("undefined", "null");
        
        // Replace JavaScript NaN with 0
        json = json.Replace("NaN", "0");
        
        // Replace JavaScript Infinity with a large number
        json = json.Replace("Infinity", "999999");
        json = json.Replace("-Infinity", "-999999");

        return json.Trim();
    }

    /// <summary>
    /// Validate that a JSON string is properly formatted
    /// </summary>
    /// <param name="json">JSON string to validate</param>
    /// <returns>True if JSON appears valid</returns>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        json = json.Trim();
        
        // Basic JSON structure validation
        return (json.StartsWith("{") && json.EndsWith("}")) ||
               (json.StartsWith("[") && json.EndsWith("]")) ||
               json == "null" ||
               json == "true" ||
               json == "false" ||
               (json.StartsWith("\"") && json.EndsWith("\""));
    }

    /// <summary>
    /// Create a test GameData object for debugging serialization
    /// </summary>
    /// <returns>Test GameData object</returns>
    public static GameData CreateTestGameData()
    {
        return new GameData
        {
            game = "unity-test",
            messageId = "test_" + DateTime.Now.Ticks,
            timeElapsed = 1.5f,
            data = new List<GameMetric>
            {
                new GameMetric
                {
                    score = 100f,
                    type = "hit",
                    precision = 0.85f,
                    age = 1,
                    nth = 1,
                    victim = 0,
                    streak = 1,
                    obstacleBlock = false,
                    barsActive = 2,
                    targetClicks = new List<string> { "mid", "inner" },
                    question = "Test question",
                    answer = "Test answer",
                    value = 50f
                }
            }
        };
    }

    /// <summary>
    /// Test JSON serialization and deserialization
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void TestSerialization()
    {
        Debug.Log("=== WebGL JSON Serialization Test ===");
        
        // Test GameData serialization
        GameData testData = CreateTestGameData();
        string json = SerializeGameData(testData);
        Debug.Log($"Serialized GameData: {json}");
        Debug.Log($"JSON is valid: {IsValidJson(json)}");
        
        // Test SocketIOResponse deserialization
        string testResponseJson = @"{""messageId"":""test123"",""value"":42.5}";
        SocketIOResponse response = DeserializeSocketIOResponse(testResponseJson);
        if (response != null)
        {
            Debug.Log($"Deserialized Response - MessageId: {response.messageId}, Value: {response.value}");
        }
        else
        {
            Debug.LogError("Failed to deserialize test response");
        }
        
        Debug.Log("=== End WebGL JSON Test ===");
    }
}
