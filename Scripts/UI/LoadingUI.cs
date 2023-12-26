using Godot;
using System;

namespace MC
{
    public partial class LoadingUI : Control
    {
        [Export] Label _loadingLabel;
        [Export] Button _returnToMainMenuButton;
        [Signal] public delegate void ReturnToMainMenuButtonPressedEventHandler();

        public void SetLoadingMessage(string text)
        {
            _loadingLabel.Text = text;
        }

        public void SetErrorMessage(string text)
        {
            _loadingLabel.Text = text;
            _returnToMainMenuButton.Show();
        }

        public override void _Ready()
        {
            _returnToMainMenuButton.Pressed += () =>
            {
                EmitSignal(SignalName.ReturnToMainMenuButtonPressed);
            };
        }

        public override void _EnterTree()
        {
            _returnToMainMenuButton.Hide();
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

}