using UnityEngine;
using System.Collections.Generic;

public class RadarSensor : MonoBehaviour {
    public float sensorRadius = 20f; // Jangkauan radar
    public LayerMask unitLayer;      // Pilih Layer Team_A dan Team_B
    
    // Fungsi ini akan dipanggil oleh NPCController
    public float[] GetNeuronInputs(int inputCount) {
        float[] inputs = new float[inputCount];
        
        // 1. Deteksi semua unit di sekitar
        Collider[] hits = Physics.OverlapSphere(transform.position, sensorRadius, unitLayer);
        
        // 2. Logika Pengisian Neuron (1-16: 4 Region)
        // Kita bagi area 360 derajat menjadi 4 bagian (90 derajat per region)
        FillRegionData(hits, inputs);

        // 3. Logika Pengisian Neuron (22-37: 8 Grid)
        // Area kecil di sekitar unit
        FillGridData(hits, inputs);

        // 4. TA EXTENSION: Dangerousness & Attack Range (Input 38-45)
        if (inputCount == 45) {
            FillExtensionData(hits, inputs);
        }

        return inputs;
    }

    void FillRegionData(Collider[] hits, float[] inputs) {
        // Implementasi menghitung jumlah kawan/lawan di 4 kuadran
        // Neuron 1-4: Region 1 (Kawan), 5-8: Region 1 (Lawan), dst.
    }
    
    void FillExtensionData(Collider[] hits, float[] inputs) {
        // Logika kamu: Hitung total damage musuh per region
        // Neuron 38-41: Dangerousness
        // Neuron 42-45: Attack Range Awareness
    }
}