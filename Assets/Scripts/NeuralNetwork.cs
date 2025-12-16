using UnityEngine;
using System;

/// <summary>
/// Simple neural network implementation matching the PyTorch model:
/// Linear(4, 16) -> ReLU -> Linear(16, 16) -> ReLU -> Linear(16, 2) -> Tanh
/// </summary>
[Serializable]
public class NeuralNetwork
{
    // Layer weights and biases
    private float[,] weights1; // 4 -> 16
    private float[] biases1;
    private float[,] weights2; // 16 -> 16
    private float[] biases2;
    private float[,] weights3; // 16 -> 2
    private float[] biases3;

    // Learning rate
    private float learningRate = 0.0005f;

    // For storing gradients
    private float[] layer1Output;
    private float[] layer1Activated;
    private float[] layer2Output;
    private float[] layer2Activated;
    private float[] outputLayer;

    public float[] LastOutput { get; private set; }
    public float LastLoss { get; private set; }

    public NeuralNetwork()
    {
        InitializeWeights();
        layer1Output = new float[16];
        layer1Activated = new float[16];
        layer2Output = new float[16];
        layer2Activated = new float[16];
        outputLayer = new float[2];
        LastOutput = new float[2];
    }

    private void InitializeWeights()
    {
        // Xavier initialization
        weights1 = new float[4, 16];
        biases1 = new float[16];
        weights2 = new float[16, 16];
        biases2 = new float[16];
        weights3 = new float[16, 2];
        biases3 = new float[2];

        float scale1 = Mathf.Sqrt(2f / 4f);
        float scale2 = Mathf.Sqrt(2f / 16f);

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 16; j++)
                weights1[i, j] = UnityEngine.Random.Range(-1f, 1f) * scale1;

        for (int i = 0; i < 16; i++)
        {
            biases1[i] = 0;
            biases2[i] = 0;
            for (int j = 0; j < 16; j++)
                weights2[i, j] = UnityEngine.Random.Range(-1f, 1f) * scale2;
            for (int j = 0; j < 2; j++)
                weights3[i, j] = UnityEngine.Random.Range(-1f, 1f) * scale2;
        }

        biases3[0] = 0;
        biases3[1] = 0;
    }

    public float[] Forward(float[] input)
    {
        // Layer 1: Linear(4, 16) + ReLU
        for (int j = 0; j < 16; j++)
        {
            layer1Output[j] = biases1[j];
            for (int i = 0; i < 4; i++)
                layer1Output[j] += input[i] * weights1[i, j];
            layer1Activated[j] = ReLU(layer1Output[j]);
        }

        // Layer 2: Linear(16, 16) + ReLU
        for (int j = 0; j < 16; j++)
        {
            layer2Output[j] = biases2[j];
            for (int i = 0; i < 16; i++)
                layer2Output[j] += layer1Activated[i] * weights2[i, j];
            layer2Activated[j] = ReLU(layer2Output[j]);
        }

        // Layer 3: Linear(16, 2) + Tanh
        for (int j = 0; j < 2; j++)
        {
            outputLayer[j] = biases3[j];
            for (int i = 0; i < 16; i++)
                outputLayer[j] += layer2Activated[i] * weights3[i, j];
            outputLayer[j] = Tanh(outputLayer[j]);
        }

        LastOutput[0] = outputLayer[0];
        LastOutput[1] = outputLayer[1];
        return outputLayer;
    }

    public void Train(float[] input, float[] target)
    {
        // Forward pass
        Forward(input);

        // Calculate loss (MSE)
        float loss = 0;
        float[] outputError = new float[2];
        for (int i = 0; i < 2; i++)
        {
            float diff = outputLayer[i] - target[i];
            loss += diff * diff;
            // Derivative of MSE * derivative of Tanh
            outputError[i] = 2 * diff * TanhDerivative(outputLayer[i]);
        }
        LastLoss = loss / 2;

        // Backpropagation
        // Layer 3 gradients
        float[] layer2Error = new float[16];
        for (int i = 0; i < 16; i++)
        {
            layer2Error[i] = 0;
            for (int j = 0; j < 2; j++)
            {
                layer2Error[i] += outputError[j] * weights3[i, j];
                // Update weights3
                weights3[i, j] -= learningRate * outputError[j] * layer2Activated[i];
            }
            layer2Error[i] *= ReLUDerivative(layer2Output[i]);
        }

        // Update biases3
        for (int j = 0; j < 2; j++)
            biases3[j] -= learningRate * outputError[j];

        // Layer 2 gradients
        float[] layer1Error = new float[16];
        for (int i = 0; i < 16; i++)
        {
            layer1Error[i] = 0;
            for (int j = 0; j < 16; j++)
            {
                layer1Error[i] += layer2Error[j] * weights2[i, j];
                // Update weights2
                weights2[i, j] -= learningRate * layer2Error[j] * layer1Activated[i];
            }
            layer1Error[i] *= ReLUDerivative(layer1Output[i]);
        }

        // Update biases2
        for (int j = 0; j < 16; j++)
            biases2[j] -= learningRate * layer2Error[j];

        // Layer 1 gradients
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                // Update weights1
                weights1[i, j] -= learningRate * layer1Error[j] * input[i];
            }
        }

        // Update biases1
        for (int j = 0; j < 16; j++)
            biases1[j] -= learningRate * layer1Error[j];
    }

    private float ReLU(float x) => Mathf.Max(0, x);
    private float ReLUDerivative(float x) => x > 0 ? 1 : 0;
    private float Tanh(float x) => (float)Math.Tanh(x);
    private float TanhDerivative(float x) => 1 - x * x; // x is already tanh(input)

    public void Reset()
    {
        InitializeWeights();
    }
}
