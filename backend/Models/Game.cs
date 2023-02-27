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
        Reset();
        while (winner == null && !gameOver)
        {
            Update();
            var data = new GameData(ballX, ballY, leftScore, rightScore, leftPaddleY, rightPaddleY, winner);
            WebsocketController.HandleGameUpdate(data, player1, player2);
            await Task.Delay(tickDelay);
        }
    }

    private void Update()
    {
        if (leftScore == MaxScore)
        {
            winner = "left";
            return;
        }
        else if (rightScore == MaxScore)
        {
            winner = "right";
            return;
        }

        ballX += vx;
        ballY += vy;

        if (ballY > 100)
        {
            vy *= -1.025;
            vx *= 1.025;
            ballY = 100 - BallRadius / 2;
        }
        else if (ballY < 0)
        {
            vy *= -1.025;
            vx *= 1.025;
            ballY = 0 + BallRadius / 2;
        }
        else if (ballX + BallRadius / 2 > 100)
        {
            ballX = 100 - BallRadius / 2;
            leftScore++;
            Reset();
        }
        else if (ballX - BallRadius / 2 < 0)
        {
            ballX = 0 + BallRadius / 2;
            rightScore++;
            Reset();
        }
        else if (ballX - BallRadius / 2 < PaddleWidth && ballY > leftPaddleY && ballY < leftPaddleY + PaddleHeight)
        {
            vx *= -1;
            ballX = PaddleWidth;

        }
        else if (ballX + BallRadius / 2 > 100 - PaddleWidth && ballY > rightPaddleY && ballY < rightPaddleY + PaddleHeight)
        {
            vx *= -1;
            ballX = 100 - PaddleWidth;
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
        Task.Delay(tickDelay).ContinueWith((_) => { tickDelay = 30; });
    }

    public void StopGame()
    {
        gameOver = true;
        ActiveGames.TryRemove(player1, out var oldGame1);
        ActiveGames.TryRemove(player2, out var oldGame2);
    }
}