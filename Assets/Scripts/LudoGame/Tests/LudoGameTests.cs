using System;
using LudoGame.Runtime;
using NUnit.Framework;

[TestFixture]
public class LudoGameTests
{
    private LudoGameState _state;

    [SetUp]
    public void Setup()
    {
        LudoGameState.TryCreate(4, out _state, out _);
    }

    [Test]
    public void TryProcessMove_MoveFromBaseSuccessfully()
    {
        _state.CurrentPlayer = 0;
        int diceRoll = 6;
        int tokenIndex = 0;

        bool success = LudoBoard.TryProcessMove(ref _state, tokenIndex, diceRoll, out MoveResult result);

        Console.WriteLine(_state);
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(MoveResult.SuccessSix));
        Assert.That(_state.TokenPositions[tokenIndex], Is.EqualTo(0));
    }

    [Test]
    public void TryProcessMove_FailToMoveFromBase_WithoutSix()
    {
        // Arrange
        _state.CurrentPlayer = 0;
        int diceRoll = 5;
        int tokenIndex = 0;

        // Act
        bool success = LudoBoard.TryProcessMove(ref _state, tokenIndex, diceRoll, out MoveResult result);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(MoveResult.InvalidNoValidMoves));
        Assert.That(_state.TokenPositions[tokenIndex], Is.EqualTo(LudoBoard.PosBase));
    }

    [Test]
    public void TryProcessMove_EvictOpponentToken()
    {
        _state.TokenPositions[0] = 5;
        _state.TokenPositions[4] = 8;
        _state.CurrentPlayer = 0;
        int diceRoll = 3;

        bool success = LudoBoard.TryProcessMove(ref _state, 0, diceRoll, out MoveResult result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(MoveResult.SuccessEvictedOpponent));
        Assert.That(_state.TokenPositions[0], Is.EqualTo(8));
        Assert.That(_state.TokenPositions[4], Is.EqualTo(LudoBoard.PosBase));
    }

    [Test]
    public void TryProcessMove_InvalidMove_BlockedByOwnToken()
    {
        _state.CurrentPlayer = 0;
        _state.TokenPositions[0] = 10;
        _state.TokenPositions[1] = 12;
        int diceRoll = 2;

        bool success = LudoBoard.TryProcessMove(ref _state, 0, diceRoll, out MoveResult result);

        Assert.That(success, Is.False);
        Assert.That(_state.TokenPositions[0], Is.EqualTo(10));
    }

    [Test]
    public void NextTurn_AdvancesPlayer_OnNormalMove()
    {
        _state.CurrentPlayer = 0;

        LudoBoard.TryNextTurn(ref _state, MoveResult.Success);

        Assert.That(_state.CurrentPlayer, Is.EqualTo(1));
    }

    [Test]
    public void NextTurn_DoesNotAdvancePlayer_OnRollAgain()
    {
        _state.CurrentPlayer = 1;

        LudoBoard.TryNextTurn(ref _state, MoveResult.SuccessSix);

        Assert.That(_state.CurrentPlayer, Is.EqualTo(1));
        Assert.That(_state.ConsecutiveSixes, Is.EqualTo(1));
    }

    [Test]
    public void HasPlayerWon_ReturnsTrue_WhenAllTokensAreFinished()
    {
        _state.CurrentPlayer = 2;
        for (int i = 8; i < 12; i++)
        {
            _state.TokenPositions[i] = LudoBoard.PosFinished;
        }

        bool player2Won = LudoBoard.HasPlayerWon(_state, 2);
        bool player0Won = LudoBoard.HasPlayerWon(_state, 0);

        Assert.That(player2Won, Is.True);
        Assert.That(player0Won, Is.False);
    }
}