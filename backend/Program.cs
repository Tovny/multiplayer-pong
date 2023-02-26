var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();
app.UseWebSockets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Websocket}/{action=Index}/{id?}");

app.Run();
