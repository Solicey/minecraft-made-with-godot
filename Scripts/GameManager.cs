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
        GameVariables _variables;

        public override void _Ready()
        {
            _uiManager.HostGame += OnHostGame;
            _uiManager.JoinGame += OnJoinGame;

            _variables = GetNode<GameVariables>("/root/GameVariables");

            GD.Randomize();
        }

        async void OnHostGame(GameStartInfo info)
        {
            _variables.GameStartInfo = info;

            GD.Print("On host game!");
            if (!_server.CreateServer(info.Port))
            {
                // do something
                return;
            }

            if (!await _world.Create())
                return;

            // GD.Print("Reach here!");

            _uiManager.Visible = false;
        }

        async void OnJoinGame(GameStartInfo info)
        {
            _variables.GameStartInfo = info;

            GD.Print("On join game!");

            if (!await _client.CreateClient(info.Address, info.Port) || 
                !await _client.WaitForNewClientState(ClientState.PlayerSynced_SyncingWorldSeed) ||
                !await _client.SyncSeed() ||
                !await _world.Create())
            {
                return;
            }

            _variables.ClientLatestState = ClientState.WorldCreated;
            _uiManager.Visible = false;
        }

    }
}
