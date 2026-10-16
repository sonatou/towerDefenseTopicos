using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Vec2 = System.Numerics.Vector2;

namespace TowerDefense
{
    public class GameController : MonoBehaviour
    {
        private const string HighScoreKey = "highscore";

        [Header("Balanceamento")]
        [SerializeField] private GameSettings gameSettings = new GameSettings();
        [SerializeField] private TowerStats towerStats = new TowerStats();
        [SerializeField] private EnemyStats enemyStats = new EnemyStats();

        [Header("Cena")]
        [SerializeField] private Camera cam;
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap pathTilemap;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private Transform enemiesParent;
        [SerializeField] private Transform towersParent;
        [SerializeField] private Transform projectilesParent;

        [Header("Prefabs")]
        [SerializeField] private SpriteRenderer enemyPrefab;
        [SerializeField] private SpriteRenderer towerPrefab;
        [SerializeField] private SpriteRenderer projectilePrefab;

        [Header("HUD")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text endTitleText;
        [SerializeField] private float messageDuration = 2f;

        [Header("Visual")]
        [SerializeField] private Color[] towerLevelColors =
        {
            new Color(0.4f, 0.7f, 1f), new Color(0.1f, 0.3f, 1f), new Color(0.5f, 0.1f, 0.8f)
        };
        [SerializeField] private float towerBaseSize = 0.5f;
        [SerializeField] private float towerSizePerLevel = 0.1f;
        [SerializeField] private Color enemyFullColor = Color.red;
        [SerializeField] private Color enemyLowColor = new Color(0.3f, 0f, 0f);
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileBaseSize = 0.15f;
        [SerializeField] private float projectileSizePerLevel = 0.08f;

        private class Projectile
        {
            public Transform Transform;
            public Enemy Target;
            public int Damage;
        }

        private GameManager game;
        private Vec2[] path;

        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly Dictionary<Enemy, SpriteRenderer> enemySprites = new Dictionary<Enemy, SpriteRenderer>();
        private readonly Dictionary<Tower, SpriteRenderer> towerSprites = new Dictionary<Tower, SpriteRenderer>();
        private readonly List<Projectile> projectiles = new List<Projectile>();

        private void Awake()
        {
            path = new Vec2[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3Int cell = grid.WorldToCell(waypoints[i].position);
                path[i] = new Vec2(cell.x, cell.y);
            }
        }

        private void Start()
        {
            ResetGame();
        }

        public void ResetGame()
        {
            foreach (SpriteRenderer sr in enemySprites.Values) Destroy(sr.gameObject);
            foreach (SpriteRenderer sr in towerSprites.Values) Destroy(sr.gameObject);
            foreach (Projectile p in projectiles) Destroy(p.Transform.gameObject);
            enemies.Clear();
            enemySprites.Clear();
            towerSprites.Clear();
            projectiles.Clear();

            game = new GameManager(gameSettings, towerStats, enemyStats, PlayerPrefs.GetInt(HighScoreKey, 0));

            foreach (Vector3Int cell in groundTilemap.cellBounds.allPositionsWithin)
                if (groundTilemap.HasTile(cell) && !pathTilemap.HasTile(cell))
                    game.AddBuildableCell(cell.x, cell.y);

            game.NewHighScore += value => PlayerPrefs.SetInt(HighScoreKey, value);
            game.WaveStarted += wave => ShowMessage($"Onda {wave}!");
            game.Ended += ShowEnd;

            endPanel.SetActive(false);
            messageText.enabled = false;
            RefreshHud();
        }

        private void Update()
        {
            if (game.IsOver) return;

            float dt = Time.deltaTime;

            if (game.Tick(dt)) SpawnEnemy();

            foreach (Tower tower in game.Towers)
            {
                tower.Tick(dt);

                Enemy target = tower.FindTarget(enemies);
                int damage = tower.Shoot(target);
                if (damage > 0) FireProjectile(tower, target, damage);
            }

            UpdateProjectiles(dt);
            UpdateEnemies(dt);
            HandleInput();

            game.CheckEnd(enemies.Count);
            RefreshHud();
        }

        private void SpawnEnemy()
        {
            Enemy enemy = game.CreateEnemy(path);

            SpriteRenderer sr = Instantiate(enemyPrefab, CellToWorld(enemy.Position), Quaternion.identity, enemiesParent);
            enemies.Add(enemy);
            enemySprites[enemy] = sr;
        }

        private void UpdateEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                enemy.Move(dt);

                if (enemy.IsDead)
                {
                    game.EnemyKilled(enemy);
                    RemoveEnemy(i);
                }
                else if (enemy.ReachedEnd)
                {
                    game.EnemyReachedBase(enemy);
                    RemoveEnemy(i);
                }
                else
                {
                    SpriteRenderer sr = enemySprites[enemy];
                    sr.transform.position = CellToWorld(enemy.Position);
                    sr.color = Color.Lerp(enemyLowColor, enemyFullColor, (float)enemy.Hp / enemy.MaxHp);
                }
            }
        }

