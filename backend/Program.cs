using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

System.Collections.Concurrent.ConcurrentDictionary<System.Guid, WebSocket> sockets = new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, WebSocket>();

app.UseWebSockets();
app.Map("/", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var uuid = System.Guid.NewGuid();
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var rand = new Random();

        sockets.TryAdd(uuid, webSocket);

        var socketIds = sockets.ToArray().Select(socket => socket.Key);

        foreach (System.Collections.Generic.KeyValuePair<System.Guid, System.Net.WebSockets.WebSocket> socket in sockets.ToArray())
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "playerUpdate", payload = socketIds });
            await socket.Value.SendAsync(data, WebSocketMessageType.Text,
                true, CancellationToken.None);
        }


        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = WebSocket.CreateClientBuffer(1024, 16);
            var msg = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            if (msg.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                sockets.TryRemove(uuid, out var oldSocket);
            }

            using (var stream = new MemoryStream())
            {
                stream.Write(buffer.Array, buffer.Offset, msg.Count);
                var msgString = Encoding.UTF8.GetString(stream.ToArray());
                var decoded = JsonSerializer.Deserialize<IFFF>(msgString);

                if (decoded?.action == "gameRequest" && decoded.payload != null)
                {
                    var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "gameRequest", payload = uuid.ToString() });
                    await sockets[new Guid(decoded.payload)].SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                }

                if (decoded?.action == "acceptGameRequest" && decoded.payload != null)
                {
                    var opponentSocket = sockets[new Guid(decoded.payload)];
                    Game.Game game = new Game.Game(uuid.ToString(), decoded.payload, async void (Game.GameData gameData) =>
                    {
                        var data = JsonSerializer.SerializeToUtf8Bytes(gameData);
                        await webSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                        await opponentSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                    });

                    var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "startGame" });
                    await opponentSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                    await webSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                }

                if (decoded?.action == "leftPaddleChange")
                {
                    Game.ActiveGames.games[uuid.ToString()].UpdateLeftPaddle(decoded.paddle);
                }

                if (decoded?.action == "rightPaddleChange")
                {
                    Game.ActiveGames.games[uuid.ToString()].UpdateRightPaddle(decoded.paddle);
                }
            }
        }

    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

app.Run("http://localhost:5282");

class IFFF
{
    public string action { get; set; }
    public string payload { get; set; }
    public double paddle { get; set; }
}