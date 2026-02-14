using UnityEngine;
using System.Collections.Generic;

public class RadarSensor : MonoBehaviour {
    [Header("Sensor Settings")]
    public float sensorRadius = 25f; 
    public LayerMask unitLayer; // Di Inspector, pilih Team_A dan Team_B

    public float[] GetNeuronInputs(int inputCount) {
        float[] inputs = new float[inputCount];
        
        // 1. Deteksi semua unit dalam radius menggunakan Physics
        Collider[] hits = Physics.OverlapSphere(transform.position, sensorRadius, unitLayer);
        
        foreach (var hit in hits) {
            if (hit.gameObject == gameObject) continue; // Abaikan diri sendiri

            Vector3 direction = hit.transform.position - transform.position;
            float distance = direction.magnitude;
            
            // Dapatkan sudut dalam derajat (0 - 360) menggunakan Atan2
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;

            // Cek apakah kawan atau lawan berdasarkan Tag
            bool isFriend = hit.CompareTag(gameObject.tag);

            // ISI DATA REGION (Input 1-16)
            FillRegionData(inputs, angle, isFriend);

            // ISI DATA GRID (Input 22-37)
            if (distance < 5f) { 
                FillGridData(inputs, angle);
            }

            // TA EXTENSION: Dangerousness & Range Awareness (Input 38-45)
            if (inputCount == 45) {
                UnitStats targetStats = hit.GetComponent<UnitStats>();
                FillExtensionData(inputs, angle, targetStats, isFriend);
            }
        }

        // Input 17-21: Status Internal (HP & Posisi)
        UnitStats myStats = GetComponent<UnitStats>();
        if (myStats != null && myStats.profile != null) {
            inputs[16] = myStats.currentHealth / myStats.profile.RealHealth; // HP (0-1)
        }
        inputs[17] = transform.position.x / 50f; // Posisi X ternormalisasi
        inputs[18] = transform.position.z / 50f; // Posisi Z ternormalisasi

        return inputs;
    }

    void FillRegionData(float[] inputs, float angle, bool isFriend) {
        int regionIdx = (int)(angle / 90f); // Membagi 360 jadi 4 (0,1,2,3)
        if (regionIdx > 3) regionIdx = 3;

        // Offset: Kawan index 0-3, Lawan index 4-7 (menurut paper)
        int baseOffset = isFriend ? 0 : 4; 
        
        // Tambahkan nilai kecil setiap ada unit (akumulatif)
        inputs[baseOffset + regionIdx] = Mathf.Min(inputs[baseOffset + regionIdx] + 0.1f, 1f);
    }

    void FillGridData(float[] inputs, float angle) {
        int gridIdx = (int)(angle / 45f); // Membagi 360 jadi 8
        if (gridIdx > 7) gridIdx = 7;
        
        // Grid index mulai dari neuron ke-22 (index 21)
        inputs[21 + gridIdx] = 1f; 
    }

    void FillExtensionData(float[] inputs, float angle, UnitStats targetStats, bool isFriend) {
        if (isFriend || targetStats == null) return;

        int regionIdx = (int)(angle / 90f);
        if (regionIdx > 3) regionIdx = 3;

        // Neuron 38-41: Dangerousness (Index 37-40)
        inputs[37 + regionIdx] += targetStats.profile.RealAttack / 100f;

        // Neuron 42-45: Attack Range Awareness (Index 41-44)
        inputs[41 + regionIdx] += targetStats.profile.attackRange / 50f;
    }
}