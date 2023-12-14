using Godot;
using System;

namespace MC
{
    public partial class Player : CharacterBody3D
    {
        [Export] public int Id { get; set; }
        [Export] public string NameTag { get; set; }

        GameVariables _variables;

        [Export] Camera3D _camera;
        [Export] RayCast3D _rayCast;


        public override void _Ready()
        {
            if (!IsMultiplayerAuthority())
                return;

            _camera.Visible = true;

            _variables = GetNode<GameVariables>("/root/GameVariables");
            
            NameTag = _variables.GameStartInfo.PlayerName;

            _variables.LocalPlayer = this;  // Should send signal

            Position = GameVariables.PlayerSpawnPosition;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!IsMultiplayerAuthority())
                return;

            ProcessInput(delta);
            ProcessMovement(delta);
        }

        void ProcessInput(double delta)
        {

        }

        void ProcessMovement(double delta)
        {

        }
    }
}
