using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Random = UnityEngine.Random;

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

    [Header("Test Mode Settings")]
    public int    testSeed           = 42;    // Seed base deterministik (per-battle: testSeed + b)
    public int    testBattleCount    = 100;   // Jumlah battle deterministik (paper pakai ~100)
    public bool   autoRunTestAfterGA = true;  // Otomatis jalankan test setelah GA selesai
    public bool   runTestOnly        = false; // TRUE = skip GA, langsung load ckpt & test
    public string checkpointFile     = "ckpt_gen_1000.json"; // File ckpt untuk runTestOnly
    [Tooltip("TRUE = paper-like measurement (pakai snapshot population 56 kromosom).\nFALSE = sanity check lama (bestChromosome vs random).")]
    public bool   measurementMode    = true;  // TRUE = paper-like (snapshot population)

    [Header("Checkpoint / Resume")]
    public bool enableCheckpoint   = true;
    public int  checkpointInterval = 1000;  // Simpan tiap N generasi

    // ------------------------------------------------------------------ //
    //  Private State
    // ------------------------------------------------------------------ //
    private const int POPULATION_SIZE = 56;   // Sesuai paper: 56 kromosom

    private float[][] population;             // [56][720]
    private float[]   fitness;                // [56] skor fitness per kromosom
    private int       chromosomeLength;
    private List<GameObject> spawnedUnits = new List<GameObject>();

    private float[] bestChromosome;           // Kromosom terbaik sepanjang GA
    private string  logPath;
    private string  testLogPath;

    // ------------------------------------------------------------------ //
    //  Unity Lifecycle
    // ------------------------------------------------------------------ //
    void Start() {
        chromosomeLength = new NeuralNetwork(inputSize).weights.Length; // = 720

        if (runTestOnly) {
            // ── Test-Only Mode ──────────────────────────────────────────
            // Seed dulu supaya population random lawan selalu konsisten tiap run
            Random.InitState(testSeed);
            population = InitRandomPopulation();
            fitness    = new float[POPULATION_SIZE];

            string ckptPath = Path.Combine(Application.persistentDataPath, checkpointFile);
            LoadCheckpoint(ckptPath);
            StartCoroutine(RunTestMode());
        } else {
            // ── Training Mode (GA Normal) ────────────────────────────────
            population = InitRandomPopulation();
            fitness    = new float[POPULATION_SIZE];

            // Inisialisasi CSV logger
            logPath = Path.Combine(Application.persistentDataPath,
                $"GA_Log_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(logPath, "Generation,BestGen,BestEver,AvgFitness,EliteIndex\n");
            Debug.Log("GA log file: " + logPath);

            StartCoroutine(RunGALoop());
        }
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
            UpdateBestChromosome();   // simpan kromosom terbaik sebelum evolve
            LogGeneration();

            // Checkpoint otomatis tiap checkpointInterval generasi
            if (enableCheckpoint && currentGeneration > 0 && currentGeneration % checkpointInterval == 0)
                SaveCheckpoint(currentGeneration);

            // 4. GA: seleksi → crossover → mutasi
            population = Evolve(population, fitness);

            currentGeneration++;
            ClearBattlefield();
        }
        Debug.Log("=== GA Training Selesai ===");

        if (autoRunTestAfterGA)
            StartCoroutine(RunTestMode());
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

    /// <param name="chromosomeOverride">Jika tidak null, semua unit tim ini pakai kromosom ini.</param>
    void SpawnTeam(string teamTag, Vector3 centerPos, int chromoOffset, float[] chromosomeOverride = null) {
        for (int i = 0; i < totalUnitsPerTeam; i++) {
            GameObject prefab    = unitPrefabs[Random.Range(0, unitPrefabs.Length)];
            Vector3    spawnPos  = centerPos + new Vector3(
                Random.Range(-4f, 4f), 0.5f, Random.Range(-15f, 15f));

            GameObject unit = Instantiate(prefab, spawnPos, Quaternion.identity);
            unit.tag   = teamTag;
            unit.layer = LayerMask.NameToLayer(teamTag);

            int     chromoIdx   = chromoOffset + i;
            float[] chromoToUse = chromosomeOverride ?? population[chromoIdx];

            // Pasang bobot kromosom ke ANN
            NPCController npc = unit.GetComponent<NPCController>();
            npc.brain = new NeuralNetwork(inputSize);
            System.Array.Copy(chromoToUse, npc.brain.weights, chromosomeLength);

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
        float best     = float.MinValue;
        float sum      = 0f;
        int   eliteIdx = 0;

        for (int i = 0; i < fitness.Length; i++) {
            float f = fitness[i];
            sum += f;
            if (f > best) { best = f; eliteIdx = i; }
        }

        float avg = sum / fitness.Length;
        // bestFitnessEver already updated by UpdateBestChromosome()

        Debug.Log($"[Gen {currentGeneration:D4}] BestGen={best:F1}  BestEver={bestFitnessEver:F1}  Avg={avg:F1}");

        // Simpan ke CSV
        string line = $"{currentGeneration},{best:F3},{bestFitnessEver:F3},{avg:F3},{eliteIdx}";
        File.AppendAllText(logPath, line + "\n");
    }

    /// <summary>Simpan kromosom terbaik generasi ini jika lebih baik dari bestFitnessEver.</summary>
    void UpdateBestChromosome() {
        int bestIdx = 0;
        for (int i = 1; i < POPULATION_SIZE; i++)
            if (fitness[i] > fitness[bestIdx]) bestIdx = i;

        if (fitness[bestIdx] > bestFitnessEver) {
            bestFitnessEver = fitness[bestIdx];
            bestChromosome  = CopyChromosome(population[bestIdx]);
            Debug.Log($"[BestEver] Gen={currentGeneration}  Idx={bestIdx}  Fitness={bestFitnessEver:F1}  → bestChromosome tersimpan");
        }
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
    //  Test Mode (Deterministik – bestEver chromosome)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Jalankan <see cref="testBattleCount"/> battle deterministik tanpa evolusi.
    /// Team A: semua unit pakai bestChromosome.
    /// Team B: populasi terakhir GA (kromosom 28–55).
    /// Seed di-reset ke <see cref="testSeed"/> sebelum setiap battle.
    /// Hasil disimpan ke CSV: Mode, Seed, Battle, Win, TeamA_AvgFitness, TeamA_Alive, TeamB_Alive.
    /// </summary>
    public IEnumerator RunTestMode() {
        // Guard: validasi ketersediaan data
        if (measurementMode) {
            if (population == null || population.Length != POPULATION_SIZE) {
                Debug.LogWarning("[Measurement] Population snapshot belum ada. Load checkpoint yang berisi population (ckpt_gen_XXXX.json).");
                yield break;
            }
        } else {
            if (bestChromosome == null) {
                Debug.LogWarning("[TestMode] Tidak ada bestChromosome – jalankan GA minimal 1 generasi.");
                yield break;
            }
        }

        string modeName = measurementMode ? "MEASURE" : "TEST";
        testLogPath = Path.Combine(Application.persistentDataPath,
            $"{modeName}_Log_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
        File.WriteAllText(testLogPath,
            "Mode,Seed,Battle,Win,AliveA,AliveB,TeamA_AvgFitness\n");
        Debug.Log($"[{modeName}] Mulai – {testBattleCount} battle  seedBase={testSeed}  log: {testLogPath}");

        int   wins        = 0;
        float totalFitA   = 0f;

        for (int b = 0; b < testBattleCount; b++) {
            // Seed berbeda per-battle supaya tidak identik
            int seed = testSeed + b;
            Random.InitState(seed);

            if (measurementMode) SpawnTestBattle_PaperLike();
            else                 SpawnTestBattle();

            yield return new WaitForSeconds(battleDuration);

            // Hitung hasil
            int   aliveA = 0, aliveB = 0;
            float sumFitA = 0f;
            foreach (var unit in spawnedUnits) {
                if (unit == null) continue;
                UnitStats us = unit.GetComponent<UnitStats>();
                if (us == null) continue;
                us.CalculateFinalFitness();

                bool isA = unit.CompareTag("Team_A");
                if (!us.isDead) { if (isA) aliveA++; else aliveB++; }
                if (isA) sumFitA += us.fitnessScore;
            }

            bool  win  = aliveA > aliveB;
            float avgA = sumFitA / totalUnitsPerTeam;
            wins      += win ? 1 : 0;
            totalFitA += avgA;

            string row = $"{modeName},{seed},{b + 1},{(win ? 1 : 0)},{aliveA},{aliveB},{avgA:F3}";
            File.AppendAllText(testLogPath, row + "\n");
            Debug.Log(
                $"[{modeName} {b + 1:D2}/{testBattleCount}]  Win={win}" +
                $"  AliveA={aliveA}  AliveB={aliveB}  AvgFitA={avgA:F1}");

            ClearBattlefield();
        }

        float winRate = (float)wins / testBattleCount * 100f;
        float avgFit  = totalFitA / testBattleCount;
        File.AppendAllText(testLogPath,
            $"SUMMARY,{testSeed},{testBattleCount},{winRate:F1}%,,, {avgFit:F3}\n");
        Debug.Log(
            $"=== {modeName} Selesai  WinRate={winRate:F0}%  AvgFitA={avgFit:F1}  log={testLogPath} ===");
    }

    /// <summary>
    /// Measurement stage paper-like (Gen 1000/2000/3000/4000):
    /// Team A pakai kromosom 0–27 dari population snapshot.
    /// Team B pakai kromosom 28–55 dari population snapshot.
    /// TIDAK ada override satu kromosom untuk semua unit.
    /// </summary>
    void SpawnTestBattle_PaperLike() {
        ClearBattlefield();
        SpawnTeam("Team_A", new Vector3(-20f, 0f,  0f), chromoOffset:  0, chromosomeOverride: null);
        SpawnTeam("Team_B", new Vector3( 20f, 0f,  0f), chromoOffset: 28, chromosomeOverride: null);
    }

    /// <summary>
    /// Spawn battle test (sanity check lama):
    /// Team A pakai bestChromosome (dari checkpoint).
    /// Team B pakai population (random deterministik – seed dari testSeed).
    /// </summary>
    void SpawnTestBattle() {
        ClearBattlefield();
        SpawnTeam("Team_A", new Vector3(-20f, 0f,  0f), chromoOffset: 0,
                  chromosomeOverride: bestChromosome);
        SpawnTeam("Team_B", new Vector3( 20f, 0f,  0f), chromoOffset: 28,
                  chromosomeOverride: null);
    }

    // ------------------------------------------------------------------ //
    //  Checkpoint
    // ------------------------------------------------------------------ //
    [System.Serializable]
    public class CheckpointData {
        public int     generation;
        public float   bestFitnessEver;
        public float[] bestChromosome;
        // Population snapshot (flattened): population[i][j] = populationFlat[i * chromoLength + j]
        public int     populationSize;    // = 56
        public int     chromoLength;      // = 720
        public float[] populationFlat;    // length = populationSize * chromoLength
    }

    void SaveCheckpoint(int gen) {
        // Flatten population untuk serialisasi JsonUtility (tidak support jagged array)
        float[] flat = new float[POPULATION_SIZE * chromosomeLength];
        for (int i = 0; i < POPULATION_SIZE; i++)
            System.Array.Copy(population[i], 0, flat, i * chromosomeLength, chromosomeLength);

        var data = new CheckpointData {
            generation      = gen,
            bestFitnessEver = bestFitnessEver,
            bestChromosome  = bestChromosome,
            populationSize  = POPULATION_SIZE,
            chromoLength    = chromosomeLength,
            populationFlat  = flat
        };

        string json  = JsonUtility.ToJson(data);
        string path  = Path.Combine(Application.persistentDataPath, $"ckpt_gen_{gen}.json");
        File.WriteAllText(path, json);
        Debug.Log($"[CKPT] Saved {path}  (population snapshot {POPULATION_SIZE}×{chromosomeLength} disertakan)");
    }

    /// <summary>Load bestChromosome dan population snapshot dari file checkpoint.</summary>
    public void LoadCheckpoint(string path) {
        if (!File.Exists(path)) {
            Debug.LogWarning($"[CKPT] File tidak ditemukan: {path}");
            return;
        }
        string json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<CheckpointData>(json);
        bestChromosome  = data.bestChromosome;
        bestFitnessEver = data.bestFitnessEver;

        // Rekonstruksi population dari flat array
        if (data.populationFlat != null &&
            data.populationSize  == POPULATION_SIZE &&
            data.chromoLength    == chromosomeLength) {
            population = new float[POPULATION_SIZE][];
            for (int i = 0; i < POPULATION_SIZE; i++) {
                population[i] = new float[chromosomeLength];
                System.Array.Copy(data.populationFlat, i * chromosomeLength,
                                  population[i], 0, chromosomeLength);
            }
            Debug.Log($"[CKPT] Loaded gen={data.generation}  bestFitnessEver={data.bestFitnessEver:F1}" +
                      $"  + population snapshot {POPULATION_SIZE}×{chromosomeLength}  dari {path}");
        } else {
            Debug.LogWarning("[CKPT] Population snapshot tidak ada atau tidak valid di file checkpoint ini.");
        }
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