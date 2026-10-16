using System;
using System.Collections.Generic;
using System.Numerics;

namespace TowerDefense
{
    public enum GameResult
    {
        Playing,
        Victory,
        Defeat
    }

    [Serializable]
    public class GameSettings
    {
        public int startGold = 100;
        public int lives = 10;
        public int totalWaves = 5;
        public int enemiesInFirstWave = 5;
        public int extraEnemiesPerWave = 2;
        public float spawnInterval = 1f;
        public float timeBetweenWaves = 3f;
        public int pointsPerKill = 10;
        public float comboWindow = 2f;
        public int killsPerMultiplier = 5;
        public int maxMultiplier = 4;
    }

    public class GameManager
    {
        public GameSettings Settings { get; private set; }
        public TowerStats TowerStats { get; private set; }
        public EnemyStats EnemyStats { get; private set; }

        public int Gold { get; private set; }
        public int Lives { get; private set; }
        public int MaxLives { get; private set; }

        public int Wave { get; private set; }
        public int TotalWaves { get; private set; }
        public int RemainingInWave { get; private set; }
        public bool AllWavesSpawned => Wave >= TotalWaves && RemainingInWave == 0;

        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int Combo { get; private set; }
        public int Multiplier => Math.Min(1 + Combo / Settings.killsPerMultiplier, Settings.maxMultiplier);

        public GameResult Result { get; private set; } = GameResult.Playing;
        public bool IsOver => Result != GameResult.Playing;

        public IEnumerable<Tower> Towers => towers.Values;
        public int TowerCount => towers.Count;

        public event Action<int> WaveStarted;
        public event Action<int> NewHighScore;
        public event Action<GameResult> Ended;

        private readonly HashSet<(int, int)> buildableCells = new HashSet<(int, int)>();
        private readonly Dictionary<(int, int), Tower> towers = new Dictionary<(int, int), Tower>();
        private float spawnTimer;
        private float comboTimer;

        public GameManager(GameSettings settings = null, TowerStats towerStats = null, EnemyStats enemyStats = null, int highScore = 0)
        {
            Settings = settings ?? new GameSettings();
            TowerStats = towerStats ?? new TowerStats();
            EnemyStats = enemyStats ?? new EnemyStats();

            if (Settings.startGold < 0)
                throw new ArgumentException("Ouro inicial não pode ser negativo.", nameof(settings));
            if (Settings.lives <= 0)
                throw new ArgumentException("A base precisa de pelo menos 1 vida.", nameof(settings));
            if (Settings.totalWaves <= 0 || Settings.enemiesInFirstWave <= 0 || Settings.extraEnemiesPerWave < 0)
                throw new ArgumentException("Configuração de ondas inválida.", nameof(settings));
            if (Settings.killsPerMultiplier <= 0 || Settings.maxMultiplier < 1)
                throw new ArgumentException("Configuração de combo inválida.", nameof(settings));
            if (highScore < 0)
                throw new ArgumentOutOfRangeException(nameof(highScore), "Recorde não pode ser negativo.");

            Gold = Settings.startGold;
            Lives = Settings.lives;
            MaxLives = Settings.lives;
            TotalWaves = Settings.totalWaves;
            HighScore = highScore;
            spawnTimer = Settings.timeBetweenWaves;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Valor não pode ser negativo.");

            if (Gold < amount) return false;

            Gold -= amount;
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Valor não pode ser negativo.");

            Gold += amount;
        }

        public void DamageBase(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Dano não pode ser negativo.");

            Lives = Math.Max(0, Lives - amount);
        }

        public void AddBuildableCell(int x, int y)
        {
            buildableCells.Add((x, y));
        }

        public bool CanBuild(int x, int y)
        {
            return buildableCells.Contains((x, y)) && !towers.ContainsKey((x, y));
        }

        public Tower GetTower(int x, int y)
        {
            return towers.TryGetValue((x, y), out Tower tower) ? tower : null;
        }

        public Tower BuildTower(int x, int y, Func<double> critRoll = null)
        {
            if (!CanBuild(x, y) || !TrySpend(TowerStats.cost)) return null;

            var tower = new Tower(x, y, TowerStats, critRoll);
            towers[(x, y)] = tower;
            return tower;
        }

        public bool UpgradeTower(Tower tower)
        {
            if (tower == null) throw new ArgumentNullException(nameof(tower));

            if (!tower.CanUpgrade || !TrySpend(tower.UpgradeCost)) return false;

            tower.LevelUp();
            return true;
        }

        public Tower SellTower(int x, int y)
        {
            Tower tower = GetTower(x, y);
            if (tower == null) return null;

            towers.Remove((x, y));
            AddGold(tower.SellValue);
            return tower;
        }

        public int EnemiesForWave(int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave), "A primeira onda é a 1.");
            return Settings.enemiesInFirstWave + (wave - 1) * Settings.extraEnemiesPerWave;
        }

        public int EnemyHpForWave(int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave), "A primeira onda é a 1.");
            return EnemyStats.baseHp + (wave - 1) * EnemyStats.hpPerWave;
        }

        public Enemy CreateEnemy(Vector2[] path)
        {
            return new Enemy(EnemyHpForWave(Wave), path, EnemyStats);
        }

        public bool Tick(float deltaTime)
        {
            if (comboTimer > 0f)
            {
                comboTimer -= deltaTime;
                if (comboTimer <= 0f) Combo = 0;
            }

            if (IsOver || AllWavesSpawned) return false;

            spawnTimer -= deltaTime;
            if (spawnTimer > 0f) return false;

            if (RemainingInWave == 0)
            {
                Wave++;
                RemainingInWave = EnemiesForWave(Wave);
                WaveStarted?.Invoke(Wave);
            }

            RemainingInWave--;
            spawnTimer = RemainingInWave == 0 ? Settings.timeBetweenWaves : Settings.spawnInterval;
            return true;
        }

        public int EnemyKilled(Enemy enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            if (!enemy.IsDead) throw new InvalidOperationException("O inimigo ainda está vivo.");

            AddGold(enemy.Stats.reward);

            Combo++;
            comboTimer = Settings.comboWindow;

            int points = Settings.pointsPerKill * Multiplier;
            Score += points;

            if (Score > HighScore)
            {
                HighScore = Score;
                NewHighScore?.Invoke(HighScore);
            }

            return points;
        }

        public void EnemyReachedBase(Enemy enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));

            DamageBase(enemy.Stats.damageToBase);
        }

        public GameResult CheckEnd(int aliveEnemies)
        {
            if (aliveEnemies < 0) throw new ArgumentOutOfRangeException(nameof(aliveEnemies));

            if (IsOver) return Result;

            if (Lives == 0)
                End(GameResult.Defeat);
            else if (AllWavesSpawned && aliveEnemies == 0)
                End(GameResult.Victory);

            return Result;
        }

        private void End(GameResult result)
        {
            Result = result;
            Ended?.Invoke(result);
        }
    }
}
