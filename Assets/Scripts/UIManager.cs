using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all UI elements including stats display and game over screen
/// </summary>
public class UIManager : MonoBehaviour
{
    private GameManager gameManager;
    private Canvas canvas;
    private Text statsText;
    private Text gameOverText;
    private Text levelText;

    void Start()
    {
        gameManager = GameManager.Instance;
        SetupUI();
    }

    void SetupUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create Stats Text
        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(canvas.transform);
        statsText = statsObj.AddComponent<Text>();
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.fontSize = 14;
        statsText.color = new Color(0.5f, 0.5f, 0.5f);
        statsText.alignment = TextAnchor.UpperRight;

        RectTransform statsRect = statsText.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(1, 1);
        statsRect.anchorMax = new Vector2(1, 1);
        statsRect.pivot = new Vector2(1, 1);
        statsRect.anchoredPosition = new Vector2(-10, -10);
        statsRect.sizeDelta = new Vector2(300, 300);

        // Create Game Over Text
        GameObject gameOverObj = new GameObject("GameOverText");
        gameOverObj.transform.SetParent(canvas.transform);
        gameOverText = gameOverObj.AddComponent<Text>();
        gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameOverText.fontSize = 50;
        gameOverText.color = Color.red;
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.text = "Humanity Lost";

        RectTransform gameOverRect = gameOverText.GetComponent<RectTransform>();
        gameOverRect.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverRect.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverRect.pivot = new Vector2(0.5f, 0.5f);
        gameOverRect.anchoredPosition = Vector2.zero;
        gameOverRect.sizeDelta = new Vector2(600, 100);
        gameOverText.gameObject.SetActive(false);

        // Create Level Text
        GameObject levelObj = new GameObject("LevelText");
        levelObj.transform.SetParent(canvas.transform);
        levelText = levelObj.AddComponent<Text>();
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 200;
        levelText.color = Color.white;
        levelText.alignment = TextAnchor.MiddleCenter;

        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.5f, 0.5f);
        levelRect.anchorMax = new Vector2(0.5f, 0.5f);
        levelRect.pivot = new Vector2(0.5f, 0.5f);
        levelRect.anchoredPosition = Vector2.zero;
        levelRect.sizeDelta = new Vector2(600, 300);
        levelText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameManager == null) return;

        switch (gameManager.CurrentScene)
        {
            case GameScene.Play:
                UpdateStatsDisplay();
                gameOverText.gameObject.SetActive(false);
                levelText.gameObject.SetActive(false);
                break;

            case GameScene.GameOver:
                UpdateStatsDisplay();
                gameOverText.gameObject.SetActive(true);
                levelText.gameObject.SetActive(false);
                break;

            case GameScene.Level:
                statsText.gameObject.SetActive(false);
                gameOverText.gameObject.SetActive(false);
                levelText.gameObject.SetActive(true);
                levelText.text = gameManager.AI.level.ToString();
                break;
        }
    }

    void UpdateStatsDisplay()
    {
        if (gameManager.AI == null) return;

        statsText.gameObject.SetActive(true);

        AIEnemy ai = gameManager.AI;
        float[] features = ai.Features;
        float[] labels = ai.Labels;
        float[] output = ai.Output;

        string stats = $@"AI Cost (knowledge):  {ai.AverageLoss:F4}
Features (input data): {features[0]:F4}
                       {features[1]:F4}
                       {features[2]:F4}
                       {features[3]:F4}
Labels (training):     {labels[0]:F3}
                       {labels[1]:F3}
Output (AI movement):  {output[0]:F3}
                       {output[1]:F3}
Score (Player):        {gameManager.Score / 6}
Occupancy (Online):    {gameManager.Occupancy}";

        statsText.text = stats;
    }
}
