using BrowserGame;
using BrowserGame.Data;
using BrowserGame.Hubs;
using BrowserGame.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddSignalR() /*.AddMessagePackProtocol()*/;

builder.Services.AddSingleton<IMapData, MapData>();

builder.Services.AddSingleton<IGameData, GameData>();

builder.Services.AddSingleton<IPlayerPositioner, PlayerPositioner>();

builder.Services.AddHostedService<GameUpdateService>();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.MapHub<GameHub>("/gamehub");

app.Run();