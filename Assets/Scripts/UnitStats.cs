using UnityEngine;

/// <summary>
/// Menyimpan status unit dan menjalankan fitness function sesuai paper
/// Widhiyasana et al. 2022, Table VI.
///
/// REWARD / PUNISHMENT (RC) TABLE:
///   RC1    Move Success             +0.1 / move
///   RC2    Damage Taken             -1   / damage
///   RC3    Damage Given Enemy       +1   / damage  (RC3.1 melee, RC3.2 fire)
///   RC4    Crash with Wall          -0.1 / crash
///   RC5    Damage Given Friend      -1   / damage  (RC5.1 melee, RC5.2 fire)
///   RC6    Crash Friend/Enemy       -0.1 / crash   (per OnCollisionEnter, NO cooldown)
///   RC7.1  Damage to Nothing (miss) -1   / damage
///   RC7.2  Damage to Self           -1   / damage  (tidak berlaku: self-hit mustahil geometri)
///   RC8    Attack AND Fire both     -1
/// </summary>
public class UnitStats : MonoBehaviour {
    public UnitProfile profile;

    [Header("Visuals")]
    public MeshRenderer teamIndicatorRenderer;

    public float currentHealth;
    public bool  isDead = false;

    // Fitness tracking
    [HideInInspector] public float fitnessScore    = 0f;
    [HideInInspector] public int   chromosomeIndex = -1; // Diisi GameManager

    // ------------------------------------------------------------------ //
    //  Debug Counters (Read-Only) — tidak mempengaruhi fitness/win logic  //
    // ------------------------------------------------------------------ //
    [Header("Debug Stats (Read-Only)")]
    [SerializeField] private float _damageTakenTotal      = 0f;
    [SerializeField] private float _damageGivenEnemyTotal = 0f;
    [SerializeField] private float _damageGivenFriendTotal= 0f;
    [SerializeField] private float _damageMissedTotal     = 0f;
    [SerializeField] private int   _crashCountWall        = 0;
    [SerializeField] private int   _crashCountUnit        = 0;
    [SerializeField] private int   _moveCount             = 0;
    [SerializeField] private int   _attackCount           = 0;
    [SerializeField] private int   _fireCount             = 0;
    [SerializeField] private int   _idleCount             = 0;

    // Aksesor publik (read-only untuk kode lain)
    public float DamageTakenTotal       => _damageTakenTotal;
    public float DamageGivenEnemyTotal  => _damageGivenEnemyTotal;
    public float DamageGivenFriendTotal => _damageGivenFriendTotal;
    public float DamageMissedTotal      => _damageMissedTotal;
    public int   CrashCountWall         => _crashCountWall;
    public int   CrashCountUnit         => _crashCountUnit;
    public int   MoveCount              => _moveCount;
    public int   AttackCount            => _attackCount;
    public int   FireCount              => _fireCount;
    public int   IdleCount              => _idleCount;

    // (tidak ada cooldown RC6 — setiap OnCollisionEnter adalah 1 crash event sesuai paper)

    void Start() {
        if (profile != null)
            currentHealth = profile.RealHealth;

        ApplyTeamColor();
    }

    private void ApplyTeamColor() {
        if (teamIndicatorRenderer == null) return;

        if (gameObject.layer == LayerMask.NameToLayer("Team_A"))
            teamIndicatorRenderer.material.color = Color.green;
        else if (gameObject.layer == LayerMask.NameToLayer("Team_B"))
            teamIndicatorRenderer.material.color = Color.red;
    }

    // ------------------------------------------------------------------ //
    //  Damage                                                              //
    // ------------------------------------------------------------------ //
    /// <summary>
    /// Terima damage dari musuh.
    /// attacker != null → RC3 untuk penyerang, RC2 untuk diri sendiri.
    /// </summary>
    public void TakeDamage(float amount, UnitStats attacker = null, bool isMelee = true) {
        if (isDead) return;

        currentHealth  -= amount;
        fitnessScore   -= amount;        // RC2: Damage Taken (-1/damage)
        _damageTakenTotal += amount;     // debug counter

        if (attacker != null) {
            attacker.fitnessScore          += amount; // RC3: Damage Given Enemy (+1/damage)
            attacker._damageGivenEnemyTotal += amount; // debug counter
        }

        if (currentHealth <= 0) {
            currentHealth = 0;
            isDead        = true;
            if (GetComponent<NPCController>() is NPCController npc)
                npc.StopBrainLoop();
        }
    }

    // ------------------------------------------------------------------ //
    //  RC Methods (dipakai NPCController)                                  //
    // ------------------------------------------------------------------ //
    public void AddFitnessRC1()          { fitnessScore += 0.1f; _moveCount++;   }  // RC1  Move Success
    public void AddFitnessRC4()          { fitnessScore -= 0.1f; _crashCountWall++; }  // RC4  Crash Wall
    public void AddFitnessRC5(float dmg) { fitnessScore -= dmg;  _damageGivenFriendTotal += dmg; }  // RC5  Damage Friend
    /// <summary>RC6: Crash Friend/Enemy (-0.1/crash). Satu event = satu penalti, tanpa cooldown.</summary>
    public void AddFitnessRC6()          { fitnessScore -= 0.1f; _crashCountUnit++; }  // RC6  Crash Friend/Enemy
    public void AddFitnessRC7(float dmg) { fitnessScore -= dmg;  _damageMissedTotal += dmg; }  // RC7.1 Damage Nothing
    public void AddFitnessRC8()          { fitnessScore -= 1f;   _idleCount++; }  // RC8  Attack+Fire both
    public void AddAttackCount()         => _attackCount++;
    public void AddFireCount()           => _fireCount++;

    // ------------------------------------------------------------------ //
    //  Bookkeeping                                                         //
    // ------------------------------------------------------------------ //
    /// <summary>Hitung skor akhir setelah battle selesai.</summary>
    public void CalculateFinalFitness() { /* fitnessScore sudah terakumulasi real-time */ }

    /// <summary>Reset untuk battle baru.</summary>
    public void ResetStats() {
        if (profile != null) currentHealth = profile.RealHealth;
        isDead       = false;
        fitnessScore = 0f;

        // Reset debug counters
        _damageTakenTotal       = 0f;
        _damageGivenEnemyTotal  = 0f;
        _damageGivenFriendTotal = 0f;
        _damageMissedTotal      = 0f;
        _crashCountWall         = 0;
        _crashCountUnit         = 0;
        _moveCount              = 0;
        _attackCount            = 0;
        _fireCount              = 0;
        _idleCount              = 0;

        ApplyTeamColor();
    }
}