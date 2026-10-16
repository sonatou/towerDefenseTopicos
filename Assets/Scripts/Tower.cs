using System;
using System.Collections.Generic;
using System.Numerics;

namespace TowerDefense
{
    [Serializable]
    public class TowerStats
    {
        public int cost = 50;
        public int maxLevel = 3;
        public int baseDamage = 1;
        public int damagePerLevel = 1;
        public float range = 2.5f;
        public float cooldown = 0.5f;
        public float critChance = 0.15f;
        public int critMultiplier = 2;
    }

    public class Tower
    {
        public TowerStats Stats { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public Vector2 Position => new Vector2(X, Y);

        public int Level { get; private set; } = 1;
        public int TotalInvested { get; private set; }

        public int Damage => Stats.baseDamage + (Level - 1) * Stats.damagePerLevel;
        public bool CanUpgrade => Level < Stats.maxLevel;
        public int UpgradeCost => Stats.cost * Level;
        public int SellValue => TotalInvested / 2;
        public bool IsReady => cooldownTimer <= 0f;

        private readonly Func<double> critRoll;
        private float cooldownTimer;

        public Tower(int x, int y, TowerStats stats = null, Func<double> critRoll = null)
        {
            Stats = stats ?? new TowerStats();

            if (Stats.cost < 0)
                throw new ArgumentException("Custo não pode ser negativo.", nameof(stats));
            if (Stats.maxLevel < 1)
                throw new ArgumentException("Nível máximo precisa ser pelo menos 1.", nameof(stats));
            if (Stats.baseDamage < 0 || Stats.damagePerLevel < 0)
                throw new ArgumentException("Dano não pode ser negativo.", nameof(stats));
            if (Stats.range < 0f || Stats.cooldown < 0f)
                throw new ArgumentException("Alcance e recarga não podem ser negativos.", nameof(stats));
            if (Stats.critChance < 0f || Stats.critChance > 1f)
                throw new ArgumentException("Chance de crítico deve estar entre 0 e 1.", nameof(stats));
            if (Stats.critMultiplier < 1)
                throw new ArgumentException("Multiplicador de crítico precisa ser pelo menos 1.", nameof(stats));

            X = x;
            Y = y;
            TotalInvested = Stats.cost;
            this.critRoll = critRoll ?? new Random().NextDouble;
        }

        public void Tick(float deltaTime)
        {
            if (cooldownTimer > 0f) cooldownTimer -= deltaTime;
        }

        public bool IsInRange(Vector2 point)
        {
            return Vector2.Distance(Position, point) <= Stats.range;
        }

        public Enemy FindTarget(IEnumerable<Enemy> enemies)
        {
            Enemy closest = null;
            float best = float.MaxValue;

            foreach (Enemy enemy in enemies)
            {
                if (enemy.IsDead || !IsInRange(enemy.Position)) continue;

                float distance = Vector2.Distance(Position, enemy.Position);
                if (distance < best)
                {
                    best = distance;
                    closest = enemy;
                }
            }

            return closest;
        }

        public int Shoot(Enemy target)
        {
            if (!IsReady || target == null || target.IsDead || !IsInRange(target.Position))
                return 0;

            cooldownTimer = Stats.cooldown;

            bool isCritical = critRoll() < Stats.critChance;
            return isCritical ? Damage * Stats.critMultiplier : Damage;
        }

        public void LevelUp()
        {
            if (!CanUpgrade)
                throw new InvalidOperationException("A torre já está no nível máximo.");

            TotalInvested += UpgradeCost;
            Level++;
        }
    }
}
