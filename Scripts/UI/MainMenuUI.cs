using Godot;
using System;

namespace MC
{
    public partial class MainMenuUI : Control
    {
        [Signal] public delegate void HostGameButtonPressedEventHandler();
        [Signal] public delegate void JoinGameButtonPressedEventHandler();
        [Signal] public delegate void OptionsButtonPressedEventHandler();

        [Export] Control _splash;

        [Export] Button _hostGameButton;
        [Export] Button _joinGameButton;
        [Export] Button _optionsButton;
        [Export] Button _quitButton;

        [Export] Vector2 _splashMaxScale = new(1f, 1f);
        [Export] Vector2 _splashMinScale = new(0.7f, 0.7f);
        [Export] float _splashDuration = 1.0f;

        Tween _splashTween = null;

        public override void _Ready()
        {
            _splashTween?.Kill();
            _splashTween = CreateTween();

            _splash.Scale = _splashMaxScale;
            _splashTween.TweenProperty(_splash, "scale", _splashMinScale, _splashDuration / 2f).SetTrans(Tween.TransitionType.Sine);
            _splashTween.TweenProperty(_splash, "scale", _splashMaxScale, _splashDuration / 2f).SetTrans(Tween.TransitionType.Sine);
            _splashTween.SetLoops();

            _hostGameButton.Pressed += () => { EmitSignal(SignalName.HostGameButtonPressed); };
            _joinGameButton.Pressed += () => { EmitSignal(SignalName.JoinGameButtonPressed); };
            _optionsButton.Pressed += () => { EmitSignal(SignalName.OptionsButtonPressed); };
            _quitButton.Pressed += () => { GetTree().Quit(); };
        }

        public override void _EnterTree()
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
}
