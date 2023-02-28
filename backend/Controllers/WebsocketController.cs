using Microsoft.AspNetCore.Mvc;
using backend.Models;
using System.Net.WebSockets;
using System.Net;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Text;
using System.Globalization;

namespace backend.Controllers;

public class WebsocketController : Controller
{
    public static ConcurrentDictionary<string, WebSocket> Sockets = new ConcurrentDictionary<string, WebSocket>();
    private string username = string.Empty;
    private WebSocket? socket;
    private WebSocket? opponentSocket;
    private Game? activeGame;

    [Route("/{username}")]
    public async Task Index(string username)
    {
        var duplicateUsername = Sockets.ContainsKey(username);
        if (HttpContext.WebSockets.IsWebSocketRequest && !duplicateUsername)
        {
            try
            {
                this.username = username;
                socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Sockets.TryAdd(username, socket);

                await BroadcastPlayers();

                while (socket.State == WebSocketState.Open)
                {
                    var buffer = WebSocket.CreateClientBuffer(1024, 16);
                    var msg = await socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (msg.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseConnection();
                    }

                    await HandleMessage(buffer, msg.Count);
                }
            }
            catch (Exception ex)
            {
                await CloseConnection();
                Console.WriteLine(ex);
            }
        }
        else
        {
            if (duplicateUsername)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }

    private static async Task BroadcastPlayers()
    {
        try
        {
            var socketKeys = Sockets.Select(socket => socket.Key).Where(key => !Game.ActiveGames.ContainsKey(key));
            foreach (string username in socketKeys)
            {
                var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "playerUpdate", payload = socketKeys.Where(key => key != username) });
                await Sockets[username].SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task CloseConnection()
    {
        try
        {
            if (socket != null)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                Sockets.TryRemove(username, out var oldSocket);
            }
            if (Game.ActiveGames.ContainsKey(username))
            {
                Game.ActiveGames[username].StopGame();
            }
            await BroadcastPlayers();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
                            await BroadcastPlayers();
                            break;
                        // payload is a string representation of a float in the next two cases
                        case "leftPaddleChange":
                            Game.ActiveGames[username].UpdatePaddle("left", float.Parse(decoded.payload, CultureInfo.InvariantCulture));
                            break;
                        case "rightPaddleChange":
                            Game.ActiveGames[username].UpdatePaddle("right", float.Parse(decoded.payload, CultureInfo.InvariantCulture));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex.InnerException is JsonException))
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }

    private async Task HandleGameRequest(string payload)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "gameRequest", payload = username });
        await Sockets[payload].SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task HandleAcceptGameRequest(string payload)
    {
        opponentSocket = Sockets[payload];
        activeGame = new Game(username, payload);

        var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "startGame" });
        if (socket != null)
        {
            await opponentSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public static async void HandleGameUpdate(string player1, string player2, GameData gameData, bool gameOver)
    {
        foreach (string player in new[] { player1, player2 })
        {
            try
            {
                if (!Sockets.ContainsKey(player) && Game.ActiveGames.ContainsKey(player))
                {
                    Game.ActiveGames[player].StopGame();
                    return;
                }
                var socket = Sockets[player];
                if (socket?.State == WebSocketState.Open)
                {
                    var data = JsonSerializer.SerializeToUtf8Bytes(new { action = "gameUpdate", payload = gameData });
                    await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        if (gameOver)
        {
            await Task.Delay(1000);
            await BroadcastPlayers();
        }
    }
}
