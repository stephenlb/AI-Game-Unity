using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>Player controlled by mouse/touch with enhanced visuals and abilities</summary>
public class Player : Entity
{
    public float score;
    public string userId;

    [Header("Particle Trail")]
    public int trailLength = 15;
    public float trailSpawnRate = 0.03f;

    [Header("AI Block Ability")]
    public float blockDuration = 3f;
    public float blockCooldown = 8f;
    public float blockSlowAmount = 0.3f;

    [Header("Shield Ability")]
    public float shieldDuration = 1.5f;
    public float shieldCooldown = 5f;

    [Header("Power-ups")]
    public bool hasSpeedBoost;
    public bool hasInvincibility;
    public bool hasTimeSlow;
    public float speedBoostMultiplier = 1.5f;

    [Header("Near Miss")]
    public float nearMissDistance = 1.5f;
    public int nearMissBonus = 50;

    Camera cam;
    GameManager gm;
    List<GameObject> trailParticles = new List<GameObject>();
    float lastTrailSpawn;
    Vector3 lastPosition;

    // Block ability state
    float blockCooldownRemaining;
    bool blockRequested;

    // Shield ability state
    float shieldCooldownRemaining;
    bool isShielded;
    float shieldTimeRemaining;

    // Near miss state
    bool wasInNearMissZone;
    float nearMissTime;
    int nearMissStreak;

    // Visual effects
    float invincibilityFlashTime;

    protected override void Awake()
    {
        base.Awake();
        cam = Camera.main;
        userId = System.Guid.NewGuid().ToString();
        CreateTrailParticles();
    }

    void OnEnable()
    {
        // Enable enhanced touch support
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        // Disable enhanced touch support when not needed
        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();
    }

    protected override void Start()
    {
        base.Start();
        gm = GameManager.Instance;
    }

    protected override void Update()
    {
        base.Update();

        if (gm == null || gm.CurrentScene != GameScene.Play) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Block cooldown
        if (blockCooldownRemaining > 0)
            blockCooldownRemaining -= Time.deltaTime;

        // Shield cooldown
        if (shieldCooldownRemaining > 0)
            shieldCooldownRemaining -= Time.deltaTime;

        // Shield duration
        if (isShielded)
        {
            shieldTimeRemaining -= Time.deltaTime;
            if (shieldTimeRemaining <= 0)
            {
                isShielded = false;
                SetGlowColor(entityColor);
                spriteRenderer.color = entityColor;
            }
        }

        // Handle block input (keyboard, mouse right-click, or UI button)
        bool shouldBlock = blockRequested ||
                          (Keyboard.current?.spaceKey.wasPressedThisFrame == true) ||
                          (Mouse.current?.rightButton.wasPressedThisFrame == true);

        if (shouldBlock && blockCooldownRemaining <= 0)
        {
            ActivateBlock();
            blockRequested = false;
        }

        // Handle shield input (Shift key)
        bool shouldShield = (Keyboard.current?.leftShiftKey.wasPressedThisFrame == true) ||
                           (Keyboard.current?.rightShiftKey.wasPressedThisFrame == true);

        if (shouldShield && shieldCooldownRemaining <= 0 && !isShielded)
        {
            ActivateShield();
        }

        // Get input position (touch or mouse)
        Vector3 inputPosition;
        bool hasInput = GetInputPosition(out inputPosition);

        Vector3 targetPos;

        if (hasInput)
        {
            // Follow input position
            targetPos = inputPosition;
            targetPos.z = 0;

            // Apply speed boost
            if (hasSpeedBoost)
            {
                Vector3 direction = (targetPos - transform.position).normalized;
                float distance = Vector3.Distance(targetPos, transform.position);
                targetPos = transform.position + direction * Mathf.Min(distance * speedBoostMultiplier, distance);
            }
        }
        else
        {
            targetPos = transform.position;
        }

        // Apply shake effect
        if (gm.ShakeAmount > 0 && !hasInvincibility && !isShielded)
        {
            float s = gm.ShakeAmount * 0.01f;
            targetPos.x += Random.Range(-s, s);
            targetPos.y += Random.Range(-s, s);
        }

        // Clamp to game bounds
        targetPos.x = Mathf.Clamp(targetPos.x, -gm.GameWidth / 2f + 0.5f, gm.GameWidth / 2f - 0.5f);
        targetPos.y = Mathf.Clamp(targetPos.y, -gm.GameHeight / 2f + 0.5f, gm.GameHeight / 2f - 0.5f);

        transform.position = targetPos;

        // Check for near miss
        UpdateNearMiss();

        // Update trail
        UpdateTrail();

        // Invincibility visual effect
        if (hasInvincibility || isShielded)
        {
            invincibilityFlashTime += Time.deltaTime * 10f;
            float flash = Mathf.Sin(invincibilityFlashTime) * 0.5f + 0.5f;
            Color flashColor = isShielded ? new Color(0f, 1f, 0.5f) : Color.cyan;
            SetGlowColor(Color.Lerp(flashColor, Color.white, flash));
            spriteRenderer.color = Color.Lerp(entityColor, Color.white, flash * 0.5f);

            // Shield particles
            if (isShielded && Time.frameCount % 5 == 0)
                SpawnShieldParticle();
        }

        lastPosition = transform.position;
    }

