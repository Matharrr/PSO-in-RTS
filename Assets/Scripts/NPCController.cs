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
///   Fire<0.5  & Attack>=0.5 → ATTACK melee (RC3.1 jika kena, RC7 jika miss)
///   Fire>=0.5 & Attack<0.5  → FIRE  ranged (RC3.2 jika kena, RC7 jika miss)
///   Fire>=0.5 & Attack>=0.5 → IDLE  (RC8)
///
/// GRID ARAH (Table IV - 8 compass direction, searah jarum jam dari N):
///   Grid 1=N, 2=NE, 3=E, 4=SE, 5=S, 6=SW, 7=W, 8=NW
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

        // Simpan outputs[1] sebagai previous action untuk input 21
        radar.previousActionOutput = outputs[1];

        // Grid arah dari Output 2 (Table IV: range 0-1 dibagi 8 slot)
        int gridIdx   = Mathf.Clamp((int)(outputs[1] * 8f), 0, 7);
        Vector3 dir   = GridDirections[gridIdx];

        if (!doFire && !doAttack) {
            // MOVE
            Move(dir);
            if (stats != null) stats.AddFitnessRC1();
        } else if (!doFire && doAttack) {
            // ATTACK (melee) - RC3.1
            StopMoving();
            PerformAttack(dir, isMelee: true);
        } else if (doFire && !doAttack) {
            // FIRE (ranged) - RC3.2
            StopMoving();
            PerformAttack(dir, isMelee: false);
        } else {
            // IDLE (keduanya aktif) - RC8
            StopMoving();
            if (stats != null) stats.AddFitnessRC8();
        }
    }

    void Move(Vector3 dir) {
        if (rb == null || stats == null || stats.profile == null) return;
        // Kecepatan: semakin tinggi delay point, semakin cepat unit bergerak
        float speed = stats.profile.delayPoint * 1.2f;
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

    /// <summary>Cari unit mana pun (friend/enemy) dalam cone 45°, dalam range. Untuk FIRE/ranged.</summary>
    UnitStats FindAnyUnitInDirection(Vector3 dir, float range) {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, radar.unitLayer);
        UnitStats  best    = null;
        float      bestDot = 0.5f;

        foreach (var hit in hits) {
            if (hit.gameObject == gameObject) continue;

            UnitStats us = hit.GetComponent<UnitStats>();
            if (us == null || us.isDead) continue;

            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float   dot      = Vector3.Dot(dir, toTarget);
            if (dot > bestDot) { bestDot = dot; best = us; }
        }
        return best;
    }

    /// <summary>Cari musuh terdekat dalam cone 45° ke arah dir, dalam jarak range.</summary>
    UnitStats FindEnemyInDirection(Vector3 dir, float range) {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, radar.unitLayer);
        UnitStats  best    = null;
        float      bestDot = 0.5f; // cos(60°) ≈ 0.5: batas cone

        foreach (var hit in hits) {
            if (hit.gameObject == gameObject)    continue;
            if (hit.CompareTag(gameObject.tag))  continue; // Abaikan kawan

            UnitStats us = hit.GetComponent<UnitStats>();
            if (us == null || us.isDead) continue;

            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float   dot      = Vector3.Dot(dir, toTarget);
            if (dot > bestDot) { bestDot = dot; best = us; }
        }
        return best;
    }

    void OnCollisionEnter(Collision col) {
        // RC6: tabrakan dengan unit lain (friend atau enemy) sesuai paper
        if (col.gameObject != gameObject && col.gameObject.GetComponent<UnitStats>() != null)
            if (stats != null) stats.AddFitnessRC6();
    }
}