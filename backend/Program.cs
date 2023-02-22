using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;
using MongoDB.Driver;
using MongoDB.Bson;


var wss = new WebSocketServer(5282);
wss.AddWebSocketService<Echo>("/");
wss.Start();
var db = new Database.DB();
await db.FindOrCreateUser("ka je");
Console.ReadKey(true);
wss.Stop();




class IFFF
{
    public string action;
    public string payload;
    public double paddle;
}


class Echo : WebSocketBehavior
{



    private string? opponent;

    protected override void OnOpen()
    {
        base.OnOpen();
        BroadcastPlayers();
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        var msg = JsonConvert.DeserializeObject<IFFF>(e.Data);
        if (msg.action == "gameRequest" && msg.payload != null)
        {
            Sessions.SendTo(JsonConvert.SerializeObject(new { action = "gameRequest", payload = ID }), msg.payload);
        }
        if (msg.action == "acceptGameRequest" && msg.payload != null)
        {


            opponent = msg.payload;
            Game.Game game = new Game.Game(ID, msg.payload, UpdateGame);

            Sessions.SendTo(JsonConvert.SerializeObject(new { action = "startGame" }), msg.payload);
            Send(JsonConvert.SerializeObject(new { action = "startGame" }));
        }
        if (msg.action == "leftPaddleChange")
        {
            Game.ActiveGames.games[ID].UpdateLeftPaddle(msg.paddle);
        }
        if (msg.action == "rightPaddleChange")
        {
            Game.ActiveGames.games[ID].UpdateRightPaddle(msg.paddle);
        }
    }

    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        BroadcastPlayers();
        // Game.ActiveGames.games[ID].
    }

    private void BroadcastPlayers()
    {
        Sessions.Broadcast(JsonConvert.SerializeObject(new { action = "playerUpdate", payload = Sessions.ActiveIDs }));
    }

    private void UpdateGame(GameData data)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        var jsonText = JsonConvert.SerializeObject(data);
        Send(jsonText);
        Sessions.SendTo(jsonText, opponent);
    }
}

class IPayload
{
    public double ballX;


}

class Iffs
{
    public string action { get; set; }
    public IPayload payload { get; set; }
    public Guid? id { get; set; }
}

class GameData
{
    public double ballX { get; set; }
    public double ballY { get; set; }
    public int leftScore { get; set; }
    public int rightScore { get; set; }
    public double leftPaddleY { get; set; }
    public double rightPaddleY { get; set; }

    public GameData(double ballX, double ballY, int leftScore, int rightScore, double leftPaddleY, double rightPaddleY)
    {
        this.ballX = ballX;
        this.ballY = ballY;
        this.leftScore = leftScore;
        this.rightScore = rightScore;
        this.leftPaddleY = leftPaddleY;
        this.rightPaddleY = rightPaddleY;
    }
}


class SendId
{
    public Guid id { get; set; }
    public string type = "id";
    public SendId(Guid id)
    {
        this.id = id;
    }
}