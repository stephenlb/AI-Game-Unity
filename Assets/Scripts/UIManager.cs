using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>UI stats display with mobile-friendly touch controls</summary>
public class UIManager : MonoBehaviour
{
    GameManager gm;
    Text statsText, gameOverText, levelText;
    Text comboText, controlsText;
    Image dashCooldownBar, comboProgressBar;
    Image speedBoostIndicator, invincibilityIndicator, aiSlowIndicator, timeSlowIndicator, teleportIndicator;
    RectTransform dashCooldownFill, comboProgressFill;

    // Mobile UI elements
    GameObject blockButton;
    Image blockButtonImage;
    Image blockButtonFill;
    GameObject shieldButton;
    Image shieldButtonImage;
    Image shieldButtonFill;
    bool isMobile;

    // Game over UI elements
    Text gameOverSubtext;
    Text finalScoreText;
    Text highScoreText;
    Text newHighScoreText;
    Text aiVictoryText;
    Text restartHintText;
    CanvasGroup gameOverCanvasGroup;
    float lastGameOverPhase = -1;

    // Notification UI
    Text notificationText;
    CanvasGroup notificationCanvasGroup;

    // Near miss display
    Text nearMissText;

    void Start()
    {
        gm = GameManager.Instance;

        // Detect if running on mobile
        isMobile = Application.isMobilePlatform ||
                   SystemInfo.deviceType == DeviceType.Handheld;

        // Canvas
        var canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Add EventSystem if not present
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // Stats text (top-right) - smaller on mobile
        int statsFontSize = isMobile ? 11 : 14;
        statsText = CreateText(canvas.transform, "StatsText", statsFontSize, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperRight);
        SetRect(statsText, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-15, -15), new Vector2(300, 300));

        // Combo text (center-top)
        comboText = CreateText(canvas.transform, "ComboText", isMobile ? 28 : 36, Color.yellow, TextAnchor.UpperCenter);
        SetRect(comboText, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(400, 100));
        comboText.gameObject.SetActive(false);

        // Controls text (bottom-left) - different text for mobile
        controlsText = CreateText(canvas.transform, "ControlsText", 11, new Color(0.5f, 0.5f, 0.5f), TextAnchor.LowerLeft);
        SetRect(controlsText, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(15, 15), new Vector2(300, 100));
        if (isMobile)
            controlsText.text = "TOUCH: Move\nBLOCK: Slow AI | SHIELD: Invincible\nStay near AI for COMBO & NEAR MISS!";
        else
            controlsText.text = "MOUSE: Move | SHIFT: Shield\nSPACE/RIGHT-CLICK: Slow AI\nStay near AI for COMBO & NEAR MISS!";

        // Game over container with canvas group for fading
        var gameOverContainer = new GameObject("GameOverContainer");
        gameOverContainer.transform.SetParent(canvas.transform);
        var containerRect = gameOverContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        gameOverCanvasGroup = gameOverContainer.AddComponent<CanvasGroup>();
        gameOverContainer.SetActive(false);

