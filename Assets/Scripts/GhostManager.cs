using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages multiplayer ghost players via PubNub
/// </summary>
public class GhostManager : MonoBehaviour
{
    [Header("PubNub Settings")]
    private const string PUBLISH_KEY = "pub-c-ed826874-f30b-4d98-b87a-396accbe7f28";
    private const string SUBSCRIBE_KEY = "sub-c-8d52d20d-c3a8-4d4b-bed7-5ee42ad6c4a0";
    private const string GHOST_CHANNEL = "ghosts";
    private const string OCCUPANCY_CHANNEL = "ai-game,ai-game-pnpres";

    private string userId;
    private string timeToken = "0";
    private string occupancyTimeToken = "0";

    private Dictionary<string, GhostData> ghosts = new Dictionary<string, GhostData>();
    private Dictionary<string, GameObject> ghostObjects = new Dictionary<string, GameObject>();

    private GameManager gameManager;
    private int lastPublishFrame = 0;

    [System.Serializable]
    private class GhostData
    {
        public string id;
        public float x;
        public float y;
    }

    [System.Serializable]
    private class PubNubResponse
    {
        public object[] messages;
        public string timetoken;
    }

    void Start()
    {
        userId = "user-" + Random.Range(1, 1000000);
        gameManager = GameManager.Instance;

        // Start subscribe coroutines
        StartCoroutine(SubscribeToGhosts());
        StartCoroutine(SubscribeToOccupancy());
    }

    void Update()
    {
        if (gameManager == null || gameManager.CurrentScene != GameScene.Play)
            return;

        // Publish player coordinates every 60 frames
        if (Time.frameCount - lastPublishFrame >= 60)
        {
            lastPublishFrame = Time.frameCount;
            if (gameManager.Player != null)
            {
                Vector2 pos = gameManager.Player.GetPosition();
                // Convert Unity coordinates back to pixel coordinates
                float pixelX = (pos.x + gameManager.GameWidth / 2f) * 100f;
                float pixelY = (pos.y + gameManager.GameHeight / 2f) * 100f;
                StartCoroutine(PublishPosition(pixelX, pixelY));
            }
        }

        // Render ghost players
        RenderGhosts();
    }

    IEnumerator PublishPosition(float x, float y)
    {
        string message = JsonUtility.ToJson(new GhostData
        {
            id = userId,
            x = x,
            y = y
        });

        string encodedMessage = UnityWebRequest.EscapeURL(message);
        string url = $"https://ps.pndsn.com/publish/{PUBLISH_KEY}/{SUBSCRIBE_KEY}/0/{GHOST_CHANNEL}/0/{encodedMessage}?uuid={userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            // Ignore errors silently
        }
    }

    IEnumerator SubscribeToGhosts()
    {
        while (true)
        {
            string url = $"https://ps.pndsn.com/subscribe/{SUBSCRIBE_KEY}/{GHOST_CHANNEL}/0/{timeToken}?uuid={userId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string response = request.downloadHandler.text;
                        // Parse the simple PubNub response format: [[messages], "timetoken"]
                        // This is a simplified parser
                        int lastBracket = response.LastIndexOf('"');
                        int secondLastQuote = response.LastIndexOf('"', lastBracket - 1);
                        if (secondLastQuote >= 0 && lastBracket > secondLastQuote)
                        {
                            timeToken = response.Substring(secondLastQuote + 1, lastBracket - secondLastQuote - 1);
                        }

                        // Extract messages (simplified)
                        int messagesStart = response.IndexOf("[[");
                        int messagesEnd = response.IndexOf("]]");
                        if (messagesStart >= 0 && messagesEnd > messagesStart)
                        {
                            string messagesStr = response.Substring(messagesStart + 1, messagesEnd - messagesStart);
                            // Parse individual messages
                            ParseGhostMessages(messagesStr);
                        }
                    }
                    catch (System.Exception)
                    {
                        // Parsing error, continue
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void ParseGhostMessages(string messagesStr)
    {
        // Simple JSON parsing for ghost messages
        int start = 0;
        while ((start = messagesStr.IndexOf("{\"id\"", start)) >= 0)
        {
            int end = messagesStr.IndexOf("}", start);
            if (end < 0) break;

            string jsonObj = messagesStr.Substring(start, end - start + 1);
            try
            {
                GhostData ghost = JsonUtility.FromJson<GhostData>(jsonObj);
                if (ghost != null && !string.IsNullOrEmpty(ghost.id) && ghost.id != userId)
                {
                    ghosts[ghost.id] = ghost;
                }
            }
            catch (System.Exception)
            {
                // Parsing error for this message
            }

            start = end + 1;
        }
    }

    IEnumerator SubscribeToOccupancy()
    {
        while (true)
        {
            string url = $"https://ps.pndsn.com/subscribe/{SUBSCRIBE_KEY}/{OCCUPANCY_CHANNEL}/0/{occupancyTimeToken}?uuid={userId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string response = request.downloadHandler.text;

                        // Extract timetoken
                        int lastBracket = response.LastIndexOf('"');
                        int secondLastQuote = response.LastIndexOf('"', lastBracket - 1);
                        if (secondLastQuote >= 0 && lastBracket > secondLastQuote)
                        {
                            occupancyTimeToken = response.Substring(secondLastQuote + 1, lastBracket - secondLastQuote - 1);
                        }

                        // Look for occupancy in response
                        int occIndex = response.IndexOf("\"occupancy\":");
                        if (occIndex >= 0)
                        {
                            int numStart = occIndex + 12;
                            int numEnd = numStart;
                            while (numEnd < response.Length && char.IsDigit(response[numEnd]))
                                numEnd++;

                            if (numEnd > numStart)
                            {
                                string occStr = response.Substring(numStart, numEnd - numStart);
                                if (int.TryParse(occStr, out int occ) && gameManager != null)
                                {
                                    gameManager.Occupancy = occ;
                                }
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // Parsing error
                    }
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    void RenderGhosts()
    {
        foreach (var kvp in ghosts)
        {
            string id = kvp.Key;
            GhostData data = kvp.Value;

            // Convert pixel coordinates to Unity coordinates
            float unityX = (data.x / 100f) - gameManager.GameWidth / 2f;
            float unityY = (data.y / 100f) - gameManager.GameHeight / 2f;

            if (!ghostObjects.ContainsKey(id))
            {
                // Create new ghost object
                GameObject ghostObj = CreateGhostObject();
                ghostObjects[id] = ghostObj;
            }

            ghostObjects[id].transform.position = new Vector3(unityX, unityY, 0);
        }
    }

    GameObject CreateGhostObject()
    {
        GameObject obj = new GameObject("Ghost");

        // Create circle sprite renderer
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.color = new Color(1, 1, 1, 0.5f);

        // Create circle texture
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];
        float center = resolution / 2f;
        float radius = resolution / 2f - 2;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Ring effect (outline only)
                if (dist <= radius && dist >= radius - 4)
                    colors[y * resolution + x] = Color.white;
                else
                    colors[y * resolution + x] = Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100);

        // Scale to player size
        float scale = 60f / 50f;
        obj.transform.localScale = new Vector3(scale, scale, 1);

        return obj;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
