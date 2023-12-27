using Godot;
using System;

namespace MC
{
    public partial class JoinGameUI : Control
    {
        [Signal] public delegate void JoinGameButtonPressedEventHandler(string addr, uint port, string name);
        [Signal] public delegate void ReturnToMainMenuButtonPressedEventHandler();

        [Export] LineEdit _addrEntry;
        [Export] LineEdit _portEntry;
        [Export] LineEdit _nameEntry;
        [Export] Button _joinGameButton;
        [Export] Button _returnToMainMenuButton;

        public override void _Ready()
        {
            _joinGameButton.Pressed += () =>
            {
                var addr = _addrEntry.Text;
                if (addr == string.Empty)
                    addr = Global.DefaultAddress;

                if (!uint.TryParse(_portEntry.Text, out uint port))
                    port = Global.DefaultPort;

                var name = _nameEntry.Text;
                if (name == string.Empty)
                    name = Global.DefaultName;

                EmitSignal(SignalName.JoinGameButtonPressed, addr, port, name);
            };
            _returnToMainMenuButton.Pressed += () =>
            {
                EmitSignal(SignalName.ReturnToMainMenuButtonPressed);
            };
        }

    }
}
