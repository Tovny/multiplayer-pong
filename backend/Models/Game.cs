using System.Collections.Concurrent;
using backend.Controllers;

namespace backend.Models;

public record GameData(double ballX, double ballY, int leftScore, int rightScore, double leftPaddleY, double rightPaddleY, string? winner);

public class Game
{
    public static ConcurrentDictionary<Guid, Game> ActiveGames = new ConcurrentDictionary<Guid, Game>();
    private static readonly int PaddleHeight = 30;
    private static readonly int BallRadius = 2;
    private readonly double PaddleWidth = 0.5;
    private readonly int MaxScore = 11;
    private readonly int yBound = 200;
    private double leftPaddleY = 50 - PaddleHeight / 2;
    private double rightPaddleY = 50 - PaddleHeight / 2;
    private double ballY = 50 - new Random().NextDouble() * 20;
    private double ballX = 50 - BallRadius / 2;
    private double vx = 2 * (new Random().NextDouble() < 0.5 ? 1 : -1);
    private double vy = 3 * (new Random().NextDouble() < 0.5 ? 1 : -1);
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
        while (winner == null && !gameOver)
        {
            await Task.Delay(30);
            var data = Update();
            WebsocketController.HandleGameUpdate(data, player1, player2);
        }
    }

    public GameData Update()
    {
        ballX += vx;
        ballY += vy;

        double leftPaddleStart = PaddleWidth;
        if (ballX < leftPaddleStart && IsBetweenPaddle(leftPaddleY))
        {
            ballX = leftPaddleStart;
            vx = -vx;
        }

        double rightPaddleStart = 100 - PaddleWidth;
        if (ballX > rightPaddleStart && IsBetweenPaddle(rightPaddleY))
        {
            ballX = rightPaddleStart;
            vx = -vx;
        }

        if (ballX < 0)
        {
            ballX = 0;
            vx = -vx;
            rightScore++;

            if (rightScore == MaxScore)
            {
                winner = "right";
            }
        }

        int rightBound = 100 - BallRadius;
        if (ballX >= rightBound)
        {
            ballX = rightBound;
            vx = -vx;
            leftScore++;

            if (leftScore == MaxScore)
            {
                winner = "left";
            }
        }

        if (ballY < 0)
        {
            ballY = 0;
            vy = -vy;
        }

        int bottomBound = yBound - BallRadius;
        if (ballY >= bottomBound)
        {
            ballY = bottomBound;
            vy = -vy;
        }

        return new GameData(ballX, ballY / 2, leftScore, rightScore, leftPaddleY, rightPaddleY, winner);
    }

    public void StopGame()
    {
        gameOver = true;
        ActiveGames.TryRemove(player1, out var oldGame1);
        ActiveGames.TryRemove(player2, out var oldGame2);
    }

    private bool IsBetweenPaddle(double paddleTop)
    {
        return ballY >= paddleTop && ballY <= paddleTop + PaddleHeight;
    }
}