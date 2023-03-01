using System.Collections.Concurrent;
using backend.Controllers;
using System.Reflection;

namespace backend.Models;

public record GameData(
    double ballX,
    double ballY,
    int leftScore,
    int rightScore,
    double leftPaddleY,
    double rightPaddleY,
    string player1,
    string player2,
    string? winner,
    bool? cancelled
);

public enum Paddles
{
    Left,
    Right
}

public class Game
{
    public static ConcurrentDictionary<string, Game> ActiveGames = new ConcurrentDictionary<string, Game>();
    private static readonly int PaddleHeight = 15;
    private static readonly int BallRadius = 2;
    private readonly double PaddleWidth = 0.5;
    private readonly int MaxScore = 11;
    private readonly double vMultiplier = 1.1;
    private int tickDelay;
    private double leftPaddleY { get; set; }
    private double rightPaddleY { get; set; }
    private double ballX;
    private double ballY;
    private double vx;
    private double vy;
    private int leftScore = 0;
    private int rightScore = 0;
    private string player1;
    private string player2;
    private string? winner;
    private bool active;
    private bool gameCancelled = false;

    public Game(string player1, string player2)
    {
        this.player1 = player1;
        this.player2 = player2;
        ActiveGames.TryAdd(player1, this);
        ActiveGames.TryAdd(player2, this);
        Reset();
        Tick();
    }

    public void UpdatePaddle(Paddles paddle, double change)
    {
        if (active)
        {
            var selectedPaddle = this.GetType().GetProperty(
                paddle == Paddles.Left ? "leftPaddleY" : "rightPaddleY",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var currValue = (double?)selectedPaddle?.GetValue(this);
            var newVal = currValue + change;
            if (newVal < 0)
            {
                selectedPaddle?.SetValue(this, 0);
            }
            else if (newVal > 100 - PaddleHeight)
            {
                selectedPaddle?.SetValue(this, 100 - PaddleHeight);
            }
            else
            {
                selectedPaddle?.SetValue(this, newVal);
            }
        }
    }

    private async void Tick()
    {
        while (true)
        {
            var data = new GameData(ballX, ballY, leftScore, rightScore, leftPaddleY, rightPaddleY, player1, player2, winner, gameCancelled);
            WebsocketController.HandleGameUpdate(player1, player2, data, data.winner != null || gameCancelled);
            if (winner != null || gameCancelled)
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
            vy *= -vMultiplier;
            vx *= vMultiplier;
            ballY = 100 - BallRadius;
        }
        else if (ballY < 0)
        {
            vy *= -vMultiplier;
            vx *= vMultiplier;
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
        active = false;
        Task.Delay(100).ContinueWith((_) => { tickDelay = 1000 / 45; });
        Task.Delay(500).ContinueWith((_) => { active = true; });
    }

    private bool IsBetweenPaddle(double paddleY)
    {
        return ballY > paddleY && ballY < paddleY + PaddleHeight;
    }

    public void StopGame()
    {
        gameCancelled = true;
        ActiveGames.TryRemove(player1, out var oldGame1);
        ActiveGames.TryRemove(player2, out var oldGame2);
    }
}