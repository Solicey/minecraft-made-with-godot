using Godot;
using System;

namespace MC
{
    public partial class UIManager : Control
    {
        [Signal] public delegate void HostGameEventHandler(GameStartInfo info);

        [Signal] public delegate void JoinGameEventHandler(GameStartInfo info);

        [Export] MainMenuUI _mainMenuUI;
        [Export] HostGameUI _hostGameUI;
        [Export] JoinGameUI _joinGameUI;
        [Export] LoadingUI _loadingUI;
        [Export] InGameUI _inGameUI;

        [Export] string _clientCreatingMsg;
        [Export] string _clientCantCreateErrorMsg;
        [Export] string _clientDisconnectedErrorMsg;
        [Export] string _clientTimeoutErrorMsg;
        [Export] string _clientSyncingPlayerMsg;
        [Export] string _clientSyncingSeedMsg;
        [Export] string _initingWorldMsg;
        [Export] string _serverCreatingMsg;
        [Export] string _serverCantCreateErrorMsg;
        [Export] string _serverSyncingPlayerMsg;

        Global _global;
        Control _currentControl = null;

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");

            _global.GameStateChanged += OnGameStateChanged;

            ChangeCurrentControlTo(_mainMenuUI);

            _mainMenuUI.HostGameButtonPressed += () => { ChangeCurrentControlTo(_hostGameUI); };

            _mainMenuUI.JoinGameButtonPressed += () => { ChangeCurrentControlTo(_joinGameUI); };

            _hostGameUI.ReturnToMainMenuButtonPressed += () => { ChangeCurrentControlTo(_mainMenuUI); };

            _hostGameUI.HostGameButtonPressed += (uint seed, uint port, string name) =>
            {
                ChangeCurrentControlTo(_loadingUI);
                EmitSignal(SignalName.HostGame, new GameStartInfo
                {
                    Seed = seed,
                    Port = port,
                    PlayerName = name
                });
            };

            _joinGameUI.ReturnToMainMenuButtonPressed += () => { ChangeCurrentControlTo(_mainMenuUI); };

            _joinGameUI.JoinGameButtonPressed += (string addr, uint port, string name) =>
            {
                ChangeCurrentControlTo(_loadingUI);
                EmitSignal(SignalName.JoinGame, new GameStartInfo
                {
                    Address = addr,
                    Port = port,
                    PlayerName = name
                });
            };

            _loadingUI.ReturnToMainMenuButtonPressed += () => { ChangeCurrentControlTo(_mainMenuUI); };
        }

        void ChangeCurrentControlTo(Control control)
        {
            foreach (var child in GetChildren())
                RemoveChild(child);

            _currentControl = control;
            AddChild(_currentControl);

            if (_currentControl == _mainMenuUI)
                _global.GameState = GameState.InMainMenu;
        }

        void OnGameStateChanged(int state)
        {
            var gameState = (GameState)state;
            GD.Print($"Game state: {gameState}");

            switch (gameState)
            {
                case GameState.ClientConnecting:
                    _loadingUI.SetLoadingMessage(_clientCreatingMsg);
                    break;
                case GameState.ClientCantCreate:
                    _loadingUI.SetErrorMessage(_clientCantCreateErrorMsg);
                    break;
                case GameState.ClientTimeout:
                    _loadingUI.SetErrorMessage(_clientTimeoutErrorMsg);
                    break;
                case GameState.ClientDisconnected:
                    _loadingUI.SetErrorMessage(_clientDisconnectedErrorMsg);
                    break;
                case GameState.ClientConnected_SyncingPlayer:
                    _loadingUI.SetLoadingMessage(_clientSyncingPlayerMsg);
                    break;
                case GameState.ClientPlayerSynced_SyncingWorldSeed:
                    _loadingUI.SetLoadingMessage(_clientSyncingSeedMsg);
                    break;
                case GameState.InitingWorld:
                    _loadingUI.SetLoadingMessage(_initingWorldMsg);
                    break;
                case GameState.ServerCreating:
                    _loadingUI.SetLoadingMessage(_serverCreatingMsg);
                    break;
                case GameState.ServerCantCreate:
                    _loadingUI.SetErrorMessage(_serverCantCreateErrorMsg);
                    break;
                case GameState.ServerCreated_SyncingPlayer:
                    _loadingUI.SetLoadingMessage(_serverSyncingPlayerMsg);
                    break;
                case GameState.InGameActive:
                    ChangeCurrentControlTo(_inGameUI);
                    break;
            }
        }
    }
}
