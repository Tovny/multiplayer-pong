using System.Collections.Concurrent;
using backend.Controllers;

namespace backend.Models;

public record GameData(double ballX, double ballY, int leftScore, int rightScore, double leftPaddleY, double rightPaddleY, string? winner);

public class Game
{
    public static ConcurrentDictionary<Guid, Game> ActiveGames = new ConcurrentDictionary<Guid, Game>();
    private static readonly int PaddleHeight = 15;
    private static readonly int BallRadius = 2;
    private readonly double PaddleWidth = 0.5;
    private readonly int MaxScore = 11;
    private int tickDelay;
    private double leftPaddleY;
    private double rightPaddleY;
    private double ballY;
    private double ballX;
    private double vx;
    private double vy;
    private int leftScore = 0;
    private int rightScore = 0;
    private string? winner;
    private Guid player1;
    private Guid player2;
    private bool gameOver = false;

    public Game(Guid player1, Guid player2)
    {
        this.player1 = player1;
        this.player2 = player2;
        ActiveGames.TryAdd(player1, this);
        ActiveGames.TryAdd(player2, this);
        Reset();
        Tick();
    }

    public void UpdatePaddle(string side, double change)
    {
        if (side == "left")
        {
            leftPaddleY = leftPaddleY + change;
        }
        else if (side == "right")
        {
            rightPaddleY = rightPaddleY + change;
        }
    }

    private async void Tick()
    {
        while (!gameOver)
        {
            var data = new GameData(ballX, ballY, leftScore, rightScore, leftPaddleY, rightPaddleY, winner);
            WebsocketController.HandleGameUpdate(player1, player2, data, data.winner != null);
            if (winner != null)
            {
                StopGame();
                break;
            }
            await Task.Delay(tickDelay);
            Update();
        }
    }

    private void Update()
    {
        if (Math.Max(leftScore, rightScore) == MaxScore)
        {
            winner = leftScore == MaxScore ? "left" : "right";
        }
        else if (ballX < PaddleWidth && IsBetweenPaddle(leftPaddleY))
        {
            vx *= -1;
            ballX = PaddleWidth;
        }
        else if (ballX + BallRadius > 100 - PaddleWidth && IsBetweenPaddle(rightPaddleY))
        {
            vx *= -1;
            ballX = 100 - PaddleWidth - BallRadius;
        }
        else if (ballY + BallRadius > 100)
        {
            vy *= -1.025;
            vx *= 1.025;
            ballY = 100 - BallRadius;
        }
        else if (ballY < 0)
        {
            vy *= -1.025;
            vx *= 1.025;
            ballY = 0;
        }
        else if (ballX + BallRadius > 100)
        {
            leftScore++;
            Reset();
        }
        else if (ballX < 0)
        {
            rightScore++;
            Reset();
        }
        else
        {
            ballX += vx;
            ballY += vy;
        }
    }

    private void Reset()
    {
        leftPaddleY = 50 - PaddleHeight / 2;
        rightPaddleY = 50 - PaddleHeight / 2;
        ballY = 50 - BallRadius / 2;
        ballX = 50 - BallRadius / 2;
        vx = 0.5 * (new Random().NextDouble() < 0.5 ? 1 : -1);
        vy = 0.75 * (new Random().NextDouble() < 0.5 ? 1 : -1);
        tickDelay = 500;
        Task.Delay(100).ContinueWith((_) => { tickDelay = 30; });
    }

    private bool IsBetweenPaddle(double paddleY)
    {
        return ballY > paddleY && ballY < paddleY + PaddleHeight;
    }

    public void StopGame()
    {
        gameOver = true;
        ActiveGames.TryRemove(player1, out var oldGame1);
        ActiveGames.TryRemove(player2, out var oldGame2);
    }
}