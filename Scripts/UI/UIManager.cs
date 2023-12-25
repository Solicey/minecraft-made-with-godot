using Godot;
using System;

namespace MC
{
    public partial class UIManager : Control
    {
        [Signal] public delegate void HostGameEventHandler(GameStartInfo info);

        [Signal] public delegate void JoinGameEventHandler(GameStartInfo info);

        [Export] Panel _mainMenuPanel;
        [Export] Button _hostButton;
        [Export] Button _joinButton;
        [Export] TextEdit _addressEntry;
        [Export] TextEdit _portEntry;
        [Export] TextEdit _nameEntry;
        [Export] TextEdit _seedEntry;

        [Export] Panel _gameLoadingPanel;
        [Export] Label _loadingMessageLabel;
        [Export] Button _returnToMenuButton;

        Global _global;

        const string _defaultAddress = "127.0.0.1";
        const string _defaultPort = "8848";
        const string _defaultName = "Steve";

        public override void _Ready()
        {
            _addressEntry.Text = _defaultAddress;
            _portEntry.Text = _defaultPort;
            _nameEntry.Text = _defaultName;
            _seedEntry.Text = GD.Randi().ToString();

            _global = GetNode<Global>("/root/Global");

            _global.GameStateChanged += OnGameStateChanged;
        }

        public void OnHostButtonPressed()
        {
            EmitSignal(SignalName.HostGame, new GameStartInfo
            {
                Address = _addressEntry.Text,
                Port = GetPort(),
                PlayerName = _nameEntry.Text,
                Seed = GetSeed()
            });
        }

        public void OnJoinButtonPressed()
        {
            EmitSignal(SignalName.JoinGame, new GameStartInfo
            {
                Address = _addressEntry.Text,
                Port = GetPort(),
                PlayerName = _nameEntry.Text,
                Seed = GetSeed()
            });
        }

        public void OnReturnToMenuButtonPressed()
        {
            _gameLoadingPanel.Visible = false;
            // do something
        }

        int GetPort()
        {
            return int.TryParse(_portEntry.Text, out var port) ? port : int.Parse(_defaultPort);
        }

        uint GetSeed()
        {
            return uint.TryParse(_seedEntry.Text, out var seed) ? seed : GD.Randi();
        }

        void OnGameStateChanged(int state)
        {
            var gameState = (GameState)state;
            GD.Print($"Game state: {gameState}");

            switch (gameState)
            {
                case GameState.InGameActive:
                    _mainMenuPanel.Visible = false;
                    break;
            }
        }
    }
}