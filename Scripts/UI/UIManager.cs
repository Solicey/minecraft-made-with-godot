using Godot;
using System;

namespace MC
{
    public partial class UIManager : Control
    {
        [Signal] public delegate void HostGameEventHandler(GameStartInfo info);

        [Signal] public delegate void JoinGameEventHandler(GameStartInfo info);

        [Signal] public delegate void InGameOptionsReturnEventHandler();

        [Export] MainMenuUI _mainMenuUI;
        [Export] HostGameUI _hostGameUI;
        [Export] JoinGameUI _joinGameUI;
        [Export] LoadingUI _loadingUI;
        [Export] InGameUI _inGameUI;
        [Export] OptionsUI _optionsUI;

        [Export] Camera3D _panoramaCamera;
        [Export] Camera3D _canvasCamera;
        [Export] Node3D _head;

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

        [Export] float _headRotateSpeed = -0.03f;

        Global _global;
        Control _lastControl = null;
        Control _currentControl = null;

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
            _global.GameStateChanged += OnGameStateChanged;

            _global.GameState = GameState.InMainMenu;

            _mainMenuUI.HostGameButtonPressed += () => { _global.GameState = GameState.InHostGamePage; };

            _mainMenuUI.JoinGameButtonPressed += () => { _global.GameState = GameState.InJoinGamePage; };

            _hostGameUI.ReturnToMainMenuButtonPressed += () => { _global.GameState = GameState.InMainMenu; };

            _hostGameUI.HostGameButtonPressed += (uint seed, uint port, string name) =>
            {
                EmitSignal(SignalName.HostGame, new GameStartInfo
                {
                    Seed = seed,
                    Port = port,
                    PlayerName = name
                });
            };

            _joinGameUI.ReturnToMainMenuButtonPressed += () => { _global.GameState = GameState.InMainMenu; };

            _joinGameUI.JoinGameButtonPressed += (string addr, uint port, string name) =>
            {
                EmitSignal(SignalName.JoinGame, new GameStartInfo
                {
                    Address = addr,
                    Port = port,
                    PlayerName = name
                });
            };

            _loadingUI.ReturnToMainMenuButtonPressed += () => { _global.GameState = GameState.InMainMenu; };

            _inGameUI.ReturnToMainMenuButtonPressed += () => { _global.GameState = GameState.InMainMenu; };

            _optionsUI.ReturnButtonPressed += () =>
            {
                if (_lastControl == _mainMenuUI)
                    _global.GameState = GameState.InMainMenu;
                else if (_lastControl == _inGameUI)
                    EmitSignal(SignalName.InGameOptionsReturn);
            };
        }

        public override void _Process(double delta)
        {
            if (_currentControl == _mainMenuUI)
            {
                _head.RotateY(_headRotateSpeed * (float)delta);
            }
        }

        void ChangeCurrentControlTo(Control control)
        {
            _lastControl = _currentControl;

            if (_currentControl == control)
                return;

            foreach (var child in GetChildren())
                if (child is Control)
                    RemoveChild(child);

            _currentControl = control;
            AddChild(_currentControl);
        }

        void OnGameStateChanged(int state)
        {
            var gameState = (GameState)state;
            GD.Print($"Game state: {gameState}");

            switch (gameState)
            {
                case GameState.InMainMenu:
                    ChangeCurrentControlTo(_mainMenuUI);
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    _panoramaCamera.MakeCurrent();
                    break;
                case GameState.InHostGamePage:
                    ChangeCurrentControlTo(_hostGameUI);
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    _canvasCamera.MakeCurrent();
                    break;
                case GameState.InJoinGamePage:
                    ChangeCurrentControlTo(_joinGameUI);
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    _canvasCamera.MakeCurrent();
                    break;
                case GameState.ClientConnecting:
                case GameState.ClientCantCreate:
                case GameState.ClientTimeout:
                case GameState.ClientDisconnected:
                case GameState.ClientConnected_SyncingPlayer:
                case GameState.ClientPlayerSynced_SyncingWorldSeed:
                case GameState.InitingWorld:
                case GameState.ServerCreating:
                case GameState.ServerCantCreate:
                case GameState.ServerCreated_SyncingPlayer:
                    ChangeCurrentControlTo(_loadingUI);
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    _canvasCamera.MakeCurrent();
                    break;
                case GameState.InGameActive:
                case GameState.InGamePaused:
                    ChangeCurrentControlTo(_inGameUI);
                    _global.LocalPlayer?.MakeCameraCurrent();
                    break;
            }

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

            }
        }
    }
}
