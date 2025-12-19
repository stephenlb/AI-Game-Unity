using UnityEngine;
using System.Collections.Generic;

public enum GameScene { Intro, Play, Level, GameOver }

/// <summary>Main game state manager with enhanced visual effects</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float GameWidth = 7.2f;
    public float GameHeight = 12.8f;

    [Header("References")]
    public Player Player;
    public AIEnemy AI;
    public UIManager UIManager;
    public GhostManager GhostManager;
    public Camera MainCamera;
    public SpriteRenderer BackgroundRenderer;

    [Header("Names")]
    public string[] PlayerNames = {
        "QuantifiedQuantum", "Kalamata", "EmoAImusic", "MD", "Torva",
        "Haidar", "BoboBear", "Mohamed", "Alucard", "Kevin",
        "Barry", "Uniqueux", "JanHoleman", "TheJAM", "megansub",
        "Dereck", "Kyle", "Tuleku", "Travis", "Valor",
        "Lukey", "Mosh", "Alazr", "Ahmed"
    };
    public string[] AINames = {
        "HAL9000", "Skynet", "Predator", "DeepBlue", "AlphaGo",
        "Watson", "Siri", "nAIma", "Aldan", "mAIa",
        "nAlma", "gAIl", "bAIley", "dAIsy"
    };

    [Header("Visual Effects")]
    public float screenFlashDuration = 0.3f;

    [Header("Game Over")]
    public float gameOverSlowMoDuration = 2f;
    public float gameOverTotalDuration = 8f;

    [Header("Combo System")]
    public float comboZoneRadius = 3f;
    public float comboScoreMultiplier = 2f;

    [Header("Power-ups")]
    public float powerUpSpawnInterval = 15f;
    public float powerUpDuration = 5f;

    [Header("High Score")]
    public int HighScore { get; private set; }
    public bool IsNewHighScore { get; private set; }

    [Header("Achievements")]
    public int nearMissBonusTotal;
    public int longestNearMissStreak;

    public GameScene CurrentScene { get; private set; } = GameScene.Play;
    public int Frame { get; private set; }
    public int WaitFrame { get; private set; }
    public float ShakeAmount { get; private set; }
    public int Score { get; private set; }
    public int Occupancy { get; set; }
    public int ComboCount { get; private set; }
    public float ComboMultiplier => 1f + ComboCount * 0.1f;

    // Visual effect state
    Color originalBgColor = new Color(0.1f, 0.8f, 0.5f);
    SpriteRenderer vignetteRenderer;
    SpriteRenderer screenFlashRenderer;
    SpriteRenderer[] boundaryRenderers = new SpriteRenderer[4];
    float screenFlashTime;
    Color screenFlashColor;
    float gradientTime;

    // Combo state
    float comboTime;
    float lastComboScore;
    bool inComboZone;

    // Power-up state
    float lastPowerUpSpawn;
    List<GameObject> activePowerUps = new List<GameObject>();

    // Time slow state
    bool isTimeSlowed;
    float timeSlowRemaining;
    float originalTimeScale = 1f;

    // Achievement/notification state
    Queue<string> notificationQueue = new Queue<string>();
    string currentNotification;
    float notificationTime;
    bool[] achievementsUnlocked = new bool[20];

    // Near miss tracking
    int currentNearMissBonus;

    // Visual effects
    List<GameObject> starParticles = new List<GameObject>();
    GameObject dangerZoneRing;
    SpriteRenderer dangerZoneRenderer;
    List<GameObject> ambientParticles = new List<GameObject>();
    List<GameObject> speedLines = new List<GameObject>();
    List<GameObject> impactRipples = new List<GameObject>();
    float starSpawnTimer;
    float ambientParticleTimer;
    float lastPlayerSpeed;
    Vector3 lastPlayerPos;

    // Game over state
    float gameOverTime;
    float gameOverPhase;
    int finalScore;
    Vector3 deathPosition;
    List<GameObject> deathParticles = new List<GameObject>();
    bool gameOverSequenceStarted;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start() => SetupGame();

    void SetupGame()
    {
        // Camera setup
        if (MainCamera == null) MainCamera = Camera.main;
        if (MainCamera == null)
        {
            var camObj = new GameObject("Main Camera") { tag = "MainCamera" };
            MainCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0, 0, -10);
        }
        if (!MainCamera.GetComponent<AudioListener>()) MainCamera.gameObject.AddComponent<AudioListener>();

        MainCamera.transform.position = new Vector3(MainCamera.transform.position.x, MainCamera.transform.position.y, -10);
        MainCamera.orthographic = true;
        MainCamera.orthographicSize = GameHeight / 2f;
        MainCamera.clearFlags = CameraClearFlags.SolidColor;
        MainCamera.backgroundColor = Color.black;
        MainCamera.cullingMask = -1;

        // Create entities if not assigned
        if (Player == null)
        {
            var obj = new GameObject("Player") { };
            obj.transform.position = new Vector3(0, -3, 0);
            Player = obj.AddComponent<Player>();
            Player.entityName = RandomName(PlayerNames);
            Player.entityColor = Player.GetColorFromName(Player.entityName);
            Player.textColor = new Color(0, 0.6f, 0.6f);
            Player.size = 60;
            Player.UpdateVisuals();
        }

        if (AI == null)
        {
            AI = new GameObject("AI").AddComponent<AIEnemy>();
            AI.entityName = RandomName(AINames);
            AI.entityColor = Color.red;
            AI.textColor = Color.white;
            AI.size = 120;
            AI.speed = 1;
            AI.SetLevel(1, AI.entityName);
            AI.UpdateVisuals();
        }

        if (UIManager == null) UIManager = new GameObject("UIManager").AddComponent<UIManager>();
        if (GhostManager == null) GhostManager = new GameObject("GhostManager").AddComponent<GhostManager>();
        if (AudioManager.Instance == null) new GameObject("AudioManager").AddComponent<AudioManager>();

        // Load high score
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
        longestNearMissStreak = PlayerPrefs.GetInt("LongestNearMissStreak", 0);

        // Background with gradient
        CreateGradientBackground();

        // Create vignette overlay
        CreateVignetteOverlay();

        // Create screen flash overlay
        CreateScreenFlashOverlay();

        // Create boundary visualization
        CreateBoundaryVisualization();

        // Create enhanced visual effects
        CreateStarField();
        CreateDangerZone();

        CurrentScene = GameScene.Play;
    }

    void CreateStarField()
    {
        // Create initial stars
        for (int i = 0; i < 50; i++)
        {
            SpawnStar(true);
        }
    }

    void SpawnStar(bool randomY = false)
    {
        var star = new GameObject("Star");
        star.transform.position = new Vector3(
            Random.Range(-GameWidth / 2f, GameWidth / 2f),
            randomY ? Random.Range(-GameHeight / 2f, GameHeight / 2f) : GameHeight / 2f + 0.5f,
            5f
        );

        var sr = star.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = -8;

        // Create star texture
        int res = 16;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - dist / center);
                alpha = alpha * alpha * alpha;
                colors[y * res + x] = new Color(1, 1, 1, alpha);
            }
        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);

        // Random star properties
        float brightness = Random.Range(0.3f, 1f);
        float hue = Random.Range(0.5f, 0.7f); // Cyan to blue range
        sr.color = Color.HSVToRGB(hue, Random.Range(0.1f, 0.4f), brightness);
        star.transform.localScale = Vector3.one * Random.Range(0.05f, 0.2f);

        var starData = star.AddComponent<StarParticle>();
        starData.speed = Random.Range(0.5f, 2f);
        starData.twinkleSpeed = Random.Range(2f, 6f);
        starData.baseAlpha = brightness;

        starParticles.Add(star);
    }

    void CreateDangerZone()
    {
        dangerZoneRing = new GameObject("DangerZone");
        dangerZoneRing.transform.position = Vector3.zero;

        dangerZoneRenderer = dangerZoneRing.AddComponent<SpriteRenderer>();
        dangerZoneRenderer.material = new Material(Shader.Find("Sprites/Default"));
        dangerZoneRenderer.sortingOrder = -5;

        // Create ring texture
        int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;
        float innerRadius = 0.7f;
        float outerRadius = 1f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = 0f;
                if (dist >= innerRadius && dist <= outerRadius)
                {
                    float ringPos = (dist - innerRadius) / (outerRadius - innerRadius);
                    alpha = Mathf.Sin(ringPos * Mathf.PI) * 0.8f;
                }
                colors[y * res + x] = new Color(1, 1, 1, alpha);
            }

        tex.SetPixels(colors);
        tex.Apply();
        dangerZoneRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 10);
        dangerZoneRenderer.color = new Color(1, 0, 0, 0);
    }

    void CreateGradientBackground()
    {
        if (BackgroundRenderer == null)
        {
            var bg = new GameObject("Background");
            bg.transform.position = new Vector3(0, 0, 10);
            BackgroundRenderer = bg.AddComponent<SpriteRenderer>();
            BackgroundRenderer.material = new Material(Shader.Find("Sprites/Default"));
            BackgroundRenderer.sortingOrder = -10;
        }

        // Create gradient texture
        int w = 128, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            Color topColor = new Color(0.05f, 0.15f, 0.25f);
            Color bottomColor = new Color(0.1f, 0.4f, 0.3f);
            Color c = Color.Lerp(bottomColor, topColor, t);

            for (int x = 0; x < w; x++)
                colors[y * w + x] = c;
        }

        tex.SetPixels(colors);
        tex.Apply();
        BackgroundRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10);
        BackgroundRenderer.transform.localScale = new Vector3(GameWidth, GameHeight, 1);
    }

    void CreateVignetteOverlay()
    {
        var obj = new GameObject("Vignette");
        obj.transform.position = new Vector3(0, 0, -5);
        vignetteRenderer = obj.AddComponent<SpriteRenderer>();
        vignetteRenderer.material = new Material(Shader.Find("Sprites/Default"));
        vignetteRenderer.sortingOrder = 100;

        // Create vignette texture
        int res = 256;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = Mathf.Pow(dist, 2f) * 0.8f;
                colors[y * res + x] = new Color(0, 0, 0, alpha);
            }

        tex.SetPixels(colors);
        tex.Apply();
        vignetteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 10);
        vignetteRenderer.transform.localScale = new Vector3(GameWidth * 1.5f, GameHeight * 1.5f, 1);
        vignetteRenderer.color = new Color(1, 1, 1, 0);
    }

    void CreateScreenFlashOverlay()
    {
        var obj = new GameObject("ScreenFlash");
        obj.transform.position = new Vector3(0, 0, -6);
        screenFlashRenderer = obj.AddComponent<SpriteRenderer>();
        screenFlashRenderer.material = new Material(Shader.Find("Sprites/Default"));
        screenFlashRenderer.sortingOrder = 101;

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        screenFlashRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        screenFlashRenderer.transform.localScale = new Vector3(GameWidth * 2f, GameHeight * 2f, 1);
        screenFlashRenderer.color = new Color(1, 1, 1, 0);
    }

    void CreateBoundaryVisualization()
    {
        float thickness = 0.1f;
        Color boundaryColor = new Color(0.3f, 0.8f, 1f, 0.4f);

        // Create glowing boundary lines
        Vector3[] positions = {
            new Vector3(0, GameHeight / 2f, 0), // Top
            new Vector3(0, -GameHeight / 2f, 0), // Bottom
            new Vector3(-GameWidth / 2f, 0, 0), // Left
            new Vector3(GameWidth / 2f, 0, 0)  // Right
        };

        Vector3[] scales = {
            new Vector3(GameWidth, thickness, 1),
            new Vector3(GameWidth, thickness, 1),
            new Vector3(thickness, GameHeight, 1),
            new Vector3(thickness, GameHeight, 1)
        };

        for (int i = 0; i < 4; i++)
        {
            var obj = new GameObject($"Boundary{i}");
            obj.transform.position = positions[i];
            obj.transform.localScale = scales[i];

            boundaryRenderers[i] = obj.AddComponent<SpriteRenderer>();
            boundaryRenderers[i].material = new Material(Shader.Find("Sprites/Default"));
            boundaryRenderers[i].sortingOrder = 50;

            // Create gradient texture for glow effect
            int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var colors = new Color[res * res];
            float center = res / 2f;

            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float distFromCenter = Mathf.Abs(y - center) / center;
                    float alpha = 1f - distFromCenter;
                    colors[y * res + x] = new Color(1, 1, 1, alpha * alpha);
                }

            tex.SetPixels(colors);
            tex.Apply();
            boundaryRenderers[i].sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 10);
            boundaryRenderers[i].color = boundaryColor;
        }
    }

    void Update()
    {
        Frame++;
        UpdateScreenFlash();
        UpdateGradientAnimation();
        UpdateBoundaryAnimation();
        UpdateTimeSlow();
        UpdateNotifications();
        CheckAchievements();
        UpdateStarField();
        UpdateDangerZone();
        UpdateAmbientParticles();
        UpdateSpeedLines();
        UpdateImpactRipples();
        UpdateProximityEffects();

        switch (CurrentScene)
        {
            case GameScene.Play:
                UpdatePlay();
                UpdateCombo();
                UpdatePowerUpSpawning();
                break;
            case GameScene.GameOver:
                UpdateGameOverSequence();
                break;
            case GameScene.Level:
                if (Frame > WaitFrame + 500) { WaitFrame = Frame; CurrentScene = GameScene.Play; }
                break;
        }
    }

    void UpdateTimeSlow()
    {
        if (isTimeSlowed)
        {
            timeSlowRemaining -= Time.unscaledDeltaTime;
            if (timeSlowRemaining <= 0)
            {
                isTimeSlowed = false;
                Time.timeScale = originalTimeScale;
            }
        }
    }

    void UpdateStarField()
    {
        // Spawn new stars occasionally
        starSpawnTimer += Time.deltaTime;
        if (starSpawnTimer > 0.3f)
        {
            starSpawnTimer = 0;
            if (starParticles.Count < 80)
                SpawnStar();
        }

        // Update existing stars
        for (int i = starParticles.Count - 1; i >= 0; i--)
        {
            var star = starParticles[i];
            if (star == null)
            {
                starParticles.RemoveAt(i);
                continue;
            }

            var data = star.GetComponent<StarParticle>();
            if (data == null) continue;

            // Move downward
            star.transform.position += Vector3.down * data.speed * Time.deltaTime;

            // Twinkle effect
            var sr = star.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float twinkle = Mathf.Sin(Time.time * data.twinkleSpeed + i) * 0.3f + 0.7f;
                Color c = sr.color;
                c.a = data.baseAlpha * twinkle;
                sr.color = c;
            }

            // Remove if off screen
            if (star.transform.position.y < -GameHeight / 2f - 1f)
            {
                starParticles.RemoveAt(i);
                Destroy(star);
            }
        }
    }

    void UpdateDangerZone()
    {
        if (dangerZoneRenderer == null || AI == null || Player == null) return;

        float dist = Vector2.Distance(Player.GetPosition(), AI.GetPosition());
        float threat = AI.ThreatLevel;

        // Position at AI
        dangerZoneRing.transform.position = new Vector3(AI.transform.position.x, AI.transform.position.y, 1f);

        // Scale based on combo zone radius
        float baseScale = comboZoneRadius * 2f;
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.05f * threat;
        dangerZoneRing.transform.localScale = Vector3.one * baseScale * pulse;

        // Color and alpha based on threat
        float alpha = threat * 0.4f;
        float hue = Mathf.Lerp(0.1f, 0f, threat); // Orange to red
        Color zoneColor = Color.HSVToRGB(hue, 0.8f, 1f);
        zoneColor.a = alpha;
        dangerZoneRenderer.color = zoneColor;

        // Rotation
        dangerZoneRing.transform.Rotate(0, 0, 30f * Time.deltaTime * (1f + threat));
    }

    void UpdateAmbientParticles()
    {
        if (CurrentScene != GameScene.Play) return;

        // Spawn ambient particles based on game intensity
        ambientParticleTimer += Time.deltaTime;
        float spawnRate = AI != null ? 0.2f - AI.ThreatLevel * 0.1f : 0.2f;

        if (ambientParticleTimer > spawnRate && ambientParticles.Count < 30)
        {
            ambientParticleTimer = 0;
            SpawnAmbientParticle();
        }

        // Update particles
        for (int i = ambientParticles.Count - 1; i >= 0; i--)
        {
            var p = ambientParticles[i];
            if (p == null)
            {
                ambientParticles.RemoveAt(i);
                continue;
            }

            var data = p.GetComponent<AmbientParticle>();
            if (data == null) continue;

            data.lifetime -= Time.deltaTime;

            // Float upward with slight wave
            float wave = Mathf.Sin(Time.time * data.waveSpeed + i) * data.waveAmount;
            p.transform.position += new Vector3(wave * Time.deltaTime, data.speed * Time.deltaTime, 0);

            // Fade out
            var sr = p.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float alpha = Mathf.Clamp01(data.lifetime / data.maxLifetime) * data.baseAlpha;
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            // Remove if done
            if (data.lifetime <= 0)
            {
                ambientParticles.RemoveAt(i);
                Destroy(p);
            }
        }
    }

    void SpawnAmbientParticle()
    {
        var particle = new GameObject("AmbientParticle");
        particle.transform.position = new Vector3(
            Random.Range(-GameWidth / 2f, GameWidth / 2f),
            -GameHeight / 2f - 0.5f,
            2f
        );

        var sr = particle.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = -6;

        // Create soft particle texture
        int res = 24;
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

        // Color based on game state
        float threat = AI != null ? AI.ThreatLevel : 0;
        Color particleColor = Color.Lerp(
            new Color(0.3f, 0.8f, 1f, 0.4f), // Calm cyan
            new Color(1f, 0.5f, 0.3f, 0.5f), // Intense orange
            threat
        );
        sr.color = particleColor;
        particle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);

        var data = particle.AddComponent<AmbientParticle>();
        data.speed = Random.Range(0.8f, 1.5f);
        data.waveSpeed = Random.Range(1f, 3f);
        data.waveAmount = Random.Range(0.3f, 0.8f);
        data.lifetime = Random.Range(8f, 12f);
        data.maxLifetime = data.lifetime;
        data.baseAlpha = particleColor.a;

        ambientParticles.Add(particle);
    }

    void UpdateSpeedLines()
    {
        if (CurrentScene != GameScene.Play || Player == null) return;

        // Calculate player speed
        Vector3 currentPos = Player.transform.position;
        float speed = (currentPos - lastPlayerPos).magnitude / Time.deltaTime;
        lastPlayerPos = currentPos;

        // Spawn speed lines when moving fast
        if (speed > 5f && speedLines.Count < 20)
        {
            Vector3 moveDir = (currentPos - lastPlayerPos).normalized;
            if (moveDir == Vector3.zero) moveDir = Vector3.right;

            SpawnSpeedLine(currentPos, -moveDir);
        }

        // Update existing speed lines
        for (int i = speedLines.Count - 1; i >= 0; i--)
        {
            var line = speedLines[i];
            if (line == null)
            {
                speedLines.RemoveAt(i);
                continue;
            }

            var data = line.GetComponent<SpeedLineData>();
            if (data == null) continue;

            data.lifetime -= Time.deltaTime;
            line.transform.position += data.velocity * Time.deltaTime;

            var sr = line.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float alpha = Mathf.Clamp01(data.lifetime / data.maxLifetime);
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha * 0.6f);
            }

            if (data.lifetime <= 0)
            {
                speedLines.RemoveAt(i);
                Destroy(line);
            }
        }
    }

    void SpawnSpeedLine(Vector3 position, Vector3 direction)
    {
        var line = new GameObject("SpeedLine");
        float offset = Random.Range(-0.5f, 0.5f);
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
        line.transform.position = position + perpendicular * offset + direction * Random.Range(0.5f, 1.5f);
        line.transform.position = new Vector3(line.transform.position.x, line.transform.position.y, 3f);

        var sr = line.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = 50;

        // Create line texture
        int width = 64, height = 8;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float xT = (float)x / width;
                float yT = Mathf.Abs((float)y / height - 0.5f) * 2f;
                float alpha = (1f - xT) * (1f - yT * yT);
                colors[y * width + x] = new Color(1, 1, 1, alpha);
            }
        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0f, 0.5f), 100);

        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        line.transform.rotation = Quaternion.Euler(0, 0, angle);
        line.transform.localScale = new Vector3(Random.Range(0.3f, 0.6f), Random.Range(0.1f, 0.2f), 1);

        sr.color = Color.Lerp(Player.entityColor, Color.white, 0.5f);

        var data = line.AddComponent<SpeedLineData>();
        data.velocity = direction * Random.Range(3f, 6f);
        data.lifetime = Random.Range(0.15f, 0.3f);
        data.maxLifetime = data.lifetime;

        speedLines.Add(line);
    }

    void UpdateImpactRipples()
    {
        for (int i = impactRipples.Count - 1; i >= 0; i--)
        {
            var ripple = impactRipples[i];
            if (ripple == null)
            {
                impactRipples.RemoveAt(i);
                continue;
            }

            var data = ripple.GetComponent<RippleData>();
            if (data == null) continue;

            data.lifetime -= Time.deltaTime;
            float progress = 1f - (data.lifetime / data.maxLifetime);

            // Expand and fade
            float scale = Mathf.Lerp(data.startScale, data.endScale, progress);
            ripple.transform.localScale = Vector3.one * scale;

            var sr = ripple.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float alpha = 1f - progress;
                sr.color = new Color(data.color.r, data.color.g, data.color.b, alpha * 0.5f);
            }

            if (data.lifetime <= 0)
            {
                impactRipples.RemoveAt(i);
                Destroy(ripple);
            }
        }
    }

    void UpdateProximityEffects()
    {
        if (CurrentScene != GameScene.Play || Player == null || AI == null) return;

        float dist = Vector2.Distance(Player.GetPosition(), AI.GetPosition());
        float threat = AI.ThreatLevel;

        // Spawn ripples when very close
        if (dist < 2f && Random.value < 0.1f * threat)
        {
            Vector3 midpoint = (Player.transform.position + AI.transform.position) / 2f;
            SpawnImpactRipple(midpoint, Color.Lerp(Color.yellow, Color.red, threat), 0.5f, 2f);
        }

        // Screen chromatic pulse when in danger
        if (dist < 1.5f)
        {
            float intensity = (1.5f - dist) / 1.5f;
            if (vignetteRenderer != null)
            {
                Color vignetteColor = Color.Lerp(new Color(1, 0, 0, 0), new Color(1, 0, 0, 0.4f), intensity * threat);
                vignetteRenderer.color = Color.Lerp(vignetteRenderer.color, vignetteColor, Time.deltaTime * 5f);
            }
        }
    }

    public void SpawnImpactRipple(Vector3 position, Color color, float startScale, float endScale)
    {
        var ripple = new GameObject("ImpactRipple");
        ripple.transform.position = new Vector3(position.x, position.y, 2f);

        var sr = ripple.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = 45;

        // Create ring texture
        int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center)) / center;
                float ringAlpha = 0f;
                if (dist > 0.7f && dist < 1f)
                {
                    float ringPos = (dist - 0.7f) / 0.3f;
                    ringAlpha = Mathf.Sin(ringPos * Mathf.PI);
                }
                colors[y * res + x] = new Color(1, 1, 1, ringAlpha);
            }

        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 50);
        sr.color = color;
        ripple.transform.localScale = Vector3.one * startScale;

        var data = ripple.AddComponent<RippleData>();
        data.lifetime = 0.5f;
        data.maxLifetime = 0.5f;
        data.startScale = startScale;
        data.endScale = endScale;
        data.color = color;

        impactRipples.Add(ripple);
    }

    public void SpawnScorePopup(Vector3 position, int score, Color color)
    {
        var popup = new GameObject("ScorePopup");
        popup.transform.position = position;

        var tm = popup.AddComponent<TextMesh>();
        tm.text = "+" + score;
        tm.fontSize = 32;
        tm.characterSize = 0.15f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;

        var mr = popup.GetComponent<MeshRenderer>();
        if (mr) mr.sortingOrder = 200;

        StartCoroutine(AnimateScorePopup(popup));
    }

    System.Collections.IEnumerator AnimateScorePopup(GameObject popup)
    {
        float elapsed = 0f;
        float duration = 1f;
        Vector3 startPos = popup.transform.position;
        var tm = popup.GetComponent<TextMesh>();
        Color startColor = tm.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Float upward with easing
            popup.transform.position = startPos + Vector3.up * t * 1.5f;

            // Scale pop effect
            float scale = t < 0.2f ? Mathf.Lerp(0.5f, 1.2f, t / 0.2f) : Mathf.Lerp(1.2f, 1f, (t - 0.2f) / 0.8f);
            popup.transform.localScale = Vector3.one * scale;

            // Fade out
            tm.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        Destroy(popup);
    }

    public void SpawnPowerUpBurst(Vector3 position, Color color)
    {
        // Ring explosion
        SpawnImpactRipple(position, color, 0.3f, 3f);

        // Particle burst
        for (int i = 0; i < 20; i++)
        {
            var particle = new GameObject("PowerUpParticle");
            particle.transform.position = position;

            var sr = particle.AddComponent<SpriteRenderer>();
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.sortingOrder = 100;

            // Create soft particle
            int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var colors = new Color[res * res];
            float center = res / 2f;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha;
                    colors[y * res + x] = new Color(1, 1, 1, alpha);
                }
            tex.SetPixels(colors);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);

            sr.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.5f));
            particle.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * Random.Range(3f, 7f);

            StartCoroutine(AnimateBurstParticle(particle, velocity));
        }

        // Screen flash
        TriggerScreenFlash(color, 0.15f);
    }

    System.Collections.IEnumerator AnimateBurstParticle(GameObject particle, Vector3 velocity)
    {
        float elapsed = 0f;
        float duration = Random.Range(0.4f, 0.7f);
        var sr = particle.GetComponent<SpriteRenderer>();
        Color startColor = sr.color;

        while (elapsed < duration && particle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            particle.transform.position += velocity * Time.deltaTime;
            velocity *= 0.95f; // Slow down

            float scale = (1f - t) * 0.25f;
            particle.transform.localScale = Vector3.one * scale;
            sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        if (particle != null) Destroy(particle);
    }

    public void SpawnComboBurst(Vector3 position, int comboCount)
    {
        Color comboColor = Color.Lerp(Color.yellow, Color.red, Mathf.Min(comboCount / 10f, 1f));

        // Expanding ring
        SpawnImpactRipple(position, comboColor, 0.2f, 1.5f);

        // Sparkles
        for (int i = 0; i < comboCount * 2; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(0.3f, 0.8f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * dist;

            var sparkle = new GameObject("ComboSparkle");
            sparkle.transform.position = position + offset;

            var sr = sparkle.AddComponent<SpriteRenderer>();
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.sortingOrder = 110;

            // Star shape texture
            int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var colors = new Color[res * res];
            float center = res / 2f;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float starShape = Mathf.Abs(Mathf.Cos(ang * 2f)) * 0.5f + 0.5f;
                    float alpha = Mathf.Clamp01((starShape - d) * 3f);
                    colors[y * res + x] = new Color(1, 1, 1, alpha);
                }
            tex.SetPixels(colors);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);

            sr.color = comboColor;
            sparkle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);

            StartCoroutine(AnimateSparkle(sparkle, offset.normalized * Random.Range(1f, 2f)));
        }
    }

    System.Collections.IEnumerator AnimateSparkle(GameObject sparkle, Vector3 velocity)
    {
        float elapsed = 0f;
        float duration = 0.5f;
        var sr = sparkle.GetComponent<SpriteRenderer>();
        Color startColor = sr.color;
        Vector3 startScale = sparkle.transform.localScale;

        while (elapsed < duration && sparkle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            sparkle.transform.position += velocity * Time.deltaTime * (1f - t);
            sparkle.transform.Rotate(0, 0, 360f * Time.deltaTime);
            sparkle.transform.localScale = startScale * (1f - t);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        if (sparkle != null) Destroy(sparkle);
    }

    void UpdateNotifications()
    {
        if (!string.IsNullOrEmpty(currentNotification))
        {
            notificationTime -= Time.deltaTime;
            if (notificationTime <= 0)
            {
                currentNotification = null;
            }
        }
        else if (notificationQueue.Count > 0)
        {
            currentNotification = notificationQueue.Dequeue();
            notificationTime = 2f;
        }
    }

    void CheckAchievements()
    {
        if (CurrentScene != GameScene.Play) return;

        int currentScore = Score / 6;

        // Score milestones
        if (currentScore >= 100 && !achievementsUnlocked[0])
        {
            achievementsUnlocked[0] = true;
            ShowNotification("SURVIVOR! 100 points!");
        }
        if (currentScore >= 500 && !achievementsUnlocked[1])
        {
            achievementsUnlocked[1] = true;
            ShowNotification("ENDURANCE! 500 points!");
        }
        if (currentScore >= 1000 && !achievementsUnlocked[2])
        {
            achievementsUnlocked[2] = true;
            ShowNotification("LEGENDARY! 1000 points!");
        }

        // Combo milestones
        if (ComboCount >= 3 && !achievementsUnlocked[3])
        {
            achievementsUnlocked[3] = true;
            ShowNotification("COMBO STARTER! 3x combo!");
        }
        if (ComboCount >= 5 && !achievementsUnlocked[4])
        {
            achievementsUnlocked[4] = true;
            ShowNotification("COMBO MASTER! 5x combo!");
        }
        if (ComboCount >= 10 && !achievementsUnlocked[5])
        {
            achievementsUnlocked[5] = true;
            ShowNotification("COMBO LEGEND! 10x combo!");
        }

        // Near miss milestones
        if (Player != null && Player.NearMissStreak >= 3 && !achievementsUnlocked[6])
        {
            achievementsUnlocked[6] = true;
            ShowNotification("DAREDEVIL! 3 near misses!");
        }
        if (nearMissBonusTotal >= 500 && !achievementsUnlocked[7])
        {
            achievementsUnlocked[7] = true;
            ShowNotification("RISK TAKER! 500 near miss points!");
        }

        // Level milestones
        if (AI != null && AI.level >= 3 && !achievementsUnlocked[8])
        {
            achievementsUnlocked[8] = true;
            ShowNotification("LEVEL 3! AI is learning...");
        }
        if (AI != null && AI.level >= 5 && !achievementsUnlocked[9])
        {
            achievementsUnlocked[9] = true;
            ShowNotification("LEVEL 5! The machine awakens!");
        }
    }

    void UpdatePlay()
    {
        if (Player == null || AI == null) return;
        Score = (int)(Frame * ComboMultiplier);

        float dist = Vector2.Distance(Player.GetPosition(), AI.GetPosition());
        float combinedRadius = (AI.size + Player.size) / 100f;
        float threshold = 4f;

        if (dist <= threshold)
        {
            ShakeAmount = (threshold - dist) * 12.5f;
            UpdateDangerEffects(dist, threshold);
        }
        else
        {
            ShakeAmount = 0;
            if (vignetteRenderer) vignetteRenderer.color = new Color(1, 0, 0, 0);
        }

        // Check collision (with invincibility)
        if (Frame > WaitFrame + 100 && dist <= combinedRadius && !Player.HasInvincibility)
        {
            StartGameOverSequence();
        }
    }

    void UpdateDangerEffects(float dist, float threshold)
    {
        // Vignette intensity based on distance
        float danger = Mathf.Clamp01((threshold - dist) / threshold);
        if (vignetteRenderer)
        {
            Color c = Color.Lerp(new Color(0, 0, 0, 0), new Color(1, 0, 0, 0.6f), danger);
            vignetteRenderer.color = c;
        }

        // Camera shake
        if (MainCamera && ShakeAmount > 0)
        {
            float s = ShakeAmount * 0.01f;
            MainCamera.transform.position = new Vector3(Random.Range(-s, s), Random.Range(-s, s), MainCamera.transform.position.z);
        }
    }

    void UpdateCombo()
    {
        if (Player == null || AI == null) return;

        float dist = Vector2.Distance(Player.GetPosition(), AI.GetPosition());
        bool wasInZone = inComboZone;
        inComboZone = dist <= comboZoneRadius && dist > (AI.size + Player.size) / 100f;

        if (inComboZone)
        {
            comboTime += Time.deltaTime;
            if (comboTime >= 1f)
            {
                ComboCount++;
                comboTime = 0;
                AudioManager.Instance?.PlaySFX(AudioManager.SFXType.Combo);

                // Visual feedback - burst effect
                SpawnComboBurst(Player.transform.position, ComboCount);
                TriggerScreenFlash(Color.yellow, 0.1f);

                // Score popup
                int comboScore = (int)(10 * ComboMultiplier);
                SpawnScorePopup(Player.transform.position + Vector3.up * 0.3f, comboScore, Color.yellow);
            }
        }
        else if (wasInZone && !inComboZone)
        {
            // Lost combo
            if (ComboCount > 0)
            {
                ComboCount = 0;
                comboTime = 0;
            }
        }
    }

    void UpdatePowerUpSpawning()
    {
        if (Time.time - lastPowerUpSpawn < powerUpSpawnInterval) return;
        lastPowerUpSpawn = Time.time;

        // Random chance to spawn power-up
        if (Random.value < 0.5f)
            SpawnPowerUp();
    }

    void SpawnPowerUp()
    {
        var obj = new GameObject("PowerUp");
        float x = Random.Range(-GameWidth / 2f + 1f, GameWidth / 2f - 1f);
        float y = Random.Range(-GameHeight / 2f + 1f, GameHeight / 2f - 1f);
        obj.transform.position = new Vector3(x, y, 0);

        var powerUp = obj.AddComponent<PowerUp>();
        // Weighted random: common power-ups more likely
        float roll = Random.value;
        if (roll < 0.3f)
            powerUp.type = PowerUpType.SpeedBoost;
        else if (roll < 0.5f)
            powerUp.type = PowerUpType.Invincibility;
        else if (roll < 0.7f)
            powerUp.type = PowerUpType.AISlowdown;
        else if (roll < 0.85f)
            powerUp.type = PowerUpType.TimeSlow;
        else
            powerUp.type = PowerUpType.Teleport;

        powerUp.duration = powerUpDuration;

        activePowerUps.Add(obj);
    }

    public void ShowNotification(string message)
    {
        notificationQueue.Enqueue(message);
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.Combo);
    }

    public void AddNearMissBonus(int bonus)
    {
        Score += bonus * 6; // Multiply by 6 since we divide by 6 for display
        nearMissBonusTotal += bonus;
        currentNearMissBonus += bonus;
    }

    public void OnNearMissEscape(int streak)
    {
        if (streak > longestNearMissStreak)
        {
            longestNearMissStreak = streak;
            PlayerPrefs.SetInt("LongestNearMissStreak", streak);
            PlayerPrefs.Save();
            ShowNotification($"NEW RECORD! {streak} near miss streak!");
        }
    }

    public void ApplyTimeSlow(float duration, float slowFactor = 0.3f)
    {
        if (!isTimeSlowed)
        {
            originalTimeScale = Time.timeScale;
        }
        isTimeSlowed = true;
        timeSlowRemaining = duration;
        Time.timeScale = slowFactor;
        TriggerScreenFlash(new Color(0.5f, 0f, 1f), 0.2f); // Purple flash
    }

    public void TeleportPlayer()
    {
        if (Player == null || AI == null) return;

        // Find a safe position away from AI
        Vector3 newPos;
        int attempts = 0;
        do
        {
            newPos = new Vector3(
                Random.Range(-GameWidth / 2f + 1f, GameWidth / 2f - 1f),
                Random.Range(-GameHeight / 2f + 1f, GameHeight / 2f - 1f),
                0
            );
            attempts++;
        } while (Vector3.Distance(newPos, AI.transform.position) < 3f && attempts < 20);

        // Spawn particles at old position
        SpawnTeleportParticles(Player.transform.position, Player.entityColor);

        // Move player
        Player.transform.position = newPos;

        // Spawn particles at new position
        SpawnTeleportParticles(newPos, Player.entityColor);

        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.PowerUp);
        TriggerScreenFlash(Color.white, 0.15f);
    }

    void SpawnTeleportParticles(Vector3 position, Color color)
    {
        for (int i = 0; i < 12; i++)
        {
            var particle = new GameObject("TeleportParticle");
            particle.transform.position = position;

            var sr = particle.AddComponent<SpriteRenderer>();
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.sortingOrder = 150;

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
            sr.color = color;
            particle.transform.localScale = Vector3.one * 0.3f;

            float angle = i * 30f * Mathf.Deg2Rad;
            StartCoroutine(AnimateTeleportParticle(particle, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 2f));
        }
    }

    System.Collections.IEnumerator AnimateTeleportParticle(GameObject particle, Vector3 velocity)
    {
        float elapsed = 0f;
        float duration = 0.4f;
        Vector3 startPos = particle.transform.position;
        var sr = particle.GetComponent<SpriteRenderer>();
        Color startColor = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            particle.transform.position = startPos + velocity * t;
            particle.transform.localScale = Vector3.one * (1f - t) * 0.3f;
            sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        Destroy(particle);
    }

    public string CurrentNotification => currentNotification;
    public float NotificationTime => notificationTime;
    public int NearMissBonusTotal => nearMissBonusTotal;

    void UpdateScreenFlash()
    {
        if (screenFlashTime > 0)
        {
            screenFlashTime -= Time.deltaTime;
            float alpha = screenFlashTime / screenFlashDuration;
            screenFlashRenderer.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, alpha * 0.5f);
        }
        else if (screenFlashRenderer.color.a > 0)
        {
            screenFlashRenderer.color = new Color(1, 1, 1, 0);
        }
    }

    void UpdateGradientAnimation()
    {
        gradientTime += Time.deltaTime * 0.2f;

        // Subtle color shift based on game state
        float hueShift = Mathf.Sin(gradientTime) * 0.05f;
        float saturation = CurrentScene == GameScene.Play ? 0.6f : 0.3f;

        if (BackgroundRenderer)
        {
            Color tint = Color.HSVToRGB(0.45f + hueShift, saturation, 0.8f);
            BackgroundRenderer.color = tint;
        }
    }

    void UpdateBoundaryAnimation()
    {
        float pulse = Mathf.Sin(Time.time * 2f) * 0.2f + 0.6f;
        Color boundaryColor = new Color(0.3f, 0.8f, 1f, pulse * 0.3f);

        // Intensify near player
        if (Player != null)
        {
            Vector2 pPos = Player.GetPosition();
            float[] distances = {
                Mathf.Abs(pPos.y - GameHeight / 2f),
                Mathf.Abs(pPos.y + GameHeight / 2f),
                Mathf.Abs(pPos.x + GameWidth / 2f),
                Mathf.Abs(pPos.x - GameWidth / 2f)
            };

            for (int i = 0; i < 4; i++)
            {
                float proximity = 1f - Mathf.Clamp01(distances[i] / 2f);
                Color c = Color.Lerp(boundaryColor, new Color(1f, 0.5f, 0.2f, 0.8f), proximity);
                if (boundaryRenderers[i]) boundaryRenderers[i].color = c;
            }
        }
    }

    public void TriggerScreenFlash(Color color, float duration)
    {
        screenFlashColor = color;
        screenFlashTime = duration;
        screenFlashDuration = duration;
    }

    public void NextLevel()
    {
        WaitFrame = Frame;
        CurrentScene = GameScene.Level;
        AI.SetLevel(AI.level + 1, RandomName(AINames));
        TriggerScreenFlash(Color.white, 0.3f);
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.LevelUp);

        // Change music based on new level
        AudioManager.Instance?.PlayMusicForLevel(AI.level);
    }

    void StartGameOverSequence()
    {
        if (gameOverSequenceStarted) return;
        gameOverSequenceStarted = true;

        WaitFrame = Frame;
        CurrentScene = GameScene.GameOver;
        finalScore = Score / 6;
        deathPosition = Player.transform.position;
        gameOverTime = 0;
        gameOverPhase = 0;

        // Reset time slow if active
        isTimeSlowed = false;

        // Check for new high score
        if (finalScore > HighScore)
        {
            HighScore = finalScore;
            IsNewHighScore = true;
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }
        else
        {
            IsNewHighScore = false;
        }

        // Haptic feedback on game over
        #if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
        #endif

        // Initial dramatic freeze
        Time.timeScale = 0.1f;

        // Intense screen flash
        TriggerScreenFlash(Color.white, 0.8f);

        // Spawn death particles
        SpawnDeathParticles();

        // Intense camera shake
        ShakeAmount = 50f;

        // Play sound
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.GameOver);

        // Hide player immediately with dramatic effect
        if (Player.SpriteRenderer) Player.SpriteRenderer.enabled = false;
        if (Player.GlowRenderer) Player.GlowRenderer.enabled = false;
    }

    void SpawnDeathParticles()
    {
        // Clear existing particles
        foreach (var p in deathParticles)
            if (p) Destroy(p);
        deathParticles.Clear();

        // Create explosion of particles
        for (int i = 0; i < 24; i++)
        {
            var particle = new GameObject("DeathParticle");
            particle.transform.position = deathPosition;

            var sr = particle.AddComponent<SpriteRenderer>();
            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.sortingOrder = 200;

            // Create soft circle
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

            // Mix of player color and red for dramatic effect
            Color particleColor = i % 2 == 0 ? Player.entityColor : Color.red;
            particleColor.a = 1f;
            sr.color = particleColor;

            particle.transform.localScale = Vector3.one * Random.Range(0.3f, 0.8f);

            deathParticles.Add(particle);
        }
    }

    void UpdateGameOverSequence()
    {
        // Use unscaled delta time since we're manipulating timeScale
        gameOverTime += Time.unscaledDeltaTime;

        // Phase 0: Initial slow-mo explosion (0-2 seconds)
        if (gameOverTime < gameOverSlowMoDuration)
        {
            float t = gameOverTime / gameOverSlowMoDuration;

            // Gradually restore time
            Time.timeScale = Mathf.Lerp(0.1f, 0.3f, t);

            // Camera shake fades
            ShakeAmount = Mathf.Lerp(50f, 20f, t);

            // Animate death particles outward
            UpdateDeathParticles(t, false);

            // Red vignette intensifies
            if (vignetteRenderer)
                vignetteRenderer.color = new Color(1, 0, 0, Mathf.Lerp(0.3f, 0.8f, t));

            // Background desaturates
            if (BackgroundRenderer)
                BackgroundRenderer.color = Color.Lerp(BackgroundRenderer.color, new Color(0.3f, 0.2f, 0.2f), t * 0.5f);

            gameOverPhase = t;
        }
        // Phase 1: Hold and absorb (2-4 seconds)
        else if (gameOverTime < gameOverSlowMoDuration + 2f)
        {
            float t = (gameOverTime - gameOverSlowMoDuration) / 2f;

            Time.timeScale = 0.3f;
            ShakeAmount = Mathf.Lerp(20f, 5f, t);

            // Particles start pulling back toward AI
            UpdateDeathParticles(1f + t, true);

            gameOverPhase = 1f + t;
        }
        // Phase 2: Final dramatic moment (4-6 seconds)
        else if (gameOverTime < gameOverSlowMoDuration + 4f)
        {
            float t = (gameOverTime - gameOverSlowMoDuration - 2f) / 2f;

            Time.timeScale = Mathf.Lerp(0.3f, 0.8f, t);
            ShakeAmount = Mathf.Sin(t * Mathf.PI * 4f) * 3f;

            // AI pulses triumphantly
            if (AI != null)
            {
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 8f) * 0.2f;
                AI.transform.localScale = Vector3.one * (AI.size / 126f) * pulse;

                if (AI.SpriteRenderer)
                    AI.SpriteRenderer.color = Color.Lerp(Color.red, new Color(1f, 0.2f, 0f), Mathf.Sin(t * Mathf.PI * 6f) * 0.5f + 0.5f);
            }

            // Clean up remaining particles
            float particleAlpha = 1f - t;
            foreach (var p in deathParticles)
            {
                if (p)
                {
                    var sr = p.GetComponent<SpriteRenderer>();
                    if (sr) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, particleAlpha);
                }
            }

            gameOverPhase = 2f + t;
        }
        // Phase 3: Fade and wait (6+ seconds)
        else
        {
            Time.timeScale = 1f;
            ShakeAmount = 0;

            // Clean up particles
            foreach (var p in deathParticles)
                if (p) Destroy(p);
            deathParticles.Clear();

            // Restart after total duration
            if (gameOverTime >= gameOverTotalDuration)
            {
                RestartGame();
            }

            gameOverPhase = 3f;
        }

        // Camera shake effect
        if (MainCamera && ShakeAmount > 0)
        {
            float s = ShakeAmount * 0.02f;
            MainCamera.transform.position = new Vector3(
                Random.Range(-s, s),
                Random.Range(-s, s),
                MainCamera.transform.position.z
            );
        }
    }

    void UpdateDeathParticles(float progress, bool pullBack)
    {
        Vector3 aiPos = AI != null ? AI.transform.position : deathPosition;

        for (int i = 0; i < deathParticles.Count; i++)
        {
            var p = deathParticles[i];
            if (p == null) continue;

            float angle = (i / (float)deathParticles.Count) * Mathf.PI * 2f;
            angle += progress * 2f; // Rotate over time

            if (pullBack)
            {
                // Pull particles toward AI
                float pullT = progress - 1f;
                Vector3 targetPos = Vector3.Lerp(p.transform.position, aiPos, pullT * 0.5f * Time.unscaledDeltaTime * 60f);
                p.transform.position = targetPos;

                // Shrink
                p.transform.localScale *= 0.98f;
            }
            else
            {
                // Expand outward
                float radius = progress * 3f;
                float wobble = Mathf.Sin(progress * Mathf.PI * 4f + i) * 0.5f;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * (radius + wobble),
                    Mathf.Sin(angle) * (radius + wobble),
                    0
                );
                p.transform.position = deathPosition + offset;

                // Rotate and scale
                p.transform.Rotate(0, 0, 360f * Time.unscaledDeltaTime);
                float scale = (1f - progress * 0.3f) * Random.Range(0.9f, 1.1f);
                p.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale * 0.5f);
            }

            // Fade based on progress
            var sr = p.GetComponent<SpriteRenderer>();
            if (sr)
            {
                float alpha = Mathf.Clamp01(1f - (progress - 1.5f));
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }
        }
    }

    public float GameOverPhase => gameOverPhase;
    public int FinalScore => finalScore;

    void RestartGame()
    {
        Frame = WaitFrame = Score = 0;
        ComboCount = 0;
        comboTime = 0;
        CurrentScene = GameScene.Play;

        // Reset game over state
        gameOverSequenceStarted = false;
        gameOverTime = 0;
        gameOverPhase = 0;
        Time.timeScale = 1f;
        IsNewHighScore = false;

        // Reset near miss tracking
        nearMissBonusTotal = 0;
        currentNearMissBonus = 0;

        // Reset achievements for this run
        for (int i = 0; i < achievementsUnlocked.Length; i++)
            achievementsUnlocked[i] = false;

        // Clear notifications
        notificationQueue.Clear();
        currentNotification = null;

        // Clear death particles
        foreach (var p in deathParticles)
            if (p) Destroy(p);
        deathParticles.Clear();

        // Reset player visuals
        if (Player.SpriteRenderer) Player.SpriteRenderer.enabled = true;
        if (Player.GlowRenderer) Player.GlowRenderer.enabled = true;
        Player.ResetPlayer(RandomName(PlayerNames));

        AI.SetLevel(1, RandomName(AINames));
        AI.ResetAI();
        if (MainCamera) MainCamera.transform.position = new Vector3(0, 0, MainCamera.transform.position.z);

        // Reset music to level 1
        AudioManager.Instance?.PlayMusicForLevel(1);

        // Reset visual effects
        if (vignetteRenderer) vignetteRenderer.color = new Color(1, 0, 0, 0);
        if (BackgroundRenderer) BackgroundRenderer.color = Color.white;

        // Clear power-ups
        foreach (var p in activePowerUps)
            if (p) Destroy(p);
        activePowerUps.Clear();
    }

    public void RemovePowerUp(GameObject powerUp)
    {
        activePowerUps.Remove(powerUp);
    }

    string RandomName(string[] names) => names[Random.Range(0, names.Length)];

    public bool IsInComboZone => inComboZone;
    public float ComboProgress => comboTime;
}

