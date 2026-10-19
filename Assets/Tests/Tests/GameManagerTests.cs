using NUnit.Framework;
using TowerDefense;
using System;

[TestFixture]
public class GameManagerTests
{
    private GameSettings defaultSettings;

    [SetUp]
    public void Setup()
    {
        defaultSettings = new GameSettings { startGold = 100 };
    }

    [Test]
    public void TrySpend_DeduzOuroCorretamente()
    {
        // Arrange
        var game = new GameManager(defaultSettings);

        // Act
        bool result = game.TrySpend(40);

        // Assert: Verifica se retornou true e se o saldo atualizou
        Assert.IsTrue(result, "A transação deveria ser aprovada.");
        Assert.AreEqual(60, game.Gold, "O saldo não foi deduzido corretamente.");
    }

    [Test]
    public void TrySpend_FalhaAoTentarGastarMaisQueOSaldo()
    {
        // Arrange
        var game = new GameManager(defaultSettings);

        // Act: Tentar gastar 150 tendo apenas 100 (Edge Case)
        bool result = game.TrySpend(150);

        // Assert: Verifica se a compra foi bloqueada e o saldo mantido
        Assert.IsFalse(result, "A transação deveria ser recusada por falta de saldo.");
        Assert.AreEqual(100, game.Gold, "O saldo não deveria ser alterado em uma transação falha.");
    }

    [Test]
    public void TrySpend_ValoresNegativosGeramExcecao()
    {
        // Arrange
        var game = new GameManager(defaultSettings);

        // Act & Assert: Tentar subverter a economia passando valor negativo (Edge Case)
        Assert.Throws<ArgumentOutOfRangeException>(() => game.TrySpend(-20), "Tentar gastar um valor negativo deve gerar uma exceção de segurança.");
    }
}