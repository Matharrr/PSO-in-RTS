using System;
using UnityEngine;

[Serializable]
public class NeuralNetwork {
    public int inputSize;   
    public int hiddenSize = 18; 
    public int outputSize = 3;  

    public float[] weights; 

    public NeuralNetwork(int inputCount) {
        this.inputSize = inputCount;
        int totalWeights = (inputSize * hiddenSize) + (hiddenSize * outputSize);
        weights = new float[totalWeights];
    }

    public float[] FeedForward(float[] inputs) {
        float[] hiddenLayer = new float[hiddenSize];
        float[] outputs = new float[outputSize];

        int wIdx = 0;
        // Input to Hidden
        for (int i = 0; i < hiddenSize; i++) {
            float sum = 0;
            for (int j = 0; j < inputSize; j++) {
                sum += inputs[j] * weights[wIdx++];
            }
            hiddenLayer[i] = Sigmoid(sum);
        }

        // Hidden to Output
        for (int i = 0; i < outputSize; i++) {
            float sum = 0;
            for (int j = 0; j < hiddenSize; j++) {
                sum += hiddenLayer[j] * weights[wIdx++];
            }
            outputs[i] = Sigmoid(sum);
        }
        return outputs;
    }

    private float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));
}