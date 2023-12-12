using Godot;
using System;

namespace MC
{
    public partial class Player : CharacterBody3D
    {
        [Export] public int Id { get; set; }
        [Export] public string NameTag { get; set; }

        GameVariables _variables;

        public override void _Ready()
        {
            if (!IsMultiplayerAuthority())
                return;

            _variables = GetNode<GameVariables>("/root/GameVariables");
            
            NameTag = _variables.GameStartInfo.PlayerName;

            _variables.LocalPlayer = this;
        }
    }
}