        // Main game over text (center)
        gameOverText = CreateText(gameOverContainer.transform, "GameOverText", isMobile ? 48 : 64, Color.red, TextAnchor.MiddleCenter);
        gameOverText.text = "HUMANITY LOST";
        SetRect(gameOverText, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 80), new Vector2(800, 120));

        // Subtext - "CAPTURED" or "ASSIMILATED"
        gameOverSubtext = CreateText(gameOverContainer.transform, "GameOverSubtext", isMobile ? 24 : 32, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
        gameOverSubtext.text = "YOU HAVE BEEN CAPTURED";
        SetRect(gameOverSubtext, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 20), new Vector2(600, 60));

        // Final score
        finalScoreText = CreateText(gameOverContainer.transform, "FinalScore", isMobile ? 36 : 48, Color.white, TextAnchor.MiddleCenter);
        finalScoreText.text = "FINAL SCORE: 0";
        SetRect(finalScoreText, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -60), new Vector2(600, 80));

        // High score
        highScoreText = CreateText(gameOverContainer.transform, "HighScore", isMobile ? 18 : 22, new Color(0.8f, 0.8f, 0.2f), TextAnchor.MiddleCenter);
        highScoreText.text = "HIGH SCORE: 0";
        SetRect(highScoreText, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -100), new Vector2(400, 40));

        // New high score banner
        newHighScoreText = CreateText(gameOverContainer.transform, "NewHighScore", isMobile ? 24 : 32, new Color(1f, 0.8f, 0f), TextAnchor.MiddleCenter);
        newHighScoreText.text = "★ NEW HIGH SCORE! ★";
        SetRect(newHighScoreText, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -140), new Vector2(500, 50));
        newHighScoreText.gameObject.SetActive(false);

        // AI victory text
        aiVictoryText = CreateText(gameOverContainer.transform, "AIVictory", isMobile ? 18 : 22, new Color(0.8f, 0.4f, 0.4f), TextAnchor.MiddleCenter);
        aiVictoryText.text = "The AI has grown stronger...";
        SetRect(aiVictoryText, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -180), new Vector2(600, 50));

        // Restart hint
        restartHintText = CreateText(gameOverContainer.transform, "RestartHint", isMobile ? 14 : 16, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleCenter);
        restartHintText.text = "Restarting...";
        SetRect(restartHintText, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, -230), new Vector2(400, 40));

        // Level text (center)
        levelText = CreateText(canvas.transform, "LevelText", isMobile ? 150 : 200, Color.white, TextAnchor.MiddleCenter);
        SetRect(levelText, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(600, 300));
        levelText.gameObject.SetActive(false);

        // Combo progress bar (under combo text)
        CreateComboProgressBar(canvas.transform);

        // Power-up indicators (top-left)
        CreatePowerUpIndicators(canvas.transform);

        // Create mobile block button (bottom-right, large and touch-friendly)
        CreateMobileBlockButton(canvas.transform);

        // Create mobile shield button (left of block button)
        CreateMobileShieldButton(canvas.transform);

        // Create small block cooldown indicator near the button
        CreateBlockCooldownBar(canvas.transform);

        // Create notification text (center screen)
        CreateNotificationUI(canvas.transform);

        // Create near miss display
        CreateNearMissDisplay(canvas.transform);
    }

    void CreateNotificationUI(Transform parent)
    {
        var container = new GameObject("NotificationContainer");
        container.transform.SetParent(parent);
        var rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.6f);
        rect.anchorMax = new Vector2(0.5f, 0.6f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500, 60);

        notificationCanvasGroup = container.AddComponent<CanvasGroup>();
        notificationCanvasGroup.alpha = 0;

        // Background
        var bgImage = container.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        notificationText = CreateText(container.transform, "NotificationText", isMobile ? 20 : 24, Color.white, TextAnchor.MiddleCenter);
        SetRect(notificationText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    void CreateNearMissDisplay(Transform parent)
    {
        nearMissText = CreateText(parent, "NearMissText", isMobile ? 18 : 22, new Color(1f, 0.8f, 0f), TextAnchor.MiddleCenter);
        SetRect(nearMissText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(300, 50));
        nearMissText.gameObject.SetActive(false);
    }

    void CreateMobileShieldButton(Transform parent)
    {
        shieldButton = new GameObject("ShieldButton");
        shieldButton.transform.SetParent(parent);

        var rect = shieldButton.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-180, 30); // Left of block button
        rect.sizeDelta = new Vector2(120, 120);

        // Background circle
        var bgObj = new GameObject("ShieldBg");
        bgObj.transform.SetParent(shieldButton.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.3f, 0.2f, 0.8f);
        bgImage.sprite = CreateCircleSprite(64);

        // Cooldown fill
        var fillObj = new GameObject("ShieldFill");
        fillObj.transform.SetParent(shieldButton.transform);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(8, 8);
        fillRect.offsetMax = new Vector2(-8, -8);

        shieldButtonFill = fillObj.AddComponent<Image>();
        shieldButtonFill.color = new Color(0f, 1f, 0.5f, 0.9f);
        shieldButtonFill.sprite = CreateCircleSprite(64);
        shieldButtonFill.type = Image.Type.Filled;
        shieldButtonFill.fillMethod = Image.FillMethod.Radial360;
        shieldButtonFill.fillOrigin = (int)Image.Origin360.Top;
        shieldButtonFill.fillClockwise = true;

        // Button image
        shieldButtonImage = shieldButton.AddComponent<Image>();
        shieldButtonImage.color = new Color(0, 0, 0, 0);
        shieldButtonImage.sprite = CreateCircleSprite(64);

        // Add button component
        var button = shieldButton.AddComponent<Button>();
        button.targetGraphic = shieldButtonImage;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnShieldButtonPressed);

        // Label
        var labelText = CreateText(shieldButton.transform, "ShieldLabel", 12, Color.white, TextAnchor.MiddleCenter);
        SetRect(labelText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        labelText.text = "SHIELD";
    }

    void OnShieldButtonPressed()
    {
        if (gm?.Player != null)
        {
            gm.Player.RequestShield();
        }
    }

    void CreateMobileBlockButton(Transform parent)
    {
        blockButton = new GameObject("BlockButton");
        blockButton.transform.SetParent(parent);

        var rect = blockButton.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-30, 30);
        rect.sizeDelta = new Vector2(140, 140);

        // Background circle
        var bgObj = new GameObject("BlockBg");
        bgObj.transform.SetParent(blockButton.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.3f, 0.8f);
        bgImage.sprite = CreateCircleSprite(64);

        // Cooldown fill (shows when ready)
        var fillObj = new GameObject("BlockFill");
        fillObj.transform.SetParent(blockButton.transform);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(8, 8);
        fillRect.offsetMax = new Vector2(-8, -8);

        blockButtonFill = fillObj.AddComponent<Image>();
        blockButtonFill.color = new Color(0.3f, 0.5f, 1f, 0.9f);
        blockButtonFill.sprite = CreateCircleSprite(64);
        blockButtonFill.type = Image.Type.Filled;
        blockButtonFill.fillMethod = Image.FillMethod.Radial360;
        blockButtonFill.fillOrigin = (int)Image.Origin360.Top;
        blockButtonFill.fillClockwise = true;

        // Button image (interactive area)
        blockButtonImage = blockButton.AddComponent<Image>();
        blockButtonImage.color = new Color(0, 0, 0, 0); // Invisible but catches touches
        blockButtonImage.sprite = CreateCircleSprite(64);

        // Add button component
        var button = blockButton.AddComponent<Button>();
        button.targetGraphic = blockButtonImage;
        button.transition = Selectable.Transition.None;

        // Wire up the button click
        button.onClick.AddListener(OnBlockButtonPressed);

        // Label
        var labelText = CreateText(blockButton.transform, "BlockLabel", 16, Color.white, TextAnchor.MiddleCenter);
        SetRect(labelText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        labelText.text = "SLOW\nAI";

        // Add outline to label
        var outline = labelText.gameObject.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
        }
    }

    void OnBlockButtonPressed()
    {
        if (gm?.Player != null)
        {
            gm.Player.RequestBlock();
        }
    }

    Sprite CreateCircleSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var colors = new Color[resolution * resolution];
        float center = resolution / 2f;

        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = dist <= center - 1 ? 1f : 0f;
                colors[y * resolution + x] = new Color(1, 1, 1, alpha);
            }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    void CreateBlockCooldownBar(Transform parent)
    {
        var container = new GameObject("BlockCooldownContainer");
        container.transform.SetParent(parent);
        var containerRect = container.AddComponent<RectTransform>();

        // Position based on platform
        if (isMobile)
        {
            // Under the block button on mobile
            containerRect.anchorMin = new Vector2(1, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(1, 0);
            containerRect.anchoredPosition = new Vector2(-30, 180);
            containerRect.sizeDelta = new Vector2(140, 12);
        }
        else
        {
            // Bottom center on desktop
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, 50);
            containerRect.sizeDelta = new Vector2(200, 16);
        }

        // Background
        var bgObj = new GameObject("CooldownBg");
        bgObj.transform.SetParent(container.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        // Fill
        var fillObj = new GameObject("CooldownFill");
        fillObj.transform.SetParent(container.transform);
        dashCooldownFill = fillObj.AddComponent<RectTransform>();
        dashCooldownFill.anchorMin = Vector2.zero;
        dashCooldownFill.anchorMax = new Vector2(1, 1);
        dashCooldownFill.pivot = new Vector2(0, 0.5f);
        dashCooldownFill.offsetMin = new Vector2(2, 2);
        dashCooldownFill.offsetMax = new Vector2(-2, -2);
        dashCooldownBar = fillObj.AddComponent<Image>();
        dashCooldownBar.color = new Color(0.3f, 0.5f, 1f, 1f);
    }

    void CreateComboProgressBar(Transform parent)
    {
        var container = new GameObject("ComboProgressContainer");
        container.transform.SetParent(parent);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1);
        containerRect.anchorMax = new Vector2(0.5f, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.anchoredPosition = new Vector2(0, -110);
        containerRect.sizeDelta = new Vector2(280, 8);

        // Background
        var bgObj = new GameObject("ComboBg");
        bgObj.transform.SetParent(container.transform);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        // Fill
        var fillObj = new GameObject("ComboFill");
        fillObj.transform.SetParent(container.transform);
        comboProgressFill = fillObj.AddComponent<RectTransform>();
        comboProgressFill.anchorMin = Vector2.zero;
        comboProgressFill.anchorMax = new Vector2(0, 1);
        comboProgressFill.pivot = new Vector2(0, 0.5f);
        comboProgressFill.offsetMin = Vector2.zero;
        comboProgressFill.offsetMax = Vector2.zero;
        comboProgressBar = fillObj.AddComponent<Image>();
        comboProgressBar.color = Color.yellow;

        container.SetActive(false);
    }

    void CreatePowerUpIndicators(Transform parent)
    {
        var container = new GameObject("PowerUpIndicators");
        container.transform.SetParent(parent);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(15, -15);
        containerRect.sizeDelta = new Vector2(250, 45);

        int iconSize = isMobile ? 30 : 35;
        int spacing = isMobile ? 35 : 45;

        speedBoostIndicator = CreatePowerUpIcon(container.transform, "SpeedBoost", Color.yellow, 0, iconSize);
        invincibilityIndicator = CreatePowerUpIcon(container.transform, "Invincibility", Color.cyan, spacing, iconSize);
        aiSlowIndicator = CreatePowerUpIcon(container.transform, "AISlowdown", Color.blue, spacing * 2, iconSize);
        timeSlowIndicator = CreatePowerUpIcon(container.transform, "TimeSlow", new Color(0.5f, 0f, 1f), spacing * 3, iconSize);
    }

    Image CreatePowerUpIcon(Transform parent, string name, Color color, float xOffset, int size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(xOffset, 0);
        rect.sizeDelta = new Vector2(size, size);

        var img = obj.AddComponent<Image>();
        img.color = color;
        img.sprite = CreateCircleSprite(32);

        obj.SetActive(false);
        return img;
    }

    Text CreateText(Transform parent, string name, int size, Color color, TextAnchor anchor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var t = obj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;

        // Add outline for better readability
        var outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.6f);
        outline.effectDistance = new Vector2(1, -1);

        return t;
    }

    void SetRect(Text t, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var r = t.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot;
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    void Update()
    {
        if (gm == null) return;

        UpdateNotificationDisplay();

        switch (gm.CurrentScene)
        {
            case GameScene.Play:
                UpdateStats();
                UpdateComboDisplay();
                UpdateBlockCooldown();
                UpdatePowerUpIndicators();
                UpdateBlockButton();
                UpdateShieldButton();
                UpdateNearMissDisplay();
                gameOverText.transform.parent.gameObject.SetActive(false);
                levelText.gameObject.SetActive(false);
                controlsText.gameObject.SetActive(true);
                blockButton.SetActive(true);
                shieldButton.SetActive(true);
                lastGameOverPhase = -1;
                break;
            case GameScene.GameOver:
                UpdateGameOverUI();
                levelText.gameObject.SetActive(false);
                controlsText.gameObject.SetActive(false);
                blockButton.SetActive(false);
                shieldButton.SetActive(false);
                nearMissText.gameObject.SetActive(false);
                break;
            case GameScene.Level:
                statsText.gameObject.SetActive(false);
                gameOverText.transform.parent.gameObject.SetActive(false);
                comboText.gameObject.SetActive(false);
                levelText.gameObject.SetActive(true);
                levelText.text = gm.AI.level.ToString();
                controlsText.gameObject.SetActive(false);
                blockButton.SetActive(false);
                shieldButton.SetActive(false);
                nearMissText.gameObject.SetActive(false);

                // Animate level text
                float scale = 1f + Mathf.Sin(Time.time * 10f) * 0.1f;
                levelText.transform.localScale = Vector3.one * scale;
                break;
        }
    }

    void UpdateNotificationDisplay()
    {
        if (notificationCanvasGroup == null) return;

        string notification = gm.CurrentNotification;
        if (!string.IsNullOrEmpty(notification))
        {
            notificationText.text = notification;
            float fadeT = gm.NotificationTime;
            // Fade in quickly, fade out slowly
            float alpha = fadeT > 1.5f ? Mathf.Lerp(0, 1, (2f - fadeT) * 2f) : Mathf.Lerp(0, 1, fadeT / 1.5f);
            notificationCanvasGroup.alpha = alpha;

            // Slight bounce effect
            float bounce = 1f + Mathf.Sin(Time.time * 8f) * 0.03f;
            notificationText.transform.localScale = Vector3.one * bounce;
        }
        else
        {
            notificationCanvasGroup.alpha = 0;
        }
    }

    void UpdateNearMissDisplay()
    {
        if (gm.Player == null) return;

        int streak = gm.Player.NearMissStreak;
        if (streak > 0)
        {
            nearMissText.gameObject.SetActive(true);
            nearMissText.text = $"NEAR MISS x{streak}!";

            // Pulsing effect
            float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.15f;
            nearMissText.transform.localScale = Vector3.one * pulse;

            // Color intensifies with streak
            float intensity = Mathf.Min(1f, 0.5f + streak * 0.1f);
            nearMissText.color = new Color(1f, intensity, 0f);
        }
        else
        {
            nearMissText.gameObject.SetActive(false);
        }
    }

    void UpdateShieldButton()
    {
        if (gm.Player == null || shieldButtonFill == null) return;

        float cooldownPercent = gm.Player.GetShieldCooldownPercent();
        float fillAmount = 1f - cooldownPercent;

        shieldButtonFill.fillAmount = fillAmount;

        // Color and scale feedback
        if (cooldownPercent <= 0)
        {
            shieldButtonFill.color = new Color(0f, 1f, 0.5f, 0.9f);

            // Subtle pulse when ready
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
            shieldButton.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            shieldButtonFill.color = new Color(0f, 0.5f, 0.25f, 0.5f);
            shieldButton.transform.localScale = Vector3.one;
        }

        // Glow while shield is active
        if (gm.Player.IsShielded)
        {
            float flash = Mathf.Sin(Time.time * 10f) * 0.3f + 0.7f;
            shieldButtonFill.color = new Color(flash, 1f, flash, 0.95f);
            shieldButton.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 8f) * 0.1f);
        }
    }

    void UpdateStats()
    {
        if (gm.AI == null) return;
        statsText.gameObject.SetActive(true);
        var ai = gm.AI;

        string multiplierText = gm.ComboMultiplier > 1f ? $" x{gm.ComboMultiplier:F1}" : "";
        string highScoreStr = gm.HighScore > 0 ? $"Best: {gm.HighScore}" : "";

        if (isMobile)
        {
            // Simplified stats for mobile
            statsText.text = $@"Level: {ai.level}
Threat: {ai.ThreatLevel * 100:F0}%
Score: {gm.Score / 6}{multiplierText}
{highScoreStr}
Online: {gm.Occupancy}";
        }
        else
        {
            statsText.text = $@"AI Knowledge: {ai.AverageLoss:F4}
Threat Level: {ai.ThreatLevel * 100:F0}%
AI Level: {ai.level}

Score: {gm.Score / 6}{multiplierText}
{highScoreStr}
Online: {gm.Occupancy}

Features:  {ai.Features[0]:F2}, {ai.Features[1]:F2}
           {ai.Features[2]:F2}, {ai.Features[3]:F2}
Output:    {ai.Output[0]:F2}, {ai.Output[1]:F2}";
        }
    }

    void UpdateComboDisplay()
    {
        bool showCombo = gm.ComboCount > 0 || gm.IsInComboZone;
        comboText.gameObject.SetActive(showCombo);

        if (showCombo)
        {
            if (gm.ComboCount > 0)
            {
                comboText.text = $"COMBO x{gm.ComboCount}";
                float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.1f;
                comboText.transform.localScale = Vector3.one * pulse;
                comboText.color = Color.Lerp(Color.yellow, Color.red, (gm.ComboCount - 1) / 10f);
            }
            else
            {
                comboText.text = "DANGER ZONE";
                comboText.color = new Color(1f, 0.5f, 0f);
                comboText.transform.localScale = Vector3.one;
            }

            // Update combo progress bar
            if (comboProgressFill != null && comboProgressFill.transform.parent != null)
            {
                comboProgressFill.transform.parent.gameObject.SetActive(true);
                comboProgressFill.anchorMax = new Vector2(gm.ComboProgress, 1);
            }
        }
        else
        {
            if (comboProgressFill != null && comboProgressFill.transform.parent != null)
                comboProgressFill.transform.parent.gameObject.SetActive(false);
        }
    }

    void UpdateBlockCooldown()
    {
        if (gm.Player == null || dashCooldownFill == null) return;

        float cooldownPercent = gm.Player.GetBlockCooldownPercent();
        float fillAmount = 1f - cooldownPercent;

        dashCooldownFill.anchorMax = new Vector2(fillAmount, 1);

        // Color based on readiness
        if (cooldownPercent <= 0)
        {
            dashCooldownBar.color = new Color(0.3f, 0.6f, 1f, 1f);
        }
        else
        {
            dashCooldownBar.color = new Color(0.2f, 0.3f, 0.6f, 0.8f);
        }

        // Flash when ready
        if (cooldownPercent <= 0)
        {
            float flash = Mathf.Sin(Time.time * 5f) * 0.2f + 0.8f;
            dashCooldownBar.color = new Color(dashCooldownBar.color.r, dashCooldownBar.color.g, dashCooldownBar.color.b, flash);
        }
    }

    void UpdateBlockButton()
    {
        if (gm.Player == null || blockButtonFill == null) return;

        float cooldownPercent = gm.Player.GetBlockCooldownPercent();
        float fillAmount = 1f - cooldownPercent;

        blockButtonFill.fillAmount = fillAmount;

        // Color and scale feedback
        if (cooldownPercent <= 0)
        {
            blockButtonFill.color = new Color(0.3f, 0.6f, 1f, 0.9f);

            // Subtle pulse when ready
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
            blockButton.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            blockButtonFill.color = new Color(0.2f, 0.3f, 0.5f, 0.5f);
            blockButton.transform.localScale = Vector3.one;
        }
    }

    void UpdatePowerUpIndicators()
    {
        if (gm.Player == null) return;

        speedBoostIndicator.gameObject.SetActive(gm.Player.hasSpeedBoost);
        invincibilityIndicator.gameObject.SetActive(gm.Player.hasInvincibility);
        aiSlowIndicator.gameObject.SetActive(gm.AI != null && gm.AI.isSlowed);
        timeSlowIndicator.gameObject.SetActive(gm.Player.HasTimeSlow);

        // Pulse active indicators
        float pulse = Mathf.Sin(Time.time * 5f) * 0.2f + 0.8f;

        if (speedBoostIndicator.gameObject.activeSelf)
            speedBoostIndicator.transform.localScale = Vector3.one * pulse;

        if (invincibilityIndicator.gameObject.activeSelf)
            invincibilityIndicator.transform.localScale = Vector3.one * pulse;

        if (aiSlowIndicator.gameObject.activeSelf)
            aiSlowIndicator.transform.localScale = Vector3.one * pulse;

        if (timeSlowIndicator.gameObject.activeSelf)
            timeSlowIndicator.transform.localScale = Vector3.one * pulse;
    }

    void UpdateGameOverUI()
    {
        float phase = gm.GameOverPhase;
        var container = gameOverText.transform.parent.gameObject;

        // Show container
        container.SetActive(true);
        statsText.gameObject.SetActive(false);
        comboText.gameObject.SetActive(false);

        // Use unscaled time for animations since timeScale is modified
        float unscaledTime = Time.unscaledTime;

        // Phase 0-1: Initial impact - show "CAPTURED" with dramatic reveal
        if (phase < 1f)
        {
            float t = phase;

            // Fade in container
            gameOverCanvasGroup.alpha = Mathf.Lerp(0, 1, t * 2f);

            // Show subtext first with shake
            gameOverSubtext.gameObject.SetActive(true);
            gameOverText.gameObject.SetActive(false);
            finalScoreText.gameObject.SetActive(false);
            aiVictoryText.gameObject.SetActive(false);
            restartHintText.gameObject.SetActive(false);

            // Dramatic shake on subtext
            float shake = Mathf.Sin(unscaledTime * 30f) * (1f - t) * 10f;
            gameOverSubtext.transform.localPosition = new Vector3(shake, 20, 0);

            // Scale up dramatically
            float scale = Mathf.Lerp(3f, 1f, Mathf.Pow(t, 0.5f));
            gameOverSubtext.transform.localScale = Vector3.one * scale;

            // Color pulse
            float colorPulse = Mathf.Sin(unscaledTime * 15f) * 0.3f + 0.7f;
            gameOverSubtext.color = new Color(1f, colorPulse * 0.3f, colorPulse * 0.3f);
        }
        // Phase 1-2: Show main "HUMANITY LOST" text
        else if (phase < 2f)
        {
            float t = phase - 1f;

            gameOverCanvasGroup.alpha = 1f;
            gameOverSubtext.gameObject.SetActive(true);
            gameOverText.gameObject.SetActive(true);
            finalScoreText.gameObject.SetActive(false);
            aiVictoryText.gameObject.SetActive(false);
            restartHintText.gameObject.SetActive(false);

            // Subtext settles
            gameOverSubtext.transform.localPosition = new Vector3(0, 20, 0);
            gameOverSubtext.transform.localScale = Vector3.one;
            gameOverSubtext.color = new Color(1f, 0.3f, 0.3f, 0.8f);

            // Main text appears with dramatic scale
            float mainScale = Mathf.Lerp(0.5f, 1f, Mathf.Pow(t, 0.3f));
            gameOverText.transform.localScale = Vector3.one * mainScale;

            // Shake effect
            float shake = Mathf.Sin(unscaledTime * 20f) * (1f - t) * 8f;
            gameOverText.transform.localPosition = new Vector3(shake, 80, 0);

            // Pulsing red
            float redPulse = Mathf.Sin(unscaledTime * 8f) * 0.2f + 0.8f;
            gameOverText.color = new Color(redPulse, 0, 0);
        }
        // Phase 2-3: Show score and AI message
        else if (phase < 3f)
        {
            float t = phase - 2f;

            gameOverSubtext.gameObject.SetActive(true);
            gameOverText.gameObject.SetActive(true);
            finalScoreText.gameObject.SetActive(true);
            highScoreText.gameObject.SetActive(true);
            newHighScoreText.gameObject.SetActive(gm.IsNewHighScore && t > 0.3f);
            aiVictoryText.gameObject.SetActive(t > 0.5f);
            restartHintText.gameObject.SetActive(false);

            // Main text still pulses but calmer
            float pulse = Mathf.Sin(unscaledTime * 4f) * 0.05f + 1f;
            gameOverText.transform.localScale = Vector3.one * pulse;
            gameOverText.transform.localPosition = new Vector3(0, 80, 0);
            gameOverText.color = Color.red;

            // Score appears and counts up
            int displayScore = (int)Mathf.Lerp(0, gm.FinalScore, Mathf.Min(1f, t * 2f));
            finalScoreText.text = $"FINAL SCORE: {displayScore}";
            float scoreScale = Mathf.Lerp(1.5f, 1f, t);
            finalScoreText.transform.localScale = Vector3.one * scoreScale;

            // High score
            highScoreText.text = $"HIGH SCORE: {gm.HighScore}";

            // New high score banner animation
            if (gm.IsNewHighScore && t > 0.3f)
            {
                float hsT = (t - 0.3f) / 0.7f;
                float hsPulse = 1f + Mathf.Sin(unscaledTime * 6f) * 0.1f;
                newHighScoreText.transform.localScale = Vector3.one * hsPulse;
                newHighScoreText.color = Color.Lerp(new Color(1f, 0.8f, 0f, 0f), new Color(1f, 0.8f, 0f, 1f), hsT);
            }

            // AI victory message fades in
            if (t > 0.5f)
            {
                float aiT = (t - 0.5f) * 2f;
                aiVictoryText.color = new Color(0.8f, 0.4f, 0.4f, aiT);

                // Dynamic AI message based on level
                if (gm.AI != null)
                {
                    string[] messages = {
                        "The AI has learned from your movements...",
                        "Your patterns have been analyzed...",
                        "Resistance was futile...",
                        "The machine grows stronger...",
                        "Your data has been assimilated..."
                    };
                    aiVictoryText.text = messages[gm.AI.level % messages.Length];
                }
            }
        }
        // Phase 3+: Final state with restart hint
        else
        {
            gameOverSubtext.gameObject.SetActive(true);
            gameOverText.gameObject.SetActive(true);
            finalScoreText.gameObject.SetActive(true);
            highScoreText.gameObject.SetActive(true);
            newHighScoreText.gameObject.SetActive(gm.IsNewHighScore);
            aiVictoryText.gameObject.SetActive(true);
            restartHintText.gameObject.SetActive(true);

            // Gentle pulse on main text
            float pulse = Mathf.Sin(unscaledTime * 2f) * 0.03f + 1f;
            gameOverText.transform.localScale = Vector3.one * pulse;
            gameOverText.color = Color.red;

            // Score is final
            finalScoreText.text = $"FINAL SCORE: {gm.FinalScore}";
            finalScoreText.transform.localScale = Vector3.one;

            // High score
            highScoreText.text = $"HIGH SCORE: {gm.HighScore}";

            // New high score continues to pulse
            if (gm.IsNewHighScore)
            {
                float hsPulse = 1f + Mathf.Sin(unscaledTime * 4f) * 0.08f;
                newHighScoreText.transform.localScale = Vector3.one * hsPulse;
                newHighScoreText.color = new Color(1f, 0.8f, 0f, 1f);
            }

            // AI text fully visible
            aiVictoryText.color = new Color(0.8f, 0.4f, 0.4f, 1f);

            // Restart hint blinks
            float blink = Mathf.Sin(unscaledTime * 3f) * 0.3f + 0.7f;
            restartHintText.color = new Color(0.5f, 0.5f, 0.5f, blink);
        }

        lastGameOverPhase = phase;
    }
}
