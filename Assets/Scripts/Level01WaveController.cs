using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Level01WaveController : MonoBehaviour
{
    [System.Serializable]
    private struct Wave
    {
        public int count;
        public float interval;
        public float speed;
        public float health;

        public Wave(int count, float interval, float speed, float health)
        {
            this.count = count;
            this.interval = interval;
            this.speed = speed;
            this.health = health;
        }
    }

    [SerializeField] private TowerDefenseLevelLayout layout;
    [SerializeField] private GameObject firePigPrefab;
    [SerializeField] private int baseLives = 5;
    [SerializeField] private float firstWaveDelay = 3f;
    [SerializeField] private float betweenWaveDelay = 6f;

    private readonly HashSet<FirePigEnemy> activeEnemies = new();
    private readonly Wave[] waves =
    {
        new(5, 1.25f, 1.05f, 60f),
        new(6, 1.00f, 1.15f, 85f),
        new(7, 0.78f, 1.25f, 120f)
    };

    private int currentWave;
    private int defeated;
    private bool spawning;
    private bool won;
    private bool lost;
    private Level01TowerBuildController buildController;

    public IReadOnlyCollection<FirePigEnemy> ActiveEnemies => activeEnemies;
    public int BaseLives => baseLives;
    public int Defeated => defeated;
    public int CurrentWaveDisplay => Mathf.Min(currentWave + 1, waves.Length);
    public int TotalWaves => waves.Length;
    public bool IsComplete => won || lost;

    public void Configure(TowerDefenseLevelLayout levelLayout, GameObject prefab)
    {
        layout = levelLayout;
        firePigPrefab = prefab;
    }

    private void Start()
    {
        buildController = GetComponent<Level01TowerBuildController>();
        Level01Hud hud = GetComponent<Level01Hud>() ?? gameObject.AddComponent<Level01Hud>();
        hud.Initialize(this);
        if (layout == null) layout = FindFirstObjectByType<TowerDefenseLevelLayout>();
        if (layout == null || firePigPrefab == null || layout.Waypoints == null || layout.Waypoints.Length < 2)
        {
            Debug.LogError("第一关无法开始：缺少路线或 Fire Pig B Prefab。");
            enabled = false;
            return;
        }
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(firstWaveDelay);
        for (currentWave = 0; currentWave < waves.Length && !lost; currentWave++)
        {
            spawning = true;
            Wave wave = waves[currentWave];
            for (int i = 0; i < wave.count && !lost; i++)
            {
                Spawn(wave);
                yield return new WaitForSeconds(wave.interval);
            }
            spawning = false;
            yield return new WaitUntil(() => activeEnemies.Count == 0 || lost);
            if (!lost && currentWave < waves.Length - 1)
                yield return new WaitForSeconds(betweenWaveDelay);
        }
        if (!lost) won = true;
    }

    private void Spawn(Wave wave)
    {
        Transform start = layout.Waypoints[0] != null ? layout.Waypoints[0] : layout.SpawnPoint;
        GameObject pig = Instantiate(firePigPrefab, start.position, start.rotation, transform);
        pig.name = $"Fire Pig B · Wave {currentWave + 1}";
        FirePigEnemy enemy = pig.GetComponent<FirePigEnemy>() ?? pig.AddComponent<FirePigEnemy>();
        activeEnemies.Add(enemy);
        enemy.Initialize(layout.Waypoints, wave.speed, wave.health, OnEnemyFinished);
    }

    private void OnEnemyFinished(FirePigEnemy enemy, bool reachedGoal)
    {
        activeEnemies.Remove(enemy);
        if (reachedGoal)
        {
            baseLives = Mathf.Max(0, baseLives - 1);
            if (baseLives == 0) lost = true;
        }
        else
        {
            defeated++;
            if (buildController != null) buildController.AddCoins(15);
        }
    }

}
