using NUnit.Framework;
using TowerDefense;
using System.Collections.Generic;
using System.Numerics;

[TestFixture]
public class TowerTests
{
    private Vector2[] pathInCoverage;
    private Vector2[] pathOutOfRange;

    [SetUp]
    public void Setup()
    {
        pathInCoverage = new Vector2[] { new Vector2(1, 0), new Vector2(2, 0) };
        pathOutOfRange = new Vector2[] { new Vector2(10, 10), new Vector2(11, 11) };
    }

    [Test]
    public void FindTarget_RetornaInimigoMaisProximo()
    {
        // Arrange
        var stats = new TowerStats { range = 5f };
        var tower = new Tower(0, 0, stats);

        var enemyDist2 = new Enemy(10, new Vector2[] { new Vector2(2, 0), new Vector2(3, 0) });
        var enemyDist1 = new Enemy(10, new Vector2[] { new Vector2(1, 0), new Vector2(2, 0) });

        var enemies = new List<Enemy> { enemyDist2, enemyDist1 };

        // Act
        Enemy target = tower.FindTarget(enemies);

        // Assert
        Assert.AreEqual(enemyDist1, target, "A torre deve selecionar o inimigo que está mais próximo.");
    }

    [Test]
    public void FindTarget_RetornaNullQuandoInimigosForaDoAlcance()
    {
        // Arrange
        var stats = new TowerStats { range = 2.5f };
        var tower = new Tower(0, 0, stats);
        var farEnemy = new Enemy(10, pathOutOfRange);

        var enemies = new List<Enemy> { farEnemy };

        // Act
        Enemy target = tower.FindTarget(enemies);

        // Assert
        Assert.IsNull(target, "A torre não deve selecionar alvos fora da sua área de alcance.");
    }

    [Test]
    public void Shoot_RespeitaTempoDeRecarga()
    {
        // Arrange
        var stats = new TowerStats { baseDamage = 5, cooldown = 1.0f, critChance = 0f };
        var tower = new Tower(0, 0, stats, critRoll: () => 1.0);
        var enemy = new Enemy(10, pathInCoverage);

        // Act 1
        int firstDamage = tower.Shoot(enemy);

        // Act 2
        int secondDamage = tower.Shoot(enemy);

        // Assert
        Assert.AreEqual(5, firstDamage, "O primeiro disparo deve causar o dano base.");
        Assert.AreEqual(0, secondDamage, "Disparos efetuados durante o cooldown devem retornar 0 de dano.");
    }
}