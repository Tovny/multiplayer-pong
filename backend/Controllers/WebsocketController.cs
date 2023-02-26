using Microsoft.AspNetCore.Mvc;
using backend.Models;
using System.Net.WebSockets;
using System.Net;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Text;

namespace backend.Controllers;

public class WebsocketController : Controller
{
    public static ConcurrentDictionary<System.Guid, WebSocket> Sockets = new ConcurrentDictionary<System.Guid, WebSocket>();
    private WebSocket? Socket;
    private Guid Uuid;
    private WebSocket? OpponentSocket;
    private Game? ActiveGame;

    public async Task Index()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            try
            {
                Uuid = System.Guid.NewGuid();
                Socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                Sockets.TryAdd(Uuid, Socket);

                var socketIds = Sockets.Select(socket => socket.Key);

                foreach (KeyValuePair<System.Guid, WebSocket> socket in Sockets)
                {
                    var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "playerUpdate", payload = socketIds.Where(s => s != socket.Key) });
                    await socket.Value.SendAsync(data, WebSocketMessageType.Text,
                        true, CancellationToken.None);
                }

                while (Socket.State == WebSocketState.Open)
                {
                    var buffer = WebSocket.CreateClientBuffer(1024, 16);
                    var msg = await Socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (msg.MessageType == WebSocketMessageType.Close)
                    {
                        await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        Sockets.TryRemove(Uuid, out var oldSocket);
                    }

                    await HandleMessage(buffer, msg.Count);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
        }
        else
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }

    private async Task HandleMessage(ArraySegment<byte> buffer, int count)
    {
        using (var stream = new MemoryStream())
        {
            try
            {
                stream.Write(buffer.Array, buffer.Offset, count);
                var msgString = Encoding.UTF8.GetString(stream.ToArray());
                var decoded = JsonSerializer.Deserialize<Payload>(msgString);

                switch (decoded?.action)
                {
                    case "gameRequest":
                        await HandleGameRequest(decoded.payload);
                        break;
                    case "acceptGameRequest":
                        await HandleAcceptGameRequest(decoded.payload);
                        break;
                    case "leftPaddleChange":
                        Game.ActiveGames[Uuid].UpdatePaddle("left", decoded.paddle);
                        break;
                    case "rightPaddleChange":
                        Game.ActiveGames[Uuid].UpdatePaddle("", decoded.paddle);
                        break;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
        }
    }

    private async Task HandleGameRequest(string payload)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "gameRequest", payload = Uuid.ToString() });
        await Sockets[new Guid(payload)].SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task HandleAcceptGameRequest(string payload)
    {
        OpponentSocket = Sockets[new Guid(payload)];
        ActiveGame = new Game(Uuid, new Guid(payload));

        var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "startGame" });
        await OpponentSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
        await Socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public static async void HandleGameUpdate(GameData gameData, Guid player1, Guid player2)
    {
        try
        {
            var socket = Sockets[player1];
            var opponentSocket = Sockets[player2];
            if (socket?.State == WebSocketState.Open && opponentSocket?.State == WebSocketState.Open)
            {
                var data = JsonSerializer.SerializeToUtf8Bytes(gameData);
                await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                await opponentSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception err)
        {
            Game.ActiveGames[player1].StopGame();
            Console.WriteLine(err);
        }
    }
}
