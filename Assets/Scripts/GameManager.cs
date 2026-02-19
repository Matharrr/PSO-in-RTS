using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GameManager – Genetic Algorithm (Eks 1: Replikasi Paper Murni)
/// Widhiyasana et al. 2022:
///
///   Populasi  : 56 kromosom (1 per unit: unit 1–28 = Team A, 29–56 = Team B)
///   Kromosom  : 720 bobot ANN (37×18 + 18×3)
///   Seleksi   : Tournament Selection (paper: TOS)
///   Crossover : Single-Point Crossover, CR = 0.6
///   Mutasi    : Random-Reset Mutation,  MR = 0.09
///   Elitism   : 1 individu terbaik dipertahankan
///   Iterasi   : maks 4000 generasi × 10 detik/battle
/// </summary>
public class GameManager : MonoBehaviour {

    // ------------------------------------------------------------------ //
    //  Inspector Settings
    // ------------------------------------------------------------------ //
    [Header("Simulation Settings")]
    public int   totalUnitsPerTeam = 28;      // 28 per tim = 56 total
    public float battleDuration    = 10f;     // Sesuai paper
    public GameObject[] unitPrefabs;          // 7 prefab unit

    [Header("GA Parameters (Eks 1 – Paper Baseline)")]
    public float crossoverRate  = 0.6f;       // Terbaik per paper
    public float mutationRate   = 0.09f;      // Terbaik per paper
    public int   maxGenerations = 4000;       // Sesuai paper
    public int   tournamentSize = 3;          // k untuk Tournament Selection
    public int   inputSize      = 37;         // Old ANN (replikasi murni)

    [Header("Runtime (Read-Only)")]
    public int   currentGeneration = 0;
    public float bestFitnessEver   = float.MinValue;

    // ------------------------------------------------------------------ //
    //  Private State
    // ------------------------------------------------------------------ //
    private const int POPULATION_SIZE = 56;   // Sesuai paper: 56 kromosom

    private float[][] population;             // [56][720]
    private float[]   fitness;                // [56] skor fitness per kromosom
    private int       chromosomeLength;
    private List<GameObject> spawnedUnits = new List<GameObject>();

    // ------------------------------------------------------------------ //
    //  Unity Lifecycle
    // ------------------------------------------------------------------ //
    void Start() {
        chromosomeLength = new NeuralNetwork(inputSize).weights.Length; // = 720
        population = InitRandomPopulation();
        fitness    = new float[POPULATION_SIZE];
        StartCoroutine(RunGALoop());
    }

    // ------------------------------------------------------------------ //
    //  Main GA Loop
    // ------------------------------------------------------------------ //
    IEnumerator RunGALoop() {
        while (currentGeneration < maxGenerations) {
            // 1. Spawn semua 56 unit
            SpawnBattle();

            // 2. Jalankan battle selama battleDuration
            yield return new WaitForSeconds(battleDuration);

            // 3. Evaluasi fitness tiap kromosom
            EvaluateFitness();
            LogGeneration();

            // 4. GA: seleksi → crossover → mutasi
            population = Evolve(population, fitness);

            currentGeneration++;
            ClearBattlefield();
        }
        Debug.Log("=== GA Training Selesai ===");
    }

    // ------------------------------------------------------------------ //
    //  Spawning
    // ------------------------------------------------------------------ //
    void SpawnBattle() {
        ClearBattlefield();
        // Unit 0–27  → kromosom 0–27  → Team A
        SpawnTeam("Team_A", new Vector3(-20f, 0f,  0f), chromoOffset: 0);
        // Unit 28–55 → kromosom 28–55 → Team B
        SpawnTeam("Team_B", new Vector3( 20f, 0f,  0f), chromoOffset: 28);
    }

    void SpawnTeam(string teamTag, Vector3 centerPos, int chromoOffset) {
        for (int i = 0; i < totalUnitsPerTeam; i++) {
            GameObject prefab    = unitPrefabs[Random.Range(0, unitPrefabs.Length)];
            Vector3    spawnPos  = centerPos + new Vector3(
                Random.Range(-4f, 4f), 0.5f, Random.Range(-15f, 15f));

            GameObject unit = Instantiate(prefab, spawnPos, Quaternion.identity);
            unit.tag   = teamTag;
            unit.layer = LayerMask.NameToLayer(teamTag);

            int chromoIdx = chromoOffset + i;

            // Pasang bobot kromosom ke ANN
            NPCController npc = unit.GetComponent<NPCController>();
            npc.brain = new NeuralNetwork(inputSize);
            System.Array.Copy(population[chromoIdx], npc.brain.weights, chromosomeLength);

            // Tandai kromosom index & reset stats
            UnitStats stats = unit.GetComponent<UnitStats>();
            if (stats != null) {
                stats.chromosomeIndex = chromoIdx;
                stats.ResetStats();
            }

            // Mulai loop ANN berbasis timer
            npc.StartBrainLoop();
            spawnedUnits.Add(unit);
        }
    }

