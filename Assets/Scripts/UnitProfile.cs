using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitProfile", menuName = "RTS/Unit Profile")]
public class UnitProfile : ScriptableObject {
    public string unitName;
    
    [Header("Base Points (1-10)")]
    public int attackPoint;
    public int firePoint;
    public int delayPoint;
    public int healthPoint;

    // Properti otomatis untuk konversi ke nilai nyata sesuai rumus paper
    public float RealHealth => healthPoint * 50f; // [cite: 290]
    public float RealAttack => attackPoint * 10f; // [cite: 290]
    public float RealFire => firePoint * 10f;     // [cite: 290]
    public float RealDelay => (5f - delayPoint) / 10f; // [cite: 291]
    
    [Header("TA Extension")]
    public float attackRange; // Untuk input baru kamu di Eks 3 & 4
}