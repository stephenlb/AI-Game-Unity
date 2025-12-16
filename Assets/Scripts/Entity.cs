using UnityEngine;

/// <summary>
/// Base class for all game entities (Player, AI, etc.)
/// </summary>
public class Entity : MonoBehaviour
{
    [Header("Entity Properties")]
    public string entityName;
    public float speed;
    public float size;
    public Color entityColor = Color.white;
    public Color textColor = Color.white;

    protected SpriteRenderer spriteRenderer;
    protected TextMesh nameLabel;

    protected virtual void Awake()
    {
        // Create visual representation
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Use unlit sprite material for URP 2D compatibility
        spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Ensure entities render in front of background
        spriteRenderer.sortingOrder = 1;

        // Create circle sprite
        CreateCircleSprite();

        // Create name label
        CreateNameLabel();
    }

    protected virtual void Start()
    {
        UpdateVisuals();
    }

    private void CreateCircleSprite()
    {
        // Create a circle texture with proper format
        int resolution = 128;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color[] colors = new Color[resolution * resolution];

        float center = resolution / 2f;
        float radius = resolution / 2f - 1;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                colors[y * resolution + x] = dist <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100);
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    private void CreateNameLabel()
    {
        GameObject labelObj = new GameObject("NameLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0, 0, -0.1f); // Slightly in front

        nameLabel = labelObj.AddComponent<TextMesh>();
        nameLabel.text = entityName;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.fontSize = 24;
        nameLabel.characterSize = 0.1f;

        // Ensure text renders in front
        MeshRenderer meshRenderer = labelObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 2;
        }
    }

    public virtual void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = entityColor;
            // Size is diameter, so we scale accordingly
            float scale = size / 50f; // Normalize to reasonable Unity scale
            transform.localScale = new Vector3(scale, scale, 1);
            // Ensure z position is correct for rendering
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        }

        if (nameLabel != null)
        {
            nameLabel.text = entityName;
            nameLabel.color = textColor;
            // Keep label at constant size relative to entity
            float inverseScale = 1f / transform.localScale.x;
            nameLabel.transform.localScale = new Vector3(inverseScale, inverseScale, 1);
        }
    }

    public void SetPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, 0);
    }

    public Vector2 GetPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }
}
