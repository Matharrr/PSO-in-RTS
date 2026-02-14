using UnityEngine;

public class NPCController : MonoBehaviour {
    [Header("Components")]
    public UnitStats stats;
    public RadarSensor radar;
    public NeuralNetwork brain; // Ini akan diisi oleh GameManager nanti
    
    private Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
        // Mencari referensi otomatis jika lupa ditarik di Inspector
        if (stats == null) stats = GetComponent<UnitStats>();
        if (radar == null) radar = GetComponent<RadarSensor>();
    }

    void FixedUpdate() {
        // Safety check: Jangan gerak kalau mati atau otak belum ada
        if (stats == null || stats.isDead || brain == null || radar == null) {
            if(rb != null) rb.velocity = Vector3.zero;
            return;
        }

        // 1. Ambil input dari radar (37 atau 45 neuron)
        float[] inputs = radar.GetNeuronInputs(brain.inputSize); 

        // 2. Masukkan ke ANN untuk diproses
        float[] outputs = brain.FeedForward(inputs);

        // 3. Eksekusi hasil (Output 0: Move X, Output 1: Move Z, Output 2: Attack)
        ApplyActions(outputs);
    }

    void ApplyActions(float[] actions) {
        // Mengubah range Sigmoid (0 sampai 1) menjadi arah (-1 sampai 1)
        float moveX = actions[0] * 2 - 1;
        float moveZ = actions[1] * 2 - 1;
        
        Vector3 moveDir = new Vector3(moveX, 0, moveZ);
        
        // Kecepatan gerak dipengaruhi oleh RealDelay unit (makin kecil delay, makin cepat)
        if(rb != null) {
            rb.velocity = moveDir * (1f - stats.profile.RealDelay) * 10f;
        }

        // Logika Menyerang (Output neuron ke-3)
        if (actions.Length > 2 && actions[2] > 0.5f) {
            Attack();
        }
    }

    void Attack() {
        // Logika damage akan kita buat setelah sistem targetting siap
        // Debug.Log(gameObject.name + " menyerang!");
    }
}