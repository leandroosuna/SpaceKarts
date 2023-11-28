using Microsoft.Extensions.Configuration;
var fileCfgApp = "CFG/app-settings.json";
var cfgApp = new ConfigurationBuilder().AddJsonFile(fileCfgApp, false, true).Build();
using var game = new SpaceKarts.SpaceKarts(cfgApp);
game.Run();
