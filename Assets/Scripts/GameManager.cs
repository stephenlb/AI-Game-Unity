using UnityEngine;
using System.Collections.Generic;

public enum GameScene
{
    Intro,
    Play,
    Level,
    GameOver
}

/// <summary>
/// Main game state manager
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float GameWidth = 7.2f;  // Unity units (720 pixels / 100)
    public float GameHeight = 12.8f; // Unity units (1280 pixels / 100)

    [Header("References")]
    public Player Player;
    public AIEnemy AI;
    public UIManager UIManager;
    public GhostManager GhostManager;
    public Camera MainCamera;
    public SpriteRenderer BackgroundRenderer;
    public SpriteRenderer CoverImage;

    [Header("Player Names")]
    public string[] PlayerNames = {
        "QuantifiedQuantum", "Kalamata", "EmoAImusic", "MD", "Torva",
        "Haidar", "BoboBear", "Mohamed", "Alucard", "Kevin",
        "Barry", "Uniqueux", "JanHoleman", "TheJAM", "megansub",
        "Dereck", "Kyle", "Tuleku", "Travis", "Valor",
        "Lukey", "Mosh", "Alazr", "Ahmed"
    };

    [Header("AI Names")]
    public string[] AINames = {
        "HAL9000", "Skynet", "Predator", "DeepBlue", "AlphaGo",
        "Watson", "Siri", "nAIma", "Aldan", "mAIa",
        "nAlma", "gAIl", "bAIley", "dAIsy"
    };

    // Game state
    public GameScene CurrentScene { get; private set; } = GameScene.Play;
    public int Frame { get; private set; } = 0;
    public int WaitFrame { get; private set; } = 0;
    public float ShakeAmount { get; private set; } = 0;
    public int Score { get; private set; } = 0;
    public int Occupancy { get; set; } = 0;

    private Color originalBgColor;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        // Setup camera
        if (MainCamera == null)
            MainCamera = Camera.main;

        // Create camera if none exists
        if (MainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            MainCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = new Vector3(0, 0, -10);
        }

        // Ensure AudioListener exists
        if (MainCamera.GetComponent<AudioListener>() == null)
        {
            MainCamera.gameObject.AddComponent<AudioListener>();
        }

        // Ensure camera is at correct Z position
        MainCamera.transform.position = new Vector3(
            MainCamera.transform.position.x,
            MainCamera.transform.position.y,
            -10
        );
        MainCamera.orthographic = true;
        MainCamera.orthographicSize = GameHeight / 2f;
        MainCamera.clearFlags = CameraClearFlags.SolidColor;
        MainCamera.backgroundColor = Color.black;
        MainCamera.cullingMask = -1; // Everything

        // Create player if not assigned
        if (Player == null)
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(0, -3, 0); // Start near bottom center
            Player = playerObj.AddComponent<Player>();
            Player.entityName = GetRandomPlayerName();
            Player.entityColor = Player.GetColorFromName(Player.entityName);
            Player.textColor = new Color(0, 0.6f, 0.6f);
            Player.size = 60;
            Player.speed = 0;
            Player.UpdateVisuals();
        }

        // Create AI if not assigned
        if (AI == null)
        {
            GameObject aiObj = new GameObject("AI");
            AI = aiObj.AddComponent<AIEnemy>();
            AI.entityName = GetRandomAIName();
            AI.entityColor = Color.red;
            AI.textColor = Color.white;
            AI.size = 120;
            AI.speed = 1;
            AI.SetLevel(1, AI.entityName);
            AI.UpdateVisuals();
        }

        // Create UI Manager if not assigned
        if (UIManager == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            UIManager = uiObj.AddComponent<UIManager>();
        }

        // Create Ghost Manager if not assigned
        if (GhostManager == null)
        {
            GameObject ghostObj = new GameObject("GhostManager");
            GhostManager = ghostObj.AddComponent<GhostManager>();
        }

        // Create Audio Manager if not assigned
        if (AudioManager.Instance == null)
        {
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();
        }

        // Create background
        if (BackgroundRenderer == null)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.position = new Vector3(0, 0, 10);
            BackgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            BackgroundRenderer.material = new Material(Shader.Find("Sprites/Default")); // Unlit for URP 2D
            BackgroundRenderer.sprite = CreateRectSprite((int)(GameWidth * 100), (int)(GameHeight * 100));
            BackgroundRenderer.color = new Color(0, 1, 0.4f); // Green tint
            BackgroundRenderer.sortingOrder = -1; // Render behind entities
            bgObj.transform.localScale = new Vector3(GameWidth, GameHeight, 1);
        }

        originalBgColor = new Color(0, 1, 0.4f);
        CurrentScene = GameScene.Play;
    }

    void Update()
    {
        Frame++;

        switch (CurrentScene)
        {
            case GameScene.Play:
                UpdatePlayScene();
                break;
            case GameScene.GameOver:
                UpdateGameOverScene();
                break;
            case GameScene.Level:
                UpdateLevelScene();
                break;
        }
    }

    void UpdatePlayScene()
    {
        // Guard against null references during initialization
        if (Player == null || AI == null) return;

        Score = Frame;

        // Check collision
        float distance = GetCollisionDistance();
        float combinedRadius = (AI.size + Player.size) / 100f; // Convert to Unity units

        // Proximity warning - color change only (no shake)
        float proximityThreshold = 4f;
        if (distance <= proximityThreshold)
        {
            ShakeAmount = (proximityThreshold - distance) * 12.5f;
            UpdateBackgroundColor(false); // Disable screen shake
        }
        else
        {
            ShakeAmount = 0;
            if (BackgroundRenderer != null)
                BackgroundRenderer.color = originalBgColor;
        }

        // Collision detection with cooldown
        if (Frame > WaitFrame + 100)
        {
            if (distance <= combinedRadius)
            {
                WaitFrame = Frame;
                CurrentScene = GameScene.GameOver;
            }
        }
    }

    void UpdateGameOverScene()
    {
        ShakeAmount = 0;

        // Restart after delay
        if (Frame > WaitFrame + 1200)
        {
            RestartGame();
        }
    }

    void UpdateLevelScene()
    {
        // Continue to next level after delay
        if (Frame > WaitFrame + 500)
        {
            WaitFrame = Frame;
            CurrentScene = GameScene.Play;
        }
    }

    void UpdateBackgroundColor(bool applyShake = true)
    {
        if (BackgroundRenderer == null) return;

        float normalizedShake = Mathf.Clamp01(ShakeAmount / 50f);
        float colorIntensity = normalizedShake;

        Color shakeColor = new Color(
            colorIntensity,
            1 - colorIntensity,
            Mathf.Max(0, 0.4f - colorIntensity * 0.4f)
        );

        BackgroundRenderer.color = shakeColor;

        // Apply screen shake only if enabled
        if (applyShake && MainCamera != null && ShakeAmount > 0)
        {
            float shake = ShakeAmount * 0.01f;
            MainCamera.transform.position = new Vector3(
                Random.Range(-shake, shake),
                Random.Range(-shake, shake),
                MainCamera.transform.position.z
            );
        }
    }

    float GetCollisionDistance()
    {
        if (Player == null || AI == null) return float.MaxValue;
        return Vector2.Distance(Player.GetPosition(), AI.GetPosition());
    }

    public void NextLevel()
    {
        WaitFrame = Frame;
        CurrentScene = GameScene.Level;
        AI.SetLevel(AI.level + 1, GetRandomAIName());
    }

    void RestartGame()
    {
        Frame = 0;
        WaitFrame = Frame;
        Score = 0;
        CurrentScene = GameScene.Play;

        Player.ResetPlayer(GetRandomPlayerName());
        AI.SetLevel(1, GetRandomAIName());
        AI.ResetAI();

        // Reset camera position
        if (MainCamera != null)
            MainCamera.transform.position = new Vector3(0, 0, MainCamera.transform.position.z);
    }

    string GetRandomPlayerName()
    {
        return PlayerNames[Random.Range(0, PlayerNames.Length)];
    }

    string GetRandomAIName()
    {
        return AINames[Random.Range(0, AINames.Length)];
    }

    Sprite CreateRectSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
    }
}
