using UnityEngine;
using System.Collections.Generic;

/// <summary>AI Enemy using neural network with enhanced visuals and behavior</summary>
public class AIEnemy : Entity
{
    public int level = 1;
    public int learning = 100;
    public int knowledge;

    [Header("Visual Effects")]
    public float rotationSpeed = 50f;
    public float aggressionPulse = 0.15f;

    NeuralNetwork nn;
    List<float> losses = new List<float>();
    GameManager gm;
    Player player;

    // Visual state
    float threatLevel;
    List<GameObject> orbitParticles = new List<GameObject>();
    float orbitAngle;

    // Slowdown state
    public bool isSlowed;
    float currentSlowAmount = 0.5f;

    public float[] Features { get; private set; } = new float[4];
    public float[] Labels { get; private set; } = new float[2];
    public float[] Output => nn?.LastOutput ?? new float[2];
    public float AverageLoss => losses.Count > 0 ? GetAvgLoss() : 0f;
    public float ThreatLevel => threatLevel;

    protected override void Awake()
    {
        base.Awake();
        nn = new NeuralNetwork();
        pulseSpeed = 3f;
        pulseAmount = aggressionPulse;
        CreateOrbitParticles();
    }

    protected override void Start()
    {
        base.Start();
        gm = GameManager.Instance;
        player = gm?.Player;
    }

    protected override void Update()
    {
        base.Update();

        if (gm == null || gm.CurrentScene != GameScene.Play) return;
        if (player == null && (player = gm.Player) == null) return;

        float w = gm.GameWidth, h = gm.GameHeight;
        Vector2 pPos = player.GetPosition(), aPos = GetPosition();

        // Calculate threat level for visual feedback
        float dist = Vector2.Distance(pPos, aPos);
        threatLevel = Mathf.Clamp01(1f - dist / 6f);

        // Training labels: direction to player
        Labels[0] = (pPos.x - aPos.x) / w;
        Labels[1] = (pPos.y - aPos.y) / h;

        // Features: positions
        Features[0] = pPos.x; Features[1] = pPos.y;
        Features[2] = aPos.x; Features[3] = aPos.y;

        // Neural network movement
        float[] output = TrainAndPredict();
        Vector3 newPos = transform.position;

        float currentSpeed = speed * (isSlowed ? currentSlowAmount : 1f);
        newPos.x += (w / 2f) * output[0] * Time.deltaTime * currentSpeed;
        newPos.y += (h / 2f) * output[1] * Time.deltaTime * currentSpeed;

        // Clamp to bounds
        newPos.x = Mathf.Clamp(newPos.x, -w / 2f, w / 2f);
        newPos.y = Mathf.Clamp(newPos.y, -h / 2f, h / 2f);
        transform.position = newPos;

        // Update visual effects
        UpdateRotation();
        UpdateOrbitParticles();
        UpdateThreatVisuals();

        // Level up check
        if (++knowledge > level * 3550) gm.NextLevel();
    }

    void CreateOrbitParticles()
    {
        for (int i = 0; i < 4; i++)
        {
            var particle = new GameObject("OrbitParticle");
            particle.transform.SetParent(transform);

            var sr = particle.AddComponent<SpriteRenderer>();
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.sortingOrder = 2;

            // Create small circle texture
            int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var colors = new Color[res * res];
            float center = res / 2f;

            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1f - d / center);
                    colors[y * res + x] = new Color(1, 1, 1, alpha * alpha);
                }

            tex.SetPixels(colors);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
            sr.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            particle.transform.localScale = Vector3.one * 0.15f;

            orbitParticles.Add(particle);
        }
    }

    void UpdateOrbitParticles()
    {
        // Rotation speed increases with level and threat
        float orbitSpeed = (50f + level * 20f) * (1f + threatLevel);
        orbitAngle += orbitSpeed * Time.deltaTime;

        float radius = 0.8f + threatLevel * 0.3f;
        float invScale = 1f / transform.localScale.x;

        for (int i = 0; i < orbitParticles.Count; i++)
        {
            float angle = (orbitAngle + i * 90f) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius * invScale;
            float y = Mathf.Sin(angle) * radius * invScale;
            orbitParticles[i].transform.localPosition = new Vector3(x, y, -0.05f);

            // Color based on threat
            var sr = orbitParticles[i].GetComponent<SpriteRenderer>();
            sr.color = Color.Lerp(new Color(1f, 0.5f, 0.2f, 0.6f), new Color(1f, 0f, 0f, 1f), threatLevel);
            orbitParticles[i].transform.localScale = Vector3.one * (0.1f + threatLevel * 0.1f) * invScale;
        }
    }

    void UpdateRotation()
    {
        // Rotation disabled - keep entity stable
        spriteRenderer.transform.localRotation = Quaternion.identity;
        if (glowRenderer)
            glowRenderer.transform.rotation = Quaternion.identity;
    }

    void UpdateThreatVisuals()
    {
        // Update pulse intensity based on threat
        pulseAmount = aggressionPulse * (1f + threatLevel);
        pulseSpeed = 3f + threatLevel * 3f;

        // Update color intensity
        float intensity = 0.6f + threatLevel * 0.4f;
        Color threatColor = new Color(intensity, 0.2f * (1f - threatLevel), 0.2f * (1f - threatLevel));
        spriteRenderer.color = Color.Lerp(entityColor, threatColor, threatLevel * 0.5f);

        // Glow intensifies when close
        if (glowRenderer)
        {
            Color glowColor = Color.Lerp(entityColor, Color.red, threatLevel);
            glowColor.a = glowIntensity * (1f + threatLevel);
            glowRenderer.color = glowColor;
        }
    }

    float[] TrainAndPredict()
    {
        float[] output = nn.Forward(Features);
        if (Time.frameCount % learning == 0)
        {
            nn.Train(Features, Labels);
            losses.Add(nn.LastLoss);
            if (losses.Count > 500) losses.RemoveAt(0);
        }
        return output;
    }

    float GetAvgLoss()
    {
        float sum = 0;
        foreach (var l in losses) sum += l;
        return sum / losses.Count;
    }

    public void SetLevel(int lvl, string name)
    {
        level = lvl;
        entityName = name;
        knowledge = 0;
        speed = 1 + lvl * 0.4f;
        size = 120 + lvl * 10;
        rotationSpeed = 50f + lvl * 15f;
        aggressionPulse = 0.15f + lvl * 0.02f;

        // Update entity color to get more menacing with level
        float r = Mathf.Min(1f, 0.8f + lvl * 0.05f);
        float gb = Mathf.Max(0f, 0.3f - lvl * 0.05f);
        entityColor = new Color(r, gb, gb);

        UpdateVisuals();
    }

    public void ApplySlowdown(float duration, float slowAmount = 0.5f)
    {
        isSlowed = true;
        currentSlowAmount = slowAmount;
        SetGlowColor(Color.blue);
        StartCoroutine(RemoveSlowdownAfterDelay(duration));
    }

    System.Collections.IEnumerator RemoveSlowdownAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isSlowed = false;
        SetGlowColor(entityColor);
    }

    public void ResetAI()
    {
        nn.Reset();
        losses.Clear();
        knowledge = 0;
        isSlowed = false;
        threatLevel = 0;
    }

    void OnDestroy()
    {
        foreach (var p in orbitParticles)
            if (p) Destroy(p);
        orbitParticles.Clear();
    }
}
