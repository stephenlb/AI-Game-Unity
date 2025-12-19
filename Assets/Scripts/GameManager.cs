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

        // Background with gradient
        CreateGradientBackground();

        // Create vignette overlay
        CreateVignetteOverlay();

        // Create screen flash overlay
        CreateScreenFlashOverlay();

        // Create boundary visualization
        CreateBoundaryVisualization();

        CurrentScene = GameScene.Play;
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

                // Visual feedback
                TriggerScreenFlash(Color.yellow, 0.1f);
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
        powerUp.type = (PowerUpType)Random.Range(0, 3);
        powerUp.duration = powerUpDuration;

        activePowerUps.Add(obj);
    }

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
                AI.transform.localScale = Vector3.one * (AI.size / 100f) * pulse;

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
        switch (type)
        {
            case PowerUpType.SpeedBoost:
            case PowerUpType.Invincibility:
                player.ApplyPowerUp(type, duration);
                break;
            case PowerUpType.AISlowdown:
                GameManager.Instance?.AI?.ApplySlowdown(duration);
                break;
        }

        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.PowerUp);
        GameManager.Instance?.RemovePowerUp(gameObject);
        Destroy(gameObject);
    }
}
