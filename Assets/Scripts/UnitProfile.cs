using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitProfile", menuName = "RTS/Unit Profile")]
public class UnitProfile : ScriptableObject {
    public string unitName;
    
    [Header("Base Points (1-10)")]
    public int attackPoint;
    public int firePoint;
    public int delayPoint;
    public int healthPoint;

    // Konversi poin ke nilai nyata sesuai paper
    public float RealHealth => healthPoint * 50f; 
    public float RealAttack => attackPoint * 10f; 
    public float RealFire => firePoint * 10f;     
    public float RealDelay => (5f - delayPoint) / 10f; 
    
    [Header("TA Extension")]
    public float attackRange; 
}