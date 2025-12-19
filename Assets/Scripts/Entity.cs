using UnityEngine;

/// <summary>Base class for game entities with enhanced visual effects</summary>
public class Entity : MonoBehaviour
{
    public string entityName;
    public float speed, size;
    public Color entityColor = Color.white;
    public Color textColor = Color.white;

    [Header("Visual Effects")]
    public bool enablePulsing = true;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    public bool enableGlow = true;
    public float glowIntensity = 0.5f;

    protected SpriteRenderer spriteRenderer;
    protected SpriteRenderer glowRenderer;
    protected TextMesh nameLabel;
    protected float baseSize;
    protected float pulseTime;

    // Public accessors for external use
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public SpriteRenderer GlowRenderer => glowRenderer;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
        spriteRenderer.sortingOrder = 1;
        CreateCircleSprite();
        CreateGlowEffect();
        CreateNameLabel();
    }

    protected virtual void Start() => UpdateVisuals();

    protected virtual void Update()
    {
        if (enablePulsing)
            AnimatePulse();
    }

    void AnimatePulse()
    {
        pulseTime += Time.deltaTime * pulseSpeed;
        float pulse = 1f + Mathf.Sin(pulseTime) * pulseAmount;
        float scale = (baseSize > 0 ? baseSize : size) / 50f * pulse;
        transform.localScale = new Vector3(scale, scale, 1);

        // Update glow pulse (inverse of main pulse for breathing effect)
        if (glowRenderer != null && enableGlow)
        {
            float glowPulse = 1.2f + Mathf.Sin(pulseTime + Mathf.PI) * 0.2f;
            glowRenderer.transform.localScale = Vector3.one * glowPulse;
            Color glowColor = entityColor;
            glowColor.a = glowIntensity * (0.5f + Mathf.Sin(pulseTime) * 0.3f);
            glowRenderer.color = glowColor;
        }

        // Keep name label at consistent size
        if (nameLabel)
        {
            float inv = 1f / transform.localScale.x;
            nameLabel.transform.localScale = new Vector3(inv, inv, 1);
        }
    }

    void CreateCircleSprite()
    {
        int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f, radius = center - 1;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    // Add subtle edge gradient for depth
                    float edgeFade = 1f - Mathf.Pow(dist / radius, 3);
                    colors[y * res + x] = new Color(1, 1, 1, edgeFade * 0.3f + 0.7f);
                }
                else
                    colors[y * res + x] = Color.clear;
            }

        tex.SetPixels(colors);
        tex.Apply();
        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
    }

    void CreateGlowEffect()
    {
        var glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localScale = Vector3.one * 1.5f;

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.material = new Material(Shader.Find("Sprites/Default"));
        glowRenderer.sortingOrder = 0;

        // Create soft glow texture
        int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var colors = new Color[res * res];
        float center = res / 2f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - dist / center);
                alpha = alpha * alpha * alpha; // Cubic falloff for soft glow
                colors[y * res + x] = new Color(1, 1, 1, alpha * 0.6f);
            }

        tex.SetPixels(colors);
        tex.Apply();
        glowRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
    }

    void CreateNameLabel()
    {
        var obj = new GameObject("NameLabel");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0, 0, -0.1f);

        nameLabel = obj.AddComponent<TextMesh>();
        nameLabel.text = entityName;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.fontSize = 24;
        nameLabel.characterSize = 0.1f;

        var mr = obj.GetComponent<MeshRenderer>();
        if (mr) mr.sortingOrder = 2;
    }

    public virtual void UpdateVisuals()
    {
        baseSize = size;
        if (spriteRenderer)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = entityColor;
            float scale = size / 50f;
            transform.localScale = new Vector3(scale, scale, 1);
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        }

        if (glowRenderer)
        {
            Color glowColor = entityColor;
            glowColor.a = glowIntensity;
            glowRenderer.color = glowColor;
            glowRenderer.enabled = enableGlow;
        }

        if (nameLabel)
        {
            nameLabel.text = entityName;
            nameLabel.color = textColor;
            float inv = 1f / transform.localScale.x;
            nameLabel.transform.localScale = new Vector3(inv, inv, 1);
        }
    }

    public void SetGlowColor(Color color)
    {
        if (glowRenderer)
        {
            color.a = glowIntensity;
            glowRenderer.color = color;
        }
    }

    public void SetPulseIntensity(float intensity)
    {
        pulseAmount = intensity;
    }

    public void SetPosition(Vector2 pos) => transform.position = new Vector3(pos.x, pos.y, 0);
    public Vector2 GetPosition() => new Vector2(transform.position.x, transform.position.y);
}
