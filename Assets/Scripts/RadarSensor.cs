using UnityEngine;

/// <summary>
/// Menghasilkan tepat 37 input neuron sesuai definisi paper
/// Widhiyasana et al. 2022, Table II.
///
/// PETA 37 INPUT (index 0-based, nomor 1-based = nomor di paper):
///
///  [0 – 3]   Id 1–4   Region 1–4  Enemy  Average Distance  (0–1)
///  [4 – 7]   Id 5–8   Region 1–4  Friend Average Distance  (0–1)
///  [8 – 11]  Id 9–12  Region 1–4  Number of Enemy          (0–1)
///  [12–15]   Id 13–16 Region 1–4  Number of Friend         (0–1)
///  [16]      Id 17    Self Current Health                   (0–1)
///  [17]      Id 18    Self Delay                            (0–1)
///  [18]      Id 19    Self Attack                           (0–1)
///  [19]      Id 20    Self Fire                             (0–1)
///  [20]      Id 21    Prev output 2 (Attack/Fire/Move)      (0–1)
///  [21–28]   Id 22–29 Enemy  at Grid 1–8                    (0 or 1)
///  [29–36]   Id 30–37 Friend at Grid 1–8                    (0 or 1)
/// </summary>
public class RadarSensor : MonoBehaviour {

    [Header("Sensor Settings")]
    public float sensorRadius = 25f; // Radius deteksi region besar
    public float gridRadius   =  5f; // Radius deteksi grid kecil (small-scale)
    public LayerMask unitLayer;      // Layer Team_A dan Team_B

    // Diisi NPCController setiap step sebagai input 21 (index 20)
    [HideInInspector] public float previousActionOutput = 0f;

    private const int REGION_COUNT = 4; // 4 kuadran 90°
    private const int GRID_COUNT   = 8; // 8 arah 45°
    private const int INPUT_SIZE   = 37;

    public float[] GetNeuronInputs() {
        float[] inputs = new float[INPUT_SIZE];

        // Akumulator per-region
        float[] enemyDistSum  = new float[REGION_COUNT];
        float[] friendDistSum = new float[REGION_COUNT];
        int[]   enemyCount    = new int[REGION_COUNT];
        int[]   friendCount   = new int[REGION_COUNT];

        Collider[] hits = Physics.OverlapSphere(transform.position, sensorRadius, unitLayer);
        foreach (var hit in hits) {
            if (hit.gameObject == gameObject) continue;

            Vector3 dir      = hit.transform.position - transform.position;
            float   distance = dir.magnitude;
            bool    isFriend = hit.CompareTag(gameObject.tag);

            // Sudut 0–360° dari sumbu Z+, searah jarum jam
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Region 0–3 (kuadran 90°)
            int region = Mathf.Clamp((int)(angle / 90f), 0, REGION_COUNT - 1);

            if (isFriend) { friendDistSum[region] += distance; friendCount[region]++; }
            else          { enemyDistSum[region]  += distance; enemyCount[region]++;  }

            // Grid kecil (8 arah 45°) – hanya jika dalam gridRadius
            if (distance <= gridRadius) {
                int gridIdx = Mathf.Clamp((int)(angle / 45f), 0, GRID_COUNT - 1);
                if (isFriend) inputs[29 + gridIdx] = 1f; // Id 30–37: Friend at Grid
                else          inputs[21 + gridIdx] = 1f; // Id 22–29: Enemy  at Grid
            }
        }

        // Isi region inputs [0–15]
        for (int r = 0; r < REGION_COUNT; r++) {
            // Average distance ternormalisasi dengan sensorRadius
            inputs[r]      = enemyCount[r]  > 0
                             ? Mathf.Clamp01(enemyDistSum[r]  / enemyCount[r]  / sensorRadius)
                             : 0f;
            inputs[4 + r]  = friendCount[r] > 0
                             ? Mathf.Clamp01(friendDistSum[r] / friendCount[r] / sensorRadius)
                             : 0f;

            // Count ternormalisasi (maks ~14 unit per region dari 28 musuh)
            inputs[8 + r]  = Mathf.Clamp01(enemyCount[r]  / 14f);
            inputs[12 + r] = Mathf.Clamp01(friendCount[r] / 14f);
        }

        // Self stats [16–19]
        UnitStats myStats = GetComponent<UnitStats>();
        if (myStats != null && myStats.profile != null) {
            inputs[16] = Mathf.Clamp01(myStats.currentHealth / myStats.profile.RealHealth);
            inputs[17] = Mathf.Clamp01(myStats.profile.delayPoint  / 6f); // delay point 1–6
            inputs[18] = Mathf.Clamp01(myStats.profile.attackPoint / 4f); // attack point 1–4
            inputs[19] = Mathf.Clamp01(myStats.profile.firePoint   / 4f); // fire   point 1–4
        }

        // Previous action output [20]
        inputs[20] = previousActionOutput;

        return inputs;
    }
}