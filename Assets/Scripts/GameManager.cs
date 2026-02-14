using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    [Header("Simulation Settings")]
    public int totalUnitsPerTeam = 28;
    public float battleDuration = 10f; // Sesuai paper
    public GameObject[] unitPrefabs; // Masukkan 7 prefab ke sini di Inspector

    [Header("PSO Parameters")]
    public int particleCount = 20; // Jumlah simulasi per iterasi
    public int inputSize = 37; // Ubah ke 45 untuk Eks 3 & 4

    private List<GameObject> spawnedUnits = new List<GameObject>();

    void Start() {
        // Untuk tahap awal, kita coba spawn satu pertempuran dulu
        StartNewBattle();
    }

    public void StartNewBattle() {
        ClearBattlefield();
        SpawnTeam("Team_A", new Vector3(-20, 0, 0)); // Sisi kiri
        SpawnTeam("Team_B", new Vector3(20, 0, 0));  // Sisi kanan
    }

    void SpawnTeam(string teamTag, Vector3 centerPos) {
        for (int i = 0; i < totalUnitsPerTeam; i++) {
            GameObject prefab = unitPrefabs[Random.Range(0, unitPrefabs.Length)];
            
            // Jangkauan random dipersempit agar tidak di pinggir jurang
            // X: -10 sampai 10 dari titik pusat tim
            // Z: -15 sampai 15 agar tetap di dalam area 50x50
            Vector3 spawnPos = centerPos + new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-15f, 15f));
            
            GameObject unit = Instantiate(prefab, spawnPos, Quaternion.identity);
            unit.tag = teamTag;
            unit.layer = LayerMask.NameToLayer(teamTag);

            NPCController controller = unit.GetComponent<NPCController>();
            // Pastikan brain diinisialisasi dengan inputSize yang benar (37 atau 45)
            controller.brain = new NeuralNetwork(inputSize); 
            
            // Beri bobot acak awal agar ada pergerakan sedikit untuk tes
            for(int w = 0; w < controller.brain.weights.Length; w++) {
                controller.brain.weights[w] = Random.Range(-1f, 1f);
            }

            spawnedUnits.Add(unit);
        }
    }

    void ClearBattlefield() {
        foreach (var unit in spawnedUnits) {
            if (unit != null) Destroy(unit);
        }
        spawnedUnits.Clear();
    }
}