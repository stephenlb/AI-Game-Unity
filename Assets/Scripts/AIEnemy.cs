using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI Enemy that uses a neural network to learn to chase the player
/// </summary>
public class AIEnemy : Entity
{
    [Header("AI Properties")]
    public int level = 1;
    public int learning = 100; // 1 is highest learning rate, 100 is lowest rate of learning
    public int knowledge = 0;

    private NeuralNetwork neuralNetwork;
    private List<float> losses = new List<float>();
    private GameManager gameManager;
    private Player player;

    // For UI display
    public float[] Features { get; private set; } = new float[4];
    public float[] Labels { get; private set; } = new float[2];
    public float[] Output => neuralNetwork?.LastOutput ?? new float[2];
    public float AverageLoss => losses.Count > 0 ? GetAverageLoss() : 0f;

    protected override void Awake()
    {
        base.Awake();
        neuralNetwork = new NeuralNetwork();
    }

    protected override void Start()
    {
        base.Start();
        gameManager = GameManager.Instance;
        player = gameManager?.Player;
    }

    void Update()
    {
        if (gameManager == null || gameManager.CurrentScene != GameScene.Play)
            return;

        if (player == null)
        {
            player = gameManager.Player;
            if (player == null) return;
        }

        // Calculate normalized positions
        float width = gameManager.GameWidth;
        float height = gameManager.GameHeight;

        Vector2 playerPos = player.GetPosition();
        Vector2 aiPos = GetPosition();

        // Calculate direction to player (labels for training)
        float dx = playerPos.x - aiPos.x;
        float dy = playerPos.y - aiPos.y;

        Labels[0] = dx / width;
        Labels[1] = dy / height;

        // Features: normalized positions
        Features[0] = playerPos.x;
        Features[1] = playerPos.y;
        Features[2] = aiPos.x;
        Features[3] = aiPos.y;

        // Get AI movement from neural network
        float[] output = TrainAndPredict();

        // Move AI based on neural network output
        float moveX = (width / 2f) * output[0] * Time.deltaTime * speed;
        float moveY = (height / 2f) * output[1] * Time.deltaTime * speed;

        Vector3 newPos = transform.position;
        newPos.x += moveX;
        newPos.y += moveY;

        // Clamp to screen edges
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        newPos.x = Mathf.Clamp(newPos.x, -halfWidth, halfWidth);
        newPos.y = Mathf.Clamp(newPos.y, -halfHeight, halfHeight);

        transform.position = newPos;

        // Check for level up
        knowledge++;
        if (knowledge > level * 3550)
        {
            gameManager.NextLevel();
        }
    }

    private float[] TrainAndPredict()
    {
        float[] output = neuralNetwork.Forward(Features);

        // Train every N frames based on learning rate
        if (Time.frameCount % learning == 0)
        {
            neuralNetwork.Train(Features, Labels);
            losses.Add(neuralNetwork.LastLoss);

            // Keep only last 500 losses
            if (losses.Count > 500)
                losses.RemoveAt(0);
        }

        return output;
    }

    private float GetAverageLoss()
    {
        float sum = 0;
        foreach (float loss in losses)
            sum += loss;
        return sum / losses.Count;
    }

    public void SetLevel(int newLevel, string newName)
    {
        level = newLevel;
        entityName = newName;
        knowledge = 0;
        speed = 1 + (level * 0.4f);
        size = 120 + (level * 10);

        UpdateVisuals();
    }

    public void ResetAI()
    {
        neuralNetwork.Reset();
        losses.Clear();
        knowledge = 0;
    }
}