        private void RemoveEnemy(int index)
        {
            Enemy enemy = enemies[index];
            Destroy(enemySprites[enemy].gameObject);
            enemySprites.Remove(enemy);
            enemies.RemoveAt(index);
        }

        private void FireProjectile(Tower tower, Enemy target, int damage)
        {
            SpriteRenderer sr = Instantiate(projectilePrefab, towerSprites[tower].transform.position, Quaternion.identity, projectilesParent);
            sr.color = TowerColor(tower);
            sr.transform.localScale = Vector3.one * (projectileBaseSize + projectileSizePerLevel * tower.Level);

            projectiles.Add(new Projectile { Transform = sr.transform, Target = target, Damage = damage });
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = projectiles[i];

                if (!enemySprites.TryGetValue(p.Target, out SpriteRenderer targetSprite))
                {
                    Destroy(p.Transform.gameObject);
                    projectiles.RemoveAt(i);
                    continue;
                }

                Vector3 targetPos = targetSprite.transform.position;
                p.Transform.position = Vector3.MoveTowards(p.Transform.position, targetPos, projectileSpeed * dt);

                if (p.Transform.position == targetPos)
                {
                    p.Target.TakeDamage(p.Damage);
                    Destroy(p.Transform.gameObject);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void HandleInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            bool left = mouse.leftButton.wasPressedThisFrame;
            bool right = mouse.rightButton.wasPressedThisFrame;
            if (!left && !right) return;

            Vector3Int cell = grid.WorldToCell(cam.ScreenToWorldPoint(mouse.position.ReadValue()));
            Tower existing = game.GetTower(cell.x, cell.y);

            if (right)
            {
                if (existing != null) SellTower(cell);
                return;
            }

            if (existing != null) UpgradeTower(existing);
            else BuildTower(cell);
        }

        private void BuildTower(Vector3Int cell)
        {
            if (!game.CanBuild(cell.x, cell.y))
            {
                ShowMessage("Não dá pra construir aqui");
                return;
            }

            Tower tower = game.BuildTower(cell.x, cell.y);
            if (tower == null)
            {
                ShowMessage("Ouro insuficiente");
                return;
            }

            SpriteRenderer sr = Instantiate(towerPrefab, grid.GetCellCenterWorld(cell), Quaternion.identity, towersParent);
            towerSprites[tower] = sr;
            RefreshTower(tower);
        }

        private void UpgradeTower(Tower tower)
        {
            if (!tower.CanUpgrade)
            {
                ShowMessage("Torre já está no nível máximo");
                return;
            }

            int cost = tower.UpgradeCost;
            if (!game.UpgradeTower(tower))
            {
                ShowMessage($"Melhoria custa {cost}");
                return;
            }

            RefreshTower(tower);
        }

        private void SellTower(Vector3Int cell)
        {
            Tower tower = game.SellTower(cell.x, cell.y);
            ShowMessage($"Vendida por {tower.SellValue}");

            Destroy(towerSprites[tower].gameObject);
            towerSprites.Remove(tower);
        }

        private void RefreshTower(Tower tower)
        {
            SpriteRenderer sr = towerSprites[tower];
            sr.color = TowerColor(tower);
            sr.transform.localScale = Vector3.one * (towerBaseSize + towerSizePerLevel * tower.Level);
        }

        private Color TowerColor(Tower tower)
        {
            return towerLevelColors[Mathf.Clamp(tower.Level - 1, 0, towerLevelColors.Length - 1)];
        }

        private Vector3 CellToWorld(Vec2 cellPos)
        {
            Vector3 origin = grid.GetCellCenterWorld(Vector3Int.zero);
            return origin + new Vector3(cellPos.X * grid.cellSize.x, cellPos.Y * grid.cellSize.y, 0f);
        }

        private void RefreshHud()
        {
            goldText.text = $"Ouro: {game.Gold}";
            livesText.text = $"Vidas: {game.Lives}/{game.MaxLives}";
            waveText.text = $"Onda: {game.Wave}/{game.TotalWaves}";
            scoreText.text = $"Pontos: {game.Score}  (combo {game.Combo}, x{game.Multiplier})";
            highScoreText.text = $"Recorde: {game.HighScore}";
        }

        private void ShowMessage(string text)
        {
            messageText.text = text;
            messageText.enabled = true;
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), messageDuration);
        }

        private void HideMessage()
        {
            messageText.enabled = false;
        }

        private void ShowEnd(GameResult result)
        {
            endTitleText.text = result == GameResult.Victory ? "VITÓRIA!" : "DERROTA";
            endPanel.SetActive(true);
        }
    }
}
