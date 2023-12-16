using Godot;
using System;
using System.Threading.Tasks;

namespace MC
{
    public partial class GameStartInfo : GodotObject
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string PlayerName { get; set; }
        public uint Seed { get; set; }
    }

    public enum GameState
    {
        InMainMenu,
        ClientConnecting,
        ClientCantCreate,
        ClientTimeout,
        ClientDisconnected,
        ClientConnected_SyncingPlayer,
        ClientPlayerSynced_SyncingWorldSeed,
        ClientInitingWorld,
        InGameActive,
        InGamePaused,

    }

    public partial class GameManager : Node
    {
        [Export] UIManager _uiManager;
        [Export] Server _server;
        [Export] Client _client;
        [Export] World _world;
        Global _global;

        public override void _Ready()
        {
            _uiManager.HostGame += OnHostGame;
            _uiManager.JoinGame += OnJoinGame;

            _global = GetNode<Global>("/root/Global");

            GD.Randomize();

            _global.LocalPlayerSet += () =>
            {
                if (_global.GameState == GameState.ClientConnected_SyncingPlayer)
                    _global.GameState = GameState.ClientPlayerSynced_SyncingWorldSeed;
            };

            _global.SeedSet += (uint seed) =>
            {
                GD.Print("Seed set!");
                if (_global.GameState == GameState.ClientPlayerSynced_SyncingWorldSeed)
                    _global.GameState = GameState.ClientInitingWorld;
            };
        }

        async void OnHostGame(GameStartInfo info)
        {
            _global.GameStartInfo = info;

            GD.Print("On host game!");
            if (!_server.CreateServer(info.Port))
            {
                // do something
                return;
            }

            if (!await _world.Init())
                return;

            _global.LocalPlayer?.Init();
            _global.GameState = GameState.InGameActive;
        }

        async void OnJoinGame(GameStartInfo info)
        {
            _global.GameStartInfo = info;

            GD.Print("On join game!");

            if (!await _client.CreateClient(info.Address, info.Port) || 
                !await _global.WaitForNewGameState(GameState.ClientPlayerSynced_SyncingWorldSeed) ||
                !_client.SyncSeed() ||
                !await _global.WaitForNewGameState(GameState.ClientInitingWorld) ||
                !await _world.Init())
            {
                return;
            }

            _global.LocalPlayer?.Init();
            _global.GameState = GameState.InGameActive;
        }

    }
}
