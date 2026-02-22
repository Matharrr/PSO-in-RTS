using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitProfile", menuName = "RTS/Unit Profile")]
public class UnitProfile : ScriptableObject {
    public string unitName;
    
    [Header("Base Points (1-10)")]
    public int attackPoint;
    public int firePoint;
    public int delayPoint;
    public int healthPoint;

    [Header("Movement")]
    [Tooltip("Kecepatan gerak unit (m/s). Paper tidak menyebut speed per unit-type;\ngunakan nilai konstan (default 5) untuk replikasi baseline paper.")]
    public float moveSpeed = 5f;

    // Konversi poin ke nilai nyata sesuai paper (Table V)
    public float RealHealth => healthPoint * 50f;
    public float RealAttack => attackPoint * 10f;
    public float RealFire   => firePoint   * 10f;

    /// <summary>
    /// Interval timer ANN per paper Table V: (7 - delayPoint) × 0.1 detik.
    /// Cavalry (6) → 0.1s | Swordman (5) → 0.2s | Very Heavy (1) → 0.6s
    /// </summary>
    public float RealDelayTimer => (7f - delayPoint) * 0.1f;
}