    void ClearBattlefield() {
        foreach (var u in spawnedUnits) { if (u != null) Destroy(u); }
        spawnedUnits.Clear();
    }

    // ------------------------------------------------------------------ //
    //  Fitness Evaluation
    // ------------------------------------------------------------------ //
    void EvaluateFitness() {
        System.Array.Clear(fitness, 0, POPULATION_SIZE);
        foreach (var unit in spawnedUnits) {
            if (unit == null) continue;
            UnitStats stats = unit.GetComponent<UnitStats>();
            if (stats == null || stats.chromosomeIndex < 0) continue;
            stats.CalculateFinalFitness();
            fitness[stats.chromosomeIndex] = stats.fitnessScore;
        }
    }

    void LogGeneration() {
        float best = float.MinValue;
        foreach (float f in fitness) if (f > best) best = f;
        if (best > bestFitnessEver) bestFitnessEver = best;
        Debug.Log($"[Gen {currentGeneration:D4}] BestGen={best:F1}  BestEver={bestFitnessEver:F1}");
    }

    // ------------------------------------------------------------------ //
    //  GA Operators
    // ------------------------------------------------------------------ //
    float[][] Evolve(float[][] oldPop, float[] fit) {
        float[][] newPop = new float[POPULATION_SIZE][];

        // Elitism: satu individu terbaik tidak diubah
        int eliteIdx = 0;
        for (int i = 1; i < POPULATION_SIZE; i++)
            if (fit[i] > fit[eliteIdx]) eliteIdx = i;
        newPop[0] = CopyChromosome(oldPop[eliteIdx]);

        // Isi sisa dengan Tournament → Crossover → Mutasi
        for (int i = 1; i < POPULATION_SIZE; i += 2) {
            int p1 = TournamentSelect(fit);
            int p2 = TournamentSelect(fit);
            Crossover(oldPop[p1], oldPop[p2], out float[] c1, out float[] c2);
            Mutate(c1);
            Mutate(c2);
            newPop[i] = c1;
            if (i + 1 < POPULATION_SIZE) newPop[i + 1] = c2;
        }
        return newPop;
    }

    /// <summary>
    /// Tournament Selection (TOS) sesuai paper.
    /// Pilih tournamentSize kandidat acak, kembalikan yang fitness-nya tertinggi.
    /// </summary>
    int TournamentSelect(float[] fit) {
        int best = Random.Range(0, POPULATION_SIZE);
        for (int k = 1; k < tournamentSize; k++) {
            int candidate = Random.Range(0, POPULATION_SIZE);
            if (fit[candidate] > fit[best]) best = candidate;
        }
        return best;
    }

    /// <summary>Single-Point Crossover (CR = 0.6).</summary>
    public void Crossover(float[] p1, float[] p2, out float[] c1, out float[] c2) {
        c1 = new float[chromosomeLength];
        c2 = new float[chromosomeLength];
        if (Random.value < crossoverRate) {
            int point = Random.Range(1, chromosomeLength - 1);
            for (int i = 0; i < chromosomeLength; i++) {
                if (i < point) { c1[i] = p1[i]; c2[i] = p2[i]; }
                else           { c1[i] = p2[i]; c2[i] = p1[i]; }
            }
        } else {
            System.Array.Copy(p1, c1, chromosomeLength);
            System.Array.Copy(p2, c2, chromosomeLength);
        }
    }

    /// <summary>Random-Reset Mutation (MR = 0.09).</summary>
    public void Mutate(float[] chromosome) {
        for (int i = 0; i < chromosome.Length; i++)
            if (Random.value < mutationRate)
                chromosome[i] = Random.Range(-1f, 1f);
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //
    float[][] InitRandomPopulation() {
        float[][] pop = new float[POPULATION_SIZE][];
        for (int i = 0; i < POPULATION_SIZE; i++) {
            pop[i] = new float[chromosomeLength];
            for (int w = 0; w < chromosomeLength; w++)
                pop[i][w] = Random.Range(-1f, 1f);
        }
        return pop;
    }

    float[] CopyChromosome(float[] src) {
        float[] copy = new float[src.Length];
        System.Array.Copy(src, copy, src.Length);
        return copy;
    }
}