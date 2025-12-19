using UnityEngine;
using System;

/// <summary>Neural network: Linear(4,16)->ReLU->Linear(16,16)->ReLU->Linear(16,2)->Tanh</summary>
[Serializable]
public class NeuralNetwork
{
    float[,] w1, w2, w3;
    float[] b1, b2, b3;
    float[] l1Out, l1Act, l2Out, l2Act, outLayer;
    float lr = 0.0005f;
    bool initialized = false;

    // Use System.Random to avoid Unity serialization issues
    static System.Random sysRandom = new System.Random();

    public float[] LastOutput { get; private set; }
    public float LastLoss { get; private set; }

    public NeuralNetwork()
    {
        InitArrays();
    }

    void InitArrays()
    {
        l1Out = new float[16]; l1Act = new float[16];
        l2Out = new float[16]; l2Act = new float[16];
        outLayer = new float[2]; LastOutput = new float[2];
        w1 = new float[4, 16]; b1 = new float[16];
        w2 = new float[16, 16]; b2 = new float[16];
        w3 = new float[16, 2]; b3 = new float[2];
    }

    public void Initialize()
    {
        if (initialized) return;
        InitWeights();
        initialized = true;
    }

    void InitWeights()
    {
        if (w1 == null) InitArrays();

        float s1 = Mathf.Sqrt(2f / 4f), s2 = Mathf.Sqrt(2f / 16f);

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 16; j++)
                w1[i, j] = ((float)sysRandom.NextDouble() * 2f - 1f) * s1;

        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++) w2[i, j] = ((float)sysRandom.NextDouble() * 2f - 1f) * s2;
            for (int j = 0; j < 2; j++) w3[i, j] = ((float)sysRandom.NextDouble() * 2f - 1f) * s2;
        }

        initialized = true;
    }

    public float[] Forward(float[] input)
    {
        // Auto-initialize if needed
        if (!initialized) Initialize();

        // Layer 1
        for (int j = 0; j < 16; j++)
        {
            l1Out[j] = b1[j];
            for (int i = 0; i < 4; i++) l1Out[j] += input[i] * w1[i, j];
            l1Act[j] = ReLU(l1Out[j]);
        }

        // Layer 2
        for (int j = 0; j < 16; j++)
        {
            l2Out[j] = b2[j];
            for (int i = 0; i < 16; i++) l2Out[j] += l1Act[i] * w2[i, j];
            l2Act[j] = ReLU(l2Out[j]);
        }

        // Output layer
        for (int j = 0; j < 2; j++)
        {
            outLayer[j] = b3[j];
            for (int i = 0; i < 16; i++) outLayer[j] += l2Act[i] * w3[i, j];
            outLayer[j] = Tanh(outLayer[j]);
        }

        LastOutput[0] = outLayer[0]; LastOutput[1] = outLayer[1];
        return outLayer;
    }

    public void Train(float[] input, float[] target)
    {
        Forward(input);

        // Loss & output error
        float loss = 0;
        float[] outErr = new float[2];
        for (int i = 0; i < 2; i++)
        {
            float diff = outLayer[i] - target[i];
            loss += diff * diff;
            outErr[i] = 2 * diff * TanhDeriv(outLayer[i]);
        }
        LastLoss = loss / 2;

        // Backprop layer 3
        float[] l2Err = new float[16];
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                l2Err[i] += outErr[j] * w3[i, j];
                w3[i, j] -= lr * outErr[j] * l2Act[i];
            }
            l2Err[i] *= ReLUDeriv(l2Out[i]);
        }
        for (int j = 0; j < 2; j++) b3[j] -= lr * outErr[j];

        // Backprop layer 2
        float[] l1Err = new float[16];
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                l1Err[i] += l2Err[j] * w2[i, j];
                w2[i, j] -= lr * l2Err[j] * l1Act[i];
            }
            l1Err[i] *= ReLUDeriv(l1Out[i]);
        }
        for (int j = 0; j < 16; j++) b2[j] -= lr * l2Err[j];

        // Backprop layer 1
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 16; j++)
                w1[i, j] -= lr * l1Err[j] * input[i];
        for (int j = 0; j < 16; j++) b1[j] -= lr * l1Err[j];
    }

    float ReLU(float x) => Mathf.Max(0, x);
    float ReLUDeriv(float x) => x > 0 ? 1 : 0;
    float Tanh(float x) => (float)Math.Tanh(x);
    float TanhDeriv(float x) => 1 - x * x;

    public void Reset()
    {
        initialized = false;
        InitWeights();
    }
}
