using Godot;
using System;

namespace MC
{
    public partial class PlayerSpawner : MultiplayerSpawner
    {
        [Export] PackedScene _playerScene;

        public override void _Ready()
        {
            SpawnFunction = new Callable(this, nameof(SpawnPlayerFunction));
        }

        Node SpawnPlayerFunction(Variant data)
        {
            var id = (int)data;

            Player player = _playerScene.Instantiate<Player>();
            player.Id = id;
            player.Name = id.ToString();
            player.SetMultiplayerAuthority(id);
            player.AddToGroup(Global.PlayerGroup);

            return player;
        }
    }
}
