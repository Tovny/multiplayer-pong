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
    private WebSocket? socket;
    private Guid uuid = Guid.NewGuid();
    private WebSocket? opponentSocket;
    private Game? activeGame;

    public async Task Index()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            try
            {
                socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                Sockets.TryAdd(uuid, socket);

                var socketIds = Sockets.Select(socket => socket.Key);

                foreach (KeyValuePair<Guid, WebSocket> socket in Sockets)
                {
                    var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "playerUpdate", payload = socketIds.Where(s => s != socket.Key) });
                    await socket.Value.SendAsync(data, WebSocketMessageType.Text,
                        true, CancellationToken.None);
                }

                while (socket.State == WebSocketState.Open)
                {
                    var buffer = WebSocket.CreateClientBuffer(1024, 16);
                    var msg = await socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (msg.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        Sockets.TryRemove(uuid, out var oldSocket);
                    }

                    await HandleMessage(buffer, msg.Count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        else
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }

    private async Task HandleMessage(ArraySegment<byte> buffer, int count)
    {
        if (buffer.Array != null)
        {
            try
            {
                using (var stream = new MemoryStream())
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
                            Game.ActiveGames[uuid].UpdatePaddle("left", decoded.paddle);
                            break;
                        case "rightPaddleChange":
                            Game.ActiveGames[uuid].UpdatePaddle("", decoded.paddle);
                            break;
                    }
                }
            }


            catch (Exception ex)
            {
                var jsonEx = ex.InnerException is JsonException;
                if (!jsonEx)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }

    private async Task HandleGameRequest(string payload)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "gameRequest", payload = uuid.ToString() });
        await Sockets[new Guid(payload)].SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task HandleAcceptGameRequest(string payload)
    {
        opponentSocket = Sockets[new Guid(payload)];
        activeGame = new Game(uuid, new Guid(payload));

        var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "startGame" });
        if (socket != null)
        {
            await opponentSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public static async void HandleGameUpdate(GameData gameData, Guid player1, Guid player2)
    {
        try
        {
            if (!Sockets.ContainsKey(player1) || !Sockets.ContainsKey(player2))
            {
                Game.ActiveGames[player1].StopGame();
                return;
            }

            var socket = Sockets[player1];
            var opponentSocket = Sockets[player2];
            if (socket?.State == WebSocketState.Open && opponentSocket?.State == WebSocketState.Open)
            {
                var data = JsonSerializer.SerializeToUtf8Bytes(gameData);
                await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                await opponentSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
