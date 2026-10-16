using System;
using System.Numerics;

namespace TowerDefense
{
    [Serializable]
    public class EnemyStats
    {
        public int baseHp = 3;
        public int hpPerWave = 1;
        public float speed = 1.5f;
        public int reward = 10;
        public int damageToBase = 1;
    }

    public class Enemy
    {
        public EnemyStats Stats { get; private set; }

        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;

        public Vector2 Position { get; private set; }
        public bool ReachedEnd => targetIndex >= path.Length;

        private readonly Vector2[] path;
        private int targetIndex;

        public Enemy(int maxHp, Vector2[] path, EnemyStats stats = null)
        {
            Stats = stats ?? new EnemyStats();

            if (maxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp), "A vida precisa ser maior que zero.");
            if (path == null || path.Length < 2)
                throw new ArgumentException("O caminho precisa de pelo menos 2 pontos.", nameof(path));
            if (Stats.speed < 0f)
                throw new ArgumentException("Velocidade não pode ser negativa.", nameof(stats));
            if (Stats.reward < 0 || Stats.damageToBase < 0)
                throw new ArgumentException("Recompensa e dano na base não podem ser negativos.", nameof(stats));

            MaxHp = maxHp;
            Hp = maxHp;
            this.path = path;
            Position = path[0];
            targetIndex = 1;
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Dano não pode ser negativo.");

            if (IsDead) return;

            Hp = Math.Max(0, Hp - amount);
        }

        public void Move(float deltaTime)
        {
            if (IsDead || deltaTime <= 0f) return;

            float distance = Stats.speed * deltaTime;

            while (distance > 0f && !ReachedEnd)
            {
                Vector2 target = path[targetIndex];
                float toTarget = Vector2.Distance(Position, target);

                if (distance >= toTarget)
                {
                    Position = target;
                    distance -= toTarget;
                    targetIndex++;
                }
                else
                {
                    Position += Vector2.Normalize(target - Position) * distance;
                    distance = 0f;
                }
            }
        }
    }
}
