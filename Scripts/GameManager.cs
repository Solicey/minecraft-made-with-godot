using Godot;
using System;

namespace MC
{
    public partial class GameStartInfo : GodotObject
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string PlayerName { get; set; }
        public int Seed { get; set; }

    }

    public partial class GameManager : Node
    {
        [Export] UIManager _uiManager;
        [Export] Server _server;
        [Export] Client _client;
        GameVariables _variables;

        public override void _Ready()
        {
            _uiManager.HostGame += OnHostGame;
            _uiManager.JoinGame += OnJoinGame;

            _variables = GetNode<GameVariables>("/root/GameVariables");
        }

        void OnHostGame(GameStartInfo info)
        {
            _variables.GameStartInfo = info;

            GD.Print("On host game!");
            if (!_server.CreateServer(info.Port))
            {
                // do something
                return;
            }


        }

        async void OnJoinGame(GameStartInfo info)
        {
            _variables.GameStartInfo = info;

            GD.Print("On join game!");

            if (!await _client.CreateClient(info.Address, info.Port))
            {
                // do something
                return;
            }


        }
    }
}