/// <summary>Collectible power-up</summary>
public class PowerUp : MonoBehaviour
{
    public PowerUpType type;
    public float duration;

    SpriteRenderer sr;
    float bobTime;
    float lifetime = 10f;

    void Start()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = 5;

        // Create diamond shape texture
        int res = 64;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float dy = Mathf.Abs(y - center) / center;
                float dist = dx + dy;
                if (dist <= 1f)
                {
                    float alpha = 1f - dist;
                    colors[y * res + x] = new Color(1, 1, 1, alpha);
                }
                else
                    colors[y * res + x] = Color.clear;
            }

        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);

        // Color based on type
        switch (type)
        {
            case PowerUpType.SpeedBoost:
                sr.color = Color.yellow;
                break;
            case PowerUpType.Invincibility:
                sr.color = Color.cyan;
                break;
            case PowerUpType.AISlowdown:
                sr.color = Color.blue;
                break;
            case PowerUpType.TimeSlow:
                sr.color = new Color(0.5f, 0f, 1f); // Purple
                break;
            case PowerUpType.Teleport:
                sr.color = Color.white;
                break;
        }

        transform.localScale = Vector3.one * 0.5f;
    }

    void Update()
    {
        // Bob animation
        bobTime += Time.deltaTime;
        transform.position = new Vector3(transform.position.x, transform.position.y + Mathf.Sin(bobTime * 3f) * 0.01f, 0);
        transform.Rotate(0, 0, 90 * Time.deltaTime);

        // Fade out over time
        lifetime -= Time.deltaTime;
        if (lifetime <= 3f)
        {
            float flash = Mathf.Sin(lifetime * 10f) * 0.3f + 0.7f;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, flash);
        }

        if (lifetime <= 0)
        {
            GameManager.Instance?.RemovePowerUp(gameObject);
            Destroy(gameObject);
            return;
        }

        // Check collision with player
        var player = GameManager.Instance?.Player;
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.GetPosition());
            if (dist < 1f)
            {
                CollectPowerUp(player);
            }
        }
    }

    void CollectPowerUp(Player player)
    {
        Color burstColor = GetComponent<SpriteRenderer>()?.color ?? Color.white;

        switch (type)
        {
            case PowerUpType.SpeedBoost:
            case PowerUpType.Invincibility:
                player.ApplyPowerUp(type, duration);
                break;
            case PowerUpType.AISlowdown:
                GameManager.Instance?.AI?.ApplySlowdown(duration);
                break;
            case PowerUpType.TimeSlow:
                GameManager.Instance?.ApplyTimeSlow(duration);
                player.ApplyPowerUp(type, duration);
                break;
            case PowerUpType.Teleport:
                GameManager.Instance?.TeleportPlayer();
                break;
        }

        // Spawn visual effects
        GameManager.Instance?.SpawnPowerUpBurst(transform.position, burstColor);
        GameManager.Instance?.SpawnScorePopup(transform.position + Vector3.up * 0.5f, 100, burstColor);

        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.PowerUp);
        GameManager.Instance?.RemovePowerUp(gameObject);
        Destroy(gameObject);
    }
}

/// <summary>Data component for star particles</summary>
public class StarParticle : MonoBehaviour
{
    public float speed;
    public float twinkleSpeed;
    public float baseAlpha;
}

/// <summary>Data component for ambient particles</summary>
public class AmbientParticle : MonoBehaviour
{
    public float speed;
    public float waveSpeed;
    public float waveAmount;
    public float lifetime;
    public float maxLifetime;
    public float baseAlpha;
}

/// <summary>Data component for speed lines</summary>
public class SpeedLineData : MonoBehaviour
{
    public Vector3 velocity;
    public float lifetime;
    public float maxLifetime;
}

/// <summary>Data component for impact ripples</summary>
public class RippleData : MonoBehaviour
{
    public float lifetime;
    public float maxLifetime;
    public float startScale;
    public float endScale;
    public Color color;
}
