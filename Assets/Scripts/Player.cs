using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player entity controlled by mouse position
/// </summary>
public class Player : Entity
{
    [Header("Player Properties")]
    public float score;
    public string userId;

    private Camera mainCamera;
    private GameManager gameManager;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        userId = System.Guid.NewGuid().ToString();
    }

    protected override void Start()
    {
        base.Start();
        gameManager = GameManager.Instance;
    }

    void Update()
    {
        if (gameManager == null || gameManager.CurrentScene != GameScene.Play)
            return;

        // Get camera reference if not set
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Follow mouse position (using new Input System)
        if (Mouse.current == null) return;
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;

        // Apply shake if needed
        if (gameManager.ShakeAmount > 0)
        {
            float shake = gameManager.ShakeAmount;
            mousePos.x += Random.Range(-shake, shake) * 0.01f;
            mousePos.y += Random.Range(-shake, shake) * 0.01f;
        }

        transform.position = mousePos;
    }

    public static Color GetColorFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Color.cyan;

        int colorValue = 0;
        foreach (char c in name)
        {
            colorValue += (int)c;
        }

        float r = ((colorValue >> 3) % 256) / 255f;
        float g = ((colorValue >> 4) % 256) / 255f;
        float b = ((colorValue >> 5) % 256) / 255f;

        return new Color(r, g, b);
    }

    public void ResetPlayer(string newName)
    {
        entityName = newName;
        entityColor = GetColorFromName(newName);
        score = 1;
        UpdateVisuals();
    }
}
