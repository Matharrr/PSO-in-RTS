using UnityEngine;

// 1. Definisikan label tim
public enum TeamLabel {
    TeamA,
    TeamB
}

public class UnitStats : MonoBehaviour {
    public UnitProfile profile;
    
    // 2. Tambahkan variabel tim agar bisa diatur di Inspector Unity
    [Header("Team Setup")]
    public TeamLabel team; 

    public float currentHealth;
    public bool isDead = false;

    void Start() {
        if (profile != null) {
            currentHealth = profile.RealHealth; // Tetap menggunakan rumus paper
        }
    }

    public void TakeDamage(float amount) {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0) {
            currentHealth = 0;
            isDead = true;
            // Logika unit mati
        }
    }
}