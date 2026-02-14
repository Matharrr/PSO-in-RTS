using UnityEngine;

public class NPCController : MonoBehaviour {
    public UnitStats stats;
    public RadarSensor radar; // Tambahkan referensi ke RadarSensor
    public NeuralNetwork brain;
    
    private Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
        // Jangan jalankan logika jika unit mati
        if (stats == null || stats.isDead) {
            if(rb != null) rb.velocity = Vector3.zero; // Berhenti jika mati
            return;
        }

        // 1. Kumpulkan data sensor melalui RadarSensor
        // brain.inputSize otomatis 37 atau 45 tergantung eksperimen
        float[] inputs = radar.GetNeuronInputs(brain.inputSize); 

        // 2. Berikan ke ANN (Feedforward)
        float[] outputs = brain.FeedForward(inputs);

        // 3. Eksekusi aksi berdasarkan output
        ApplyActions(outputs);
    }

    void ApplyActions(float[] actions) {
        // Interpretasi Output ANN (Sigmoid menghasilkan 0 sampai 1)
        // Kita ubah range 0..1 menjadi -1..1 untuk arah pergerakan
        float moveX = actions[0] * 2 - 1;
        float moveZ = actions[1] * 2 - 1;
        
        Vector3 moveDir = new Vector3(moveX, 0, moveZ);
        
        // Kecepatan gerak dipengaruhi oleh RealDelay (sesuai paper, unit ringan lebih gesit)
        // Kita gunakan pengali (misal 50f) agar pergerakan terlihat di Unity
        rb.velocity = moveDir * (1f - stats.profile.RealDelay) * 10f;

        // Logika Serangan (Output ke-3)
        if (actions[2] > 0.5f) {
            Attack();
        }
    }

    void Attack() {
        // Logika serangan akan kita isi setelah sistem targetting selesai
        // Debug.Log(gameObject.name + " menyerang!");
    }
}