    bool GetInputPosition(out Vector3 position)
    {
        position = Vector3.zero;

        // Check for touch input first (mobile) - with safety check
        if (EnhancedTouchSupport.enabled && Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            position = cam.ScreenToWorldPoint(new Vector3(touch.screenPosition.x, touch.screenPosition.y, 0));
            position.z = 0;
            return true;
        }

        // Fall back to mouse input (desktop)
        if (Mouse.current != null)
        {
            position = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            position.z = 0;
            return true;
        }

        return false;
    }

    void ActivateBlock()
    {
        if (gm?.AI == null) return;

        blockCooldownRemaining = blockCooldown;

        // Slow down the AI
        gm.AI.ApplySlowdown(blockDuration, blockSlowAmount);

        // Visual feedback - pulse effect on player
        SpawnBlockParticles();

        // Audio feedback
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.PowerUp);
    }

    void ActivateShield()
    {
        isShielded = true;
        shieldTimeRemaining = shieldDuration;
        shieldCooldownRemaining = shieldCooldown;

        // Audio feedback
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.PowerUp);

        // Visual feedback - shield activation burst
        SpawnShieldBurstParticles();
    }

    void SpawnShieldBurstParticles()
    {
        for (int i = 0; i < 16; i++)
        {
            var particle = CreateParticle(transform.position, new Color(0f, 1f, 0.5f), 0.9f);
            float angle = i * 22.5f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            StartCoroutine(AnimateParticle(particle, dir * 2.5f, 0.4f));
        }
    }

    void SpawnShieldParticle()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = 0.8f;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        var particle = CreateParticle(transform.position + offset, new Color(0f, 1f, 0.5f, 0.7f), 0.5f);
        particle.transform.localScale = Vector3.one * 0.25f;
        StartCoroutine(AnimateShieldParticle(particle, angle));
    }

    System.Collections.IEnumerator AnimateShieldParticle(GameObject particle, float startAngle)
    {
        float elapsed = 0;
        float duration = 0.5f;
        var sr = particle.GetComponent<SpriteRenderer>();

        while (elapsed < duration && particle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = startAngle + t * Mathf.PI;
            float radius = 0.8f * (1f - t * 0.5f);
            particle.transform.position = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            particle.transform.localScale = Vector3.one * 0.25f * (1f - t);
            sr.color = new Color(0f, 1f, 0.5f, (1f - t) * 0.7f);
            yield return null;
        }

        if (particle != null) Destroy(particle);
    }

    void UpdateNearMiss()
    {
        if (gm?.AI == null) return;

        float dist = Vector2.Distance(GetPosition(), gm.AI.GetPosition());
        float collisionDist = (gm.AI.size + size) / 100f;
        bool inNearMissZone = dist > collisionDist && dist < nearMissDistance;

        if (inNearMissZone)
        {
            nearMissTime += Time.deltaTime;

            // Award bonus for each 0.5 seconds in near miss zone
            if (nearMissTime >= 0.5f)
            {
                nearMissTime = 0f;
                nearMissStreak++;
                int bonus = nearMissBonus * nearMissStreak;
                gm.AddNearMissBonus(bonus);

                // Visual and audio feedback
                AudioManager.Instance?.PlaySFX(AudioManager.SFXType.NearMiss);
                SpawnNearMissParticles();

                // Haptic feedback on mobile
                #if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
                #endif
            }
        }
        else if (wasInNearMissZone && !inNearMissZone && dist > nearMissDistance)
        {
            // Successfully escaped near miss zone
            if (nearMissStreak > 0)
            {
                gm.OnNearMissEscape(nearMissStreak);
            }
            nearMissStreak = 0;
            nearMissTime = 0f;
        }

        wasInNearMissZone = inNearMissZone;
    }

    void SpawnNearMissParticles()
    {
        for (int i = 0; i < 6; i++)
        {
            var particle = CreateParticle(transform.position, Color.yellow, 0.7f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            StartCoroutine(AnimateParticle(particle, dir * 2f, 0.3f));
        }
    }

    // Called by UI button for shield on mobile
    public void RequestShield()
    {
        if (shieldCooldownRemaining <= 0 && !isShielded)
        {
            ActivateShield();
        }
    }

    // Called by UI button
    public void RequestBlock()
    {
        if (blockCooldownRemaining <= 0)
        {
            blockRequested = true;
        }
    }

    void SpawnBlockParticles()
    {
        for (int i = 0; i < 12; i++)
        {
            var particle = CreateParticle(transform.position, Color.blue, 0.5f);
            float angle = i * 30f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            StartCoroutine(AnimateParticle(particle, dir * 4f, 0.4f));
        }
    }

    System.Collections.IEnumerator AnimateParticle(GameObject particle, Vector3 velocity, float duration)
    {
        float elapsed = 0;
        Vector3 startPos = particle.transform.position;
        var sr = particle.GetComponent<SpriteRenderer>();
        Color startColor = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            particle.transform.position = startPos + velocity * t;
            particle.transform.localScale = Vector3.one * (1f - t) * 0.5f;
            sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
            yield return null;
        }

        Destroy(particle);
    }

    void CreateTrailParticles()
    {
        for (int i = 0; i < trailLength; i++)
        {
            var particle = CreateParticle(transform.position, entityColor, 0f);
            particle.SetActive(false);
            trailParticles.Add(particle);
        }
    }

    GameObject CreateParticle(Vector3 position, Color color, float alpha)
    {
        var obj = new GameObject("TrailParticle");
        obj.transform.position = position;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = -1;

        // Create soft circle texture
        int res = 64;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = Mathf.Clamp01(1f - dist / center);
                a = a * a;
                colors[y * res + x] = new Color(1, 1, 1, a);
            }

        tex.SetPixels(colors);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
        color.a = alpha;
        sr.color = color;

        return obj;
    }

    void UpdateTrail()
    {
        if (Time.time - lastTrailSpawn < trailSpawnRate) return;
        lastTrailSpawn = Time.time;

        // Shift particles
        for (int i = trailParticles.Count - 1; i > 0; i--)
        {
            if (trailParticles[i - 1].activeSelf)
            {
                trailParticles[i].transform.position = trailParticles[i - 1].transform.position;
                trailParticles[i].SetActive(true);
            }

            // Fade based on index
            var sr = trailParticles[i].GetComponent<SpriteRenderer>();
            float alpha = 1f - (float)i / trailLength;
            Color c = entityColor;
            c.a = alpha * 0.6f;
            sr.color = c;
            trailParticles[i].transform.localScale = Vector3.one * (0.8f - (float)i / trailLength * 0.5f);
        }

        // Update first particle
        trailParticles[0].transform.position = transform.position;
        trailParticles[0].SetActive(true);
        var sr0 = trailParticles[0].GetComponent<SpriteRenderer>();
        Color c0 = entityColor;
        c0.a = 0.6f;
        sr0.color = c0;
        trailParticles[0].transform.localScale = Vector3.one * 0.8f;
    }

    public static Color GetColorFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return Color.cyan;
        int v = 0;
        foreach (char c in name) v += c;
        return new Color(((v >> 3) % 256) / 255f, ((v >> 4) % 256) / 255f, ((v >> 5) % 256) / 255f);
    }

    public void ResetPlayer(string name)
    {
        entityName = name;
        entityColor = GetColorFromName(name);
        score = 1;
        hasSpeedBoost = false;
        hasInvincibility = false;
        hasTimeSlow = false;
        blockCooldownRemaining = 0;
        blockRequested = false;
        shieldCooldownRemaining = 0;
        isShielded = false;
        nearMissStreak = 0;
        nearMissTime = 0f;
        wasInNearMissZone = false;
        UpdateVisuals();
    }

    public void ApplyPowerUp(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.SpeedBoost:
                hasSpeedBoost = true;
                SetGlowColor(Color.yellow);
                break;
            case PowerUpType.Invincibility:
                hasInvincibility = true;
                SetGlowColor(Color.cyan);
                break;
            case PowerUpType.TimeSlow:
                hasTimeSlow = true;
                SetGlowColor(new Color(0.5f, 0f, 1f)); // Purple
                break;
        }

        StartCoroutine(RemovePowerUpAfterDelay(type, duration));
    }

    System.Collections.IEnumerator RemovePowerUpAfterDelay(PowerUpType type, float delay)
    {
        yield return new WaitForSeconds(delay);

        switch (type)
        {
            case PowerUpType.SpeedBoost:
                hasSpeedBoost = false;
                break;
            case PowerUpType.Invincibility:
                hasInvincibility = false;
                break;
            case PowerUpType.TimeSlow:
                hasTimeSlow = false;
                break;
        }

        SetGlowColor(entityColor);
        spriteRenderer.color = entityColor;
    }

    public float GetBlockCooldownPercent()
    {
        return blockCooldownRemaining / blockCooldown;
    }

    public float GetShieldCooldownPercent()
    {
        return shieldCooldownRemaining / shieldCooldown;
    }

    public bool HasInvincibility => hasInvincibility || isShielded;
    public bool IsShielded => isShielded;
    public bool HasTimeSlow => hasTimeSlow;
    public int NearMissStreak => nearMissStreak;

    void OnDestroy()
    {
        foreach (var p in trailParticles)
            if (p) Destroy(p);
        trailParticles.Clear();
    }
}

public enum PowerUpType
{
    SpeedBoost,
    Invincibility,
    AISlowdown,
    TimeSlow,
    Teleport
}
