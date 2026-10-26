using NUnit.Framework;
using TowerDefense;
using System;
using System.Numerics;

[TestFixture]
public class EnemyTests
{
    private Vector2[] defaultPath;

    [SetUp]
    public void Setup()
    {
        defaultPath = new Vector2[] { new Vector2(0, 0), new Vector2(1, 1) };
    }

    [Test]
    public void TakeDamage_ReduzHpCorretamente()
    {
        var enemy = new Enemy(10, defaultPath);

        enemy.TakeDamage(3);

        Assert.AreEqual(7, enemy.Hp, "O HP atual não foi reduzido corretamente após o dano.");
    }

    [Test]
    public void TakeDamage_HpNaoCaiAbaixoDeZero()
    {
        var enemy = new Enemy(5, defaultPath);

        enemy.TakeDamage(10);

        Assert.AreEqual(0, enemy.Hp, "O HP do inimigo não pode ser negativo.");
        Assert.IsTrue(enemy.IsDead, "O inimigo deveria ser considerado morto.");
    }

    [Test]
    public void TakeDamage_DanoNegativoGeraExcecao()
    {
        var enemy = new Enemy(10, defaultPath);

        Assert.Throws<ArgumentOutOfRangeException>(() => enemy.TakeDamage(-5), "Aplicar dano negativo deve gerar uma exceção.");
    }
}