using System;
using UnityEngine;

[Serializable]
public class NeuralNetwork {
    public int inputSize;   // 37 atau 45
    public int hiddenSize = 18; // 
    public int outputSize = 3;  // [cite: 323]

    public float[] weights; // Total: (In*Hid) + (Hid*Out)

    public NeuralNetwork(int inputCount) {
        this.inputSize = inputCount;
        int totalWeights = (inputSize * hiddenSize) + (hiddenSize * outputSize);
        weights = new float[totalWeights];
    }

    public float[] FeedForward(float[] inputs) {
        float[] hiddenLayer = new float[hiddenSize];
        float[] outputs = new float[outputSize];

        // 1. Input to Hidden
        int wIdx = 0;
        for (int i = 0; i < hiddenSize; i++) {
            float sum = 0;
            for (int j = 0; j < inputSize; j++) {
                sum += inputs[j] * weights[wIdx++];
            }
            hiddenLayer[i] = Sigmoid(sum);
        }

        // 2. Hidden to Output
        for (int i = 0; i < outputSize; i++) {
            float sum = 0;
            for (int j = 0; j < hiddenSize; j++) {
                sum += hiddenLayer[j] * weights[wIdx++];
            }
            outputs[i] = Sigmoid(sum);
        }
        return outputs;
    }

    private float Sigmoid(float x) {
        return 1f / (1f + Mathf.Exp(-x)); // 
    }
}