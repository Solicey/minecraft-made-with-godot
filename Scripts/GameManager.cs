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

            if (!await _world.Create())
                return;

            _global.LocalPlayer?.Init();
        }

        async void OnJoinGame(GameStartInfo info)
        {
            _global.GameStartInfo = info;

            GD.Print("On join game!");

            if (!await _client.CreateClient(info.Address, info.Port) || 
                !await _client.WaitForNewClientState(ClientState.PlayerSynced_SyncingWorldSeed) ||
                !await _client.SyncSeed() ||
                !await _world.Create())
            {
                return;
            }

            _global.LocalPlayer?.Init();
        }

    }
}
