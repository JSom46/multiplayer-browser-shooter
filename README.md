# BrowserMultiplayerShooter


## About
Simple multiplayer realtime 2D shooter with browser client.


## User's manual
### Running the application
Clone this repository, and inside it's root directory run  
`dotnet run --project .\BrowserGameBackend\BrowserGame.csproj`  

### Adding new maps
To add new map, paste it and its tilemap to ./BrowserGameBackend/wwwroot/assets/maps.  
Tilemap should be of PNG format, and be named identically as map file.  
  
Both client and server only support maps made in Tiled, exported to json format, with embedded tileset.  
  
Name of map displayed for players will be the same as name of its files  
  
For collision detection, boolean property 'collides' is used.  
Collision detection for players is checked against 'Obstacles' and 'Terrain' layers and for projectiles against 'Obstacles' layer.


## Technology
### Server
.NET 6.0  
SignalR for WebSocket communication  

### Client
HTML, CSS & plain javascript  
PIXI.js for rendering game with WebGL  


## Credits
Tilemap used to create sample maps has been created by Kenney Vleugels and published under Creative Commons Zero license
