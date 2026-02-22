using UnityEngine;

/// <summary>
/// Menyimpan status unit dan menjalankan fitness function sesuai paper
/// Widhiyasana et al. 2022, Table VI.
///
/// REWARD / PUNISHMENT (RC) TABLE:
///   RC1  Move Success          +0.1 / move
///   RC2  Damage Taken          -1   / damage
///   RC3  Damage Given Enemy    +1   / damage  (RC3.1 melee, RC3.2 fire)
///   RC4  Crash with Wall       -0.1 / crash
///   RC5  Damage Given Friend   -1   / damage  (RC5.1 melee, RC5.2 fire)
///   RC6  Crash Friend/Enemy    -0.1 / crash
///   RC7  Damage to Nothing     -1   / damage
///   RC8  Attack AND Fire both  -1
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

        if (attacker != null)
            attacker.fitnessScore += amount; // RC3: Damage Given Enemy (+1/damage)

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
    public void AddFitnessRC1()                   => fitnessScore += 0.1f;  // Move Success
    public void AddFitnessRC4()                   => fitnessScore -= 0.1f;  // Crash Wall
    public void AddFitnessRC5(float dmg)          => fitnessScore -= dmg;   // Damage Friend
    public void AddFitnessRC6()                   => fitnessScore -= 0.1f;  // Crash Unit
    public void AddFitnessRC7(float dmg)          => fitnessScore -= dmg;   // Damage Nothing
    public void AddFitnessRC8()                   => fitnessScore -= 1f;    // Attack+Fire both

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

        ApplyTeamColor();
    }
}