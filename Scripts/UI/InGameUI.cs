using Godot;
using System;

namespace MC
{
    public partial class InGameUI : Control
    {
        [Signal] public delegate void ReturnToMainMenuButtonPressedEventHandler();
        [Signal] public delegate void OptionsButtonPressedEventHandler();

        [Export] Panel _pausePanel;
        [Export] Button _continueGameButton;
        [Export] Button _optionsButton;
        [Export] Button _returnToMainMenuButton;
        [Export] ScrollContainer _kotobaContainer;
        Global _global;

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
            _global.GameStateChanged += OnGameStateChanged;

            _continueGameButton.Pressed += () =>
            {
                _global.GameState = GameState.InGameActive;
            };

            _returnToMainMenuButton.Pressed += () =>
            {
                EmitSignal(SignalName.ReturnToMainMenuButtonPressed);
            };

            _optionsButton.Pressed += () =>
            {
                EmitSignal(SignalName.OptionsButtonPressed);
            };
        }

        public override void _EnterTree()
        {
            if (_global != null)
                OnGameStateChanged((int)_global.GameState);
        }

        public override void _Process(double delta)
        {
            if (Input.IsActionJustPressed("Escape"))
            {
                if (_global.GameState == GameState.InGameActive)
                    _global.GameState = GameState.InGamePaused;
                else if (_global.GameState == GameState.InGamePaused)
                    _global.GameState = GameState.InGameActive;
            }
        }

        void OnGameStateChanged(int state)
        {
            var gameState = (GameState)state;

            switch (gameState)
            {
                case GameState.InGameActive:
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    _pausePanel.Hide();
                    break;
                case GameState.InGamePaused:
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    _pausePanel.Show();
                    break;
            }
        }
    }
}
