using System.Collections.Concurrent;
using backend.Controllers;

namespace backend.Models;

public record GameData(double ballX, double ballY, int leftScore, int rightScore, double leftPaddleY, double rightPaddleY, string? winner);

public class Game
{
    public static ConcurrentDictionary<Guid, Game> ActiveGames = new ConcurrentDictionary<Guid, Game>();
    private static readonly int PADDLE_HEIGHT = 15;
    private static readonly int BALL_RADIUS = 2;
    private readonly double PADDLE_WIDTH = 0.5;
    private readonly int MAX_SCORE = 11;
    private double ballY = 50 - new Random().NextDouble() * 20;
    private double ballX = 50 - BALL_RADIUS / 2;
    private double vx = 2 * (new Random().NextDouble() < 0.5 ? 1 : -1);
    private double vy = 3 * (new Random().NextDouble() < 0.5 ? 1 : -1);
    private int leftScore = 0;
    private int rightScore = 0;
    private double leftPaddleY = 50 - PADDLE_HEIGHT / 2;
    private double rightPaddleY = 50 - PADDLE_HEIGHT / 2;
    private string? winner;
    private int yBound = 200;
    private Guid player1;
    private Guid player2;
    private bool gameStopped = false;

    public Game(Guid player1, Guid player2)
    {
        this.player1 = player1;
        this.player2 = player2;
        this.StartGame();
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

    private async void StartGame()
    {

        ActiveGames.TryAdd(player1, this);
        ActiveGames.TryAdd(player2, this);
        while (this.leftScore < this.MAX_SCORE &&
        this.rightScore < this.MAX_SCORE && !gameStopped
        )
        {
            await Task.Delay(30);
            var data = Update();
            WebsocketController.HandleGameUpdate(data, player1, player2);
        }

    }
    public GameData Update()
    {
        ballX = ballX + vx;
        ballY = ballY + vy;

        double leftPaddleStart = PADDLE_WIDTH;
        if (ballX <= leftPaddleStart)
        {
            bool betweenLeftPaddle = BetweenPoints(
            ballY,
            leftPaddleY,
            leftPaddleY + PADDLE_HEIGHT
            );
            if (betweenLeftPaddle)
            {
                ballX = leftPaddleStart;
                vx = -vx;
            }
        }

        double rightPaddleStart = 100 - PADDLE_WIDTH;
        if (ballX >= rightPaddleStart)
        {
            bool betweenRightPaddle = BetweenPoints(
            ballX,
            rightPaddleY,
            rightPaddleY + PADDLE_HEIGHT
            );
            if (betweenRightPaddle)
            {
                ballX = rightPaddleStart;
                vx = -vx;
            }
        }

        if (ballX <= 0)
        {
            ballX = 0;
            vx = -vx;

            rightScore = rightScore + 1;
            if (rightScore >= MAX_SCORE)
            {
                winner = "right";
            }
        }

        int rightBound = 100 - BALL_RADIUS;

        if (ballX >= rightBound)
        {
            ballX = rightBound;
            vx = -vx;

            leftScore = leftScore + 1;
            if (leftScore >= 11)
            {
                winner = "left";
            }
        }

        if (ballY <= 0)
        {
            ballY = 0;
            vy = -vy;
        }

        int bottomBound = yBound - BALL_RADIUS;

        if (ballY >= bottomBound)
        {
            ballY = bottomBound;
            vy = -vy;
        }

        return GenerateData();
    }

    public void StopGame()
    {
        gameStopped = true;
        ActiveGames.TryRemove(player1, out var oldGame1);
        ActiveGames.TryRemove(player2, out var oldGame2);
    }

    private GameData GenerateData()
    {
        return new GameData(ballX, ballY / 2, leftScore, rightScore, leftPaddleY, rightPaddleY, winner);
    }

    private bool BetweenPoints(double y, double top, double bottom)
    {
        if (y < top || y > bottom)
        {
            return false;
        }
        return true;
    }
}