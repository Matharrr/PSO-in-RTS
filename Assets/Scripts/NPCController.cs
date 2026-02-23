using UnityEngine;
using System.Collections;

/// <summary>
/// Mengontrol unit NPC berdasarkan output ANN dengan interpretasi diskrit sesuai
/// paper Widhiyasana et al. 2022, Table III & IV.
///
/// ANN OUTPUT MAPPING:
///   outputs[0] = Output 1 (Fire)        : < 0.5 = bukan fire,  >= 0.5 = fire
///   outputs[1] = Output 2 (Coordinate)  : menentukan grid arah (Table IV)
///   outputs[2] = Output 3 (Attack)      : < 0.5 = bukan attack, >= 0.5 = attack
///
/// AKSI DISKRIT (Table III):
///   Fire<0.5  & Attack<0.5  → MOVE  (RC1)
///   Fire<0.5  & Attack>=0.5 → ATTACK melee (RC3.1 jika kena, RC7.1 jika miss)
///   Fire>=0.5 & Attack<0.5  → FIRE  ranged (RC3.2 jika kena, RC7.1 jika miss)
///   Fire>=0.5 & Attack>=0.5 → IDLE  (RC8)
///
/// GRID ARAH (Table IV - 8 compass direction, searah jarum jam dari N):
///   Grid 1=N, 2=NE, 3=E, 4=SE, 5=S, 6=SW, 7=W, 8=NW
///
/// CONE SERANGAN (100% paper-faithful):
///   Half-angle = 45° → threshold dot = cos(45°) ≈ 0.707
///   Target dipilih berdasarkan jarak TERDEKAT dalam cone (bukan best-dot).
///
/// RC7.2 SELF (paper Table VI):
///   Self-hit tidak mungkin terjadi secara geometri dalam simulator ini
///   (OverlapSphere selalu mengecualikan collider milik unit sendiri).
///   → RC7.1 (nothing) tetap diterapkan saat tidak ada target di cone.
/// </summary>
public class NPCController : MonoBehaviour {

    [Header("Components")]
    public UnitStats    stats;
    public RadarSensor  radar;
    public NeuralNetwork brain; // Diisi GameManager saat spawn

    [Header("Combat Settings")]
    public float meleeRange  =  3f;  // Jangkauan serangan melee
    public float rangedRange = 15f;  // Jangkauan serangan ranged (fire)
    public float arenaHalfSize = 24f; // Batas arena untuk deteksi wall

    private Rigidbody rb;

    // 8 arah compass (N, NE, E, SE, S, SW, W, NW)
    private static readonly Vector3[] GridDirections = {
        new Vector3( 0, 0,  1).normalized, // Grid 1: N
        new Vector3( 1, 0,  1).normalized, // Grid 2: NE
        new Vector3( 1, 0,  0),            // Grid 3: E
        new Vector3( 1, 0, -1).normalized, // Grid 4: SE
        new Vector3( 0, 0, -1),            // Grid 5: S
        new Vector3(-1, 0, -1).normalized, // Grid 6: SW
        new Vector3(-1, 0,  0),            // Grid 7: W
        new Vector3(-1, 0,  1).normalized, // Grid 8: NW
    };

    void Awake() {
        rb    = GetComponent<Rigidbody>();
        if (stats == null) stats = GetComponent<UnitStats>();
        if (radar == null) radar = GetComponent<RadarSensor>();
    }

    /// <summary>Dipanggil GameManager setelah brain diisi.</summary>
    public void StartBrainLoop() => StartCoroutine(BrainLoop());

    /// <summary>Hentikan loop (unit mati / battle berakhir).</summary>
    public void StopBrainLoop() {
        StopAllCoroutines();
        if (rb != null) rb.velocity = Vector3.zero;
    }

    IEnumerator BrainLoop() {
        while (stats != null && !stats.isDead) {
            // Interval timer sesuai delay unit (Table V)
            float timer = (stats.profile != null) ? stats.profile.RealDelayTimer : 0.3f;
            yield return new WaitForSeconds(timer);

            if (brain == null || radar == null || stats == null || stats.isDead) break;

            float[] inputs  = radar.GetNeuronInputs();
            float[] outputs = brain.FeedForward(inputs);
            DecodeAndAct(outputs);
        }
    }

    void DecodeAndAct(float[] outputs) {
        bool doFire   = outputs[0] >= 0.5f; // Output 1
        bool doAttack = outputs[2] >= 0.5f; // Output 3

        // Grid arah dari Output 2 (Table IV: threshold-based sesuai paper)
        int gridIdx   = Output2ToGrid(outputs[1]);
        Vector3 dir   = GridDirections[gridIdx];

        // Encode previous action sebagai nilai diskrit untuk input neuron #21
        // Paper: Prev (Attack/Fire/Move) → nilai diskrit agar ANN punya "memori satu langkah"
        // MOVE=0.0 | ATTACK=0.33 | FIRE=0.66 | IDLE=1.0
        float prevAction;

        if (!doFire && !doAttack) {
            // MOVE
            prevAction = 0.0f;
            Move(dir);
            if (stats != null) stats.AddFitnessRC1();
        } else if (!doFire && doAttack) {
            // ATTACK (melee) - RC3.1
            prevAction = 0.33f;
            StopMoving();
            if (stats != null) stats.AddAttackCount();
            PerformAttack(dir, isMelee: true);
        } else if (doFire && !doAttack) {
            // FIRE (ranged) - RC3.2
            prevAction = 0.66f;
            StopMoving();
            if (stats != null) stats.AddFireCount();
            PerformAttack(dir, isMelee: false);
        } else {
            // IDLE (keduanya aktif) - RC8
            prevAction = 1.0f;
            StopMoving();
            if (stats != null) stats.AddFitnessRC8();
        }

        // Simpan aksi yang benar-benar diambil sebagai previous action untuk input #21 berikutnya
        radar.previousActionOutput = prevAction;
    }

    void Move(Vector3 dir) {
        if (rb == null || stats == null || stats.profile == null) return;
        // Kecepatan konstan per profile (default 5 m/s).
        // Paper hanya mendefinisikan delayPoint sebagai interval ANN (Table V),
        // bukan kecepatan gerak — pisahkan agar unit tidak "double slow".
        float speed = stats.profile.moveSpeed;
        rb.velocity = dir * speed;

        // Deteksi batas arena → RC4
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x) >= arenaHalfSize || Mathf.Abs(pos.z) >= arenaHalfSize) {
            rb.velocity = Vector3.zero;
            stats.AddFitnessRC4();
        }
    }

    void StopMoving() {
        if (rb != null) rb.velocity = Vector3.zero;
    }

    void PerformAttack(Vector3 dir, bool isMelee) {
        if (stats == null || stats.profile == null) return;

        float range  = isMelee ? meleeRange  : rangedRange;
        float damage = isMelee ? stats.profile.RealAttack : stats.profile.RealFire;

        // Paper-consistent: attack/fire bisa mengenai unit pertama (friend/enemy)
        // → memungkinkan RC5.1 (melee ke kawan) dan RC5.2 (fire ke kawan)
        UnitStats target = FindAnyUnitInDirection(dir, range);

        if (target == null) {
            // RC7: serangan tidak mengenai siapa pun
            stats.AddFitnessRC7(damage);
            return;
        }

        bool isFriend = target.CompareTag(gameObject.tag);

        if (isFriend) {
            // Target kena RC2 (damage taken), attacker kena RC5 (damage given friend)
            target.TakeDamage(damage, attacker: null, isMelee: isMelee);
            stats.AddFitnessRC5(damage);
        } else {
            // Musuh: attacker dapat RC3, target kena RC2
            target.TakeDamage(damage, attacker: stats, isMelee: isMelee);
        }
    }

    /// <summary>
    /// Cari unit mana pun (friend/enemy) dalam cone 45° ke arah dir, dalam range.
    /// Memilih unit TERDEKAT dalam cone sesuai spirit paper (bukan best-dot).
    /// Cone threshold = cos(45°) ≈ 0.707, sesuai lebar satu grid (Fig. 3b paper).
    /// Self dikecualikan → RC7.2 tidak dibangkitkan (self-hit mustahil secara geometri).
    /// </summary>
    UnitStats FindAnyUnitInDirection(Vector3 dir, float range) {
        Collider[] hits        = Physics.OverlapSphere(transform.position, range, radar.unitLayer);
        UnitStats  nearest     = null;
        float      nearestDist = float.MaxValue;

        // cos(45°) ≈ 0.707 — sesuai lebar grid 45° pada paper Fig. 3b
        const float ConeThreshold = 0.707f;

        foreach (var hit in hits) {
            if (hit.gameObject == gameObject) continue; // Self dikecualikan (RC7.2 tidak berlaku)

            UnitStats us = hit.GetComponent<UnitStats>();
            if (us == null || us.isDead) continue;

            Vector3 toTarget = hit.transform.position - transform.position;
            float   dist     = toTarget.magnitude;
            float   dot      = Vector3.Dot(dir, toTarget.normalized);

            if (dot >= ConeThreshold && dist < nearestDist) {
                nearestDist = dist;
                nearest     = us;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Cari musuh TERDEKAT dalam cone 45° ke arah dir, dalam jarak range.
    /// Cone threshold = cos(45°) ≈ 0.707, sesuai lebar satu grid (Fig. 3b paper).
    /// </summary>
    UnitStats FindEnemyInDirection(Vector3 dir, float range) {
        Collider[] hits        = Physics.OverlapSphere(transform.position, range, radar.unitLayer);
        UnitStats  nearest     = null;
        float      nearestDist = float.MaxValue;

        // cos(45°) ≈ 0.707 — sesuai lebar grid 45° pada paper Fig. 3b
        const float ConeThreshold = 0.707f;

        foreach (var hit in hits) {
            if (hit.gameObject == gameObject)   continue;
            if (hit.CompareTag(gameObject.tag)) continue; // Abaikan kawan

            UnitStats us = hit.GetComponent<UnitStats>();
            if (us == null || us.isDead) continue;

            Vector3 toTarget = hit.transform.position - transform.position;
            float   dist     = toTarget.magnitude;
            float   dot      = Vector3.Dot(dir, toTarget.normalized);

            if (dot >= ConeThreshold && dist < nearestDist) {
                nearestDist = dist;
                nearest     = us;
            }
        }
        return nearest;
    }

    void OnCollisionEnter(Collision col) {
        // RC6: tabrakan dengan unit lain (friend atau enemy) sesuai paper
        if (col.gameObject != gameObject && col.gameObject.GetComponent<UnitStats>() != null)
            if (stats != null) stats.AddFitnessRC6();
    }

    /// <summary>
    /// Konversi Output 2 (0..1) ke index grid (0..7) sesuai Table IV paper.
    /// Threshold: 0.00-0.11=Grid1, 0.11-0.22=Grid2, ..., 0.77-1.00=Grid8.
    /// </summary>
    int Output2ToGrid(float o2) {
        if (o2 < 0.11f) return 0; // Grid 1: N
        if (o2 < 0.22f) return 1; // Grid 2: NE
        if (o2 < 0.33f) return 2; // Grid 3: E
        if (o2 < 0.44f) return 3; // Grid 4: SE
        if (o2 < 0.55f) return 4; // Grid 5: S
        if (o2 < 0.66f) return 5; // Grid 6: SW
        if (o2 < 0.77f) return 6; // Grid 7: W
        return 7;                  // Grid 8: NW
    }
}