using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>Multiplayer ghosts & player trail with enhanced visuals via PubNub</summary>
public class GhostManager : MonoBehaviour
{
    const string PUBLISH_KEY = "pub-c-ed826874-f30b-4d98-b87a-396accbe7f28";
    const string SUBSCRIBE_KEY = "sub-c-8d52d20d-c3a8-4d4b-bed7-5ee42ad6c4a0";
    const string GHOST_CHANNEL = "ghosts";
    const string OCCUPANCY_CHANNEL = "ai-game,ai-game-pnpres";
    const int TRAIL_LENGTH = 8;
    const float TRAIL_INTERVAL = 0.1f;

    string userId, timeToken = "0", occupancyTimeToken = "0";
    Dictionary<string, GhostData> ghosts = new Dictionary<string, GhostData>();
    Dictionary<string, GameObject> ghostObjects = new Dictionary<string, GameObject>();
    List<Vector2> trailPositions = new List<Vector2>();
    List<GameObject> trailObjects = new List<GameObject>();
    GameManager gm;
    int lastPublishFrame;
    float lastTrailTime;

    [System.Serializable]
    class GhostData { public string id; public float x, y; public string name; public int score; }

    void Start()
    {
        userId = "user-" + Random.Range(1, 1000000);
        gm = GameManager.Instance;
        StartCoroutine(SubscribeToGhosts());
        StartCoroutine(SubscribeToOccupancy());

        // Init trail objects with fading alpha and glow
        for (int i = 0; i < TRAIL_LENGTH; i++)
        {
            var obj = CreateGlowingTrailObject();
            var sr = obj.GetComponent<SpriteRenderer>();
            float alpha = 0.6f * (1f - (float)i / TRAIL_LENGTH);
            sr.color = new Color(0.3f, 0.8f, 1f, alpha);
            sr.sortingOrder = -1 - i;
            float scale = 0.6f * (1f - (float)i / TRAIL_LENGTH * 0.5f);
            obj.transform.localScale = Vector3.one * scale;
            obj.SetActive(false);
            trailObjects.Add(obj);
        }
    }

    void Update()
    {
        if (gm == null || gm.CurrentScene != GameScene.Play) return;

        // Publish position every 60 frames
        if (Time.frameCount - lastPublishFrame >= 60 && gm.Player != null)
        {
            lastPublishFrame = Time.frameCount;
            var pos = gm.Player.GetPosition();
            StartCoroutine(PublishPosition(
                (pos.x + gm.GameWidth / 2f) * 100f,
                (pos.y + gm.GameHeight / 2f) * 100f,
                gm.Player.entityName,
                gm.Score));
        }

        UpdateTrail();
        RenderGhosts();
    }

    void UpdateTrail()
    {
        if (gm.Player == null) return;

        if (Time.time - lastTrailTime >= TRAIL_INTERVAL)
        {
            lastTrailTime = Time.time;
            trailPositions.Insert(0, gm.Player.GetPosition());
            if (trailPositions.Count > TRAIL_LENGTH)
                trailPositions.RemoveAt(trailPositions.Count - 1);
        }

        // Animate trail with pulsing
        float pulse = Mathf.Sin(Time.time * 5f) * 0.1f + 1f;

        for (int i = 0; i < trailObjects.Count; i++)
        {
            if (i < trailPositions.Count)
            {
                trailObjects[i].SetActive(true);
                trailObjects[i].transform.position = new Vector3(trailPositions[i].x, trailPositions[i].y, 0.1f);

                var sr = trailObjects[i].GetComponent<SpriteRenderer>();
                float alpha = 0.5f * (1f - (float)i / TRAIL_LENGTH);

                // Color based on player state
                Color trailColor;
                if (gm.Player.hasInvincibility)
                    trailColor = new Color(0f, 1f, 1f, alpha);
                else
                    trailColor = new Color(gm.Player.entityColor.r, gm.Player.entityColor.g, gm.Player.entityColor.b, alpha);

                sr.color = trailColor;

                float baseScale = 0.5f * (1f - (float)i / TRAIL_LENGTH * 0.4f);
                trailObjects[i].transform.localScale = Vector3.one * baseScale * pulse;
            }
            else
            {
                trailObjects[i].SetActive(false);
            }
        }
    }

    IEnumerator PublishPosition(float x, float y, string name, int score)
    {
        var data = new GhostData { id = userId, x = x, y = y, name = name, score = score };
        var msg = UnityWebRequest.EscapeURL(JsonUtility.ToJson(data));
        using (var req = UnityWebRequest.Get($"https://ps.pndsn.com/publish/{PUBLISH_KEY}/{SUBSCRIBE_KEY}/0/{GHOST_CHANNEL}/0/{msg}?uuid={userId}"))
            yield return req.SendWebRequest();
    }

    IEnumerator SubscribeToGhosts()
    {
        while (true)
        {
            using (var req = UnityWebRequest.Get($"https://ps.pndsn.com/subscribe/{SUBSCRIBE_KEY}/{GHOST_CHANNEL}/0/{timeToken}?uuid={userId}"))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var r = req.downloadHandler.text;
                        int last = r.LastIndexOf('"'), second = r.LastIndexOf('"', last - 1);
                        if (second >= 0) timeToken = r.Substring(second + 1, last - second - 1);

                        int start = r.IndexOf("[["), end = r.IndexOf("]]");
                        if (start >= 0 && end > start) ParseGhostMessages(r.Substring(start + 1, end - start));
                    }
                    catch { }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void ParseGhostMessages(string msg)
    {
        int start = 0;
        while ((start = msg.IndexOf("{\"id\"", start)) >= 0)
        {
            int end = msg.IndexOf("}", start);
            if (end < 0) break;
            try
            {
                var ghost = JsonUtility.FromJson<GhostData>(msg.Substring(start, end - start + 1));
                if (ghost != null && !string.IsNullOrEmpty(ghost.id) && ghost.id != userId)
                    ghosts[ghost.id] = ghost;
            }
            catch { }
            start = end + 1;
        }
    }

    IEnumerator SubscribeToOccupancy()
    {
        while (true)
        {
            using (var req = UnityWebRequest.Get($"https://ps.pndsn.com/subscribe/{SUBSCRIBE_KEY}/{OCCUPANCY_CHANNEL}/0/{occupancyTimeToken}?uuid={userId}"))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var r = req.downloadHandler.text;
                        int last = r.LastIndexOf('"'), second = r.LastIndexOf('"', last - 1);
                        if (second >= 0) occupancyTimeToken = r.Substring(second + 1, last - second - 1);

                        int idx = r.IndexOf("\"occupancy\":");
                        if (idx >= 0)
                        {
                            int numStart = idx + 12, numEnd = numStart;
                            while (numEnd < r.Length && char.IsDigit(r[numEnd])) numEnd++;
                            if (numEnd > numStart && int.TryParse(r.Substring(numStart, numEnd - numStart), out int occ))
                                gm.Occupancy = occ;
                        }
                    }
                    catch { }
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    void RenderGhosts()
    {
        float pulse = Mathf.Sin(Time.time * 3f) * 0.1f + 1f;

        foreach (var kvp in ghosts)
        {
            float ux = (kvp.Value.x / 100f) - gm.GameWidth / 2f;
            float uy = (kvp.Value.y / 100f) - gm.GameHeight / 2f;

            if (!ghostObjects.ContainsKey(kvp.Key))
                ghostObjects[kvp.Key] = CreateGhostObject(kvp.Value.name);

            var ghost = ghostObjects[kvp.Key];
            ghost.transform.position = Vector3.Lerp(
                ghost.transform.position,
                new Vector3(ux, uy, 0),
                Time.deltaTime * 10f);
            ghost.transform.localScale = Vector3.one * 1.0f * pulse;

            // Update name label if exists
            var nameLabel = ghost.GetComponentInChildren<TextMesh>();
            if (nameLabel && !string.IsNullOrEmpty(kvp.Value.name))
                nameLabel.text = kvp.Value.name;
        }
    }

    GameObject CreateGlowingTrailObject()
    {
        var obj = new GameObject("TrailGlow");
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));

        // Create soft glow texture
        int res = 64;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - dist / center);
                alpha = alpha * alpha;
                colors[y * res + x] = new Color(1, 1, 1, alpha);
            }

        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);

        return obj;
    }

    GameObject CreateGhostObject(string name = null)
    {
        var obj = new GameObject("Ghost");
        var sr = obj.AddComponent<SpriteRenderer>();

        // Create color based on name for variety
        Color ghostColor;
        if (!string.IsNullOrEmpty(name))
        {
            ghostColor = Player.GetColorFromName(name);
            ghostColor.a = 0.6f;
        }
        else
        {
            ghostColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        }
        sr.color = ghostColor;

        // Shader with fallbacks
        var shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
            ?? Shader.Find("Unlit/Transparent");
        if (shader != null) sr.material = new Material(shader);
        sr.sortingOrder = 10;

        // Create circle texture with glow
        int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f, radius = center - 1;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha;
                if (dist <= radius * 0.7f)
                    alpha = 1f;
                else if (dist <= radius)
                    alpha = 1f - (dist - radius * 0.7f) / (radius * 0.3f);
                else
                    alpha = 0f;
                colors[y * res + x] = new Color(1, 1, 1, alpha * alpha);
            }

        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
        obj.transform.localScale = Vector3.one * 1.0f;

        // Add name label
        if (!string.IsNullOrEmpty(name))
        {
            var labelObj = new GameObject("GhostLabel");
            labelObj.transform.SetParent(obj.transform);
            labelObj.transform.localPosition = new Vector3(0, -0.8f, 0);
            var textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = name;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.fontSize = 20;
            textMesh.characterSize = 0.08f;
            textMesh.color = new Color(1, 1, 1, 0.7f);
            var mr = labelObj.GetComponent<MeshRenderer>();
            if (mr) mr.sortingOrder = 11;
        }

        return obj;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        foreach (var t in trailObjects) if (t) Destroy(t);
        trailObjects.Clear();
        foreach (var g in ghostObjects.Values) if (g) Destroy(g);
        ghostObjects.Clear();
    }
}
