using Godot;
using System;

namespace MC
{
    public partial class HostGameUI : Control
    {
        [Signal] public delegate void HostGameButtonPressedEventHandler(uint seed, uint port, string name);
        [Signal] public delegate void ReturnToMainMenuButtonPressedEventHandler();

        [Export] LineEdit _seedEntry;
        [Export] LineEdit _portEntry;
        [Export] LineEdit _nameEntry;
        [Export] Button _hostGameButton;
        [Export] Button _returnToMainMenuButton;

        public override void _Ready()
        {
            _hostGameButton.Pressed += () =>
            {
                if (!uint.TryParse(_seedEntry.Text, out uint seed))
                    seed = Global.DefaultSeed;
                if (!uint.TryParse(_portEntry.Text, out uint port))
                    port = Global.DefaultPort;

                var name = _nameEntry.Text;
                if (name == string.Empty)
                    name = Global.DefaultName;

                EmitSignal(SignalName.HostGameButtonPressed, seed, port, name);
            };
            _returnToMainMenuButton.Pressed += () =>
            {
                EmitSignal(SignalName.ReturnToMainMenuButtonPressed);
            };
        }

    }

}