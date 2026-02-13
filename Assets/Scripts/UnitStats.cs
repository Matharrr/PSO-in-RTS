using UnityEngine;

public class UnitStats : MonoBehaviour {
    public UnitProfile profile;
    public float currentHealth;
    public bool isDead = false;

    void Start() {
        if (profile != null) {
            currentHealth = profile.RealHealth;
        }
    }

    public void TakeDamage(float amount) {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0) {
            currentHealth = 0;
            isDead = true;
            // Di sini nanti tambahkan logika reset/disable unit
        }
    }
}