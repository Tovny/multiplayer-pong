

namespace Game
{
    class ActiveGames
    {
        public static System.Collections.Concurrent.ConcurrentDictionary<string, Game> games = new System.Collections.Concurrent.ConcurrentDictionary<string, Game>();

        public static void stopGame(string id)
        {
            games[id].Stop();
        }
    }

    class Game
    {
        private static int PADDLE_HEIGHT = 15;
        private static int BALL_RADIUS = 2;
        private double PADDLE_WIDTH = 0.5;
        private int MAX_SCORE = 11;
        private double ballY = 50 - new Random().NextDouble() * 20;
        private double ballX = 50 - BALL_RADIUS / 2;
        private double vx = 2 * (new Random().NextDouble() < 0.5 ? 1 : -1);
        private double vy = 3 * (new Random().NextDouble() < 0.5 ? 1 : -1);
        private int leftScore = 0;
        private int rightScore = 0;
        private double leftPaddleY = 50 - PADDLE_HEIGHT / 2;
        private double rightPaddleY = 50 - PADDLE_HEIGHT / 2;
        public string? winner;
        private int yBound = 200;
        private Action<GameData> updateCallback;

        public Game(string player1, string player2, Action<GameData> updateCallback)
        {
            this.updateCallback = updateCallback;
            this.StartGame();
            ActiveGames.games.TryAdd(player1, this);
            ActiveGames.games.TryAdd(player2, this);
        }

        public void UpdateLeftPaddle(double change)
        {
            leftPaddleY = leftPaddleY + change;
        }

        public void UpdateRightPaddle(double change)
        {
            rightPaddleY = rightPaddleY + change;
        }

        private async void StartGame()
        {
            while (this.leftScore < this.MAX_SCORE &&
            this.rightScore < this.MAX_SCORE //&&
            // sessions.ActiveIDs.Contains(player1) &&
            // sessions.ActiveIDs.Contains(player2))
            )
            {

                await Task.Delay(30);
                var data = this.update();
                updateCallback(data);



            }

        }
        public GameData update()
        {
            ballX = ballX + vx;
            ballY = ballY + vy;

            double leftPaddleStart = PADDLE_WIDTH;
            if (ballX <= leftPaddleStart)
            {
                bool betweenLeftPaddle = betweenPoints(
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
                bool betweenRightPaddle = betweenPoints(
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

            return generateData();
        }

        public void Stop()
        {

        }

        private GameData generateData()
        {
            return new GameData(ballX, ballY / 2, leftScore, rightScore, leftPaddleY, rightPaddleY);
        }

        private bool betweenPoints(double y, double top, double bottom)
        {
            if (y < top || y > bottom)
            {
                return false;
            }
            return true;
        }
    }
}