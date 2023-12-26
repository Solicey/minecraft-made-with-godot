using Godot;
using System;

namespace MC
{
    public partial class MainMenu : Control
    {
        [Export] Camera3D _camera;
        [Export] Node3D _head;

        [Export] float _headRotateSpeed = -0.03f;

        bool _isEnabled = false;

        public override void _Ready()
        {
            Enable();
        }

        public void Enable()
        {
            _isEnabled = true;
            _camera.MakeCurrent();
        }

        public override void _Process(double delta)
        {
            if (!_isEnabled)
                return;

            _head.RotateY(_headRotateSpeed * (float)delta);
        }
    }
}
