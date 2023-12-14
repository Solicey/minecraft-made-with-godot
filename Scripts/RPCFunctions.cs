using Godot;
using System;

namespace MC
{
    public partial class RPCFunctions : Node
    {
        GameVariables _variables;

        public override void _Ready()
        {
            _variables = GetNode<GameVariables>("/root/GameVariables");
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void SyncSeed(int id, uint seed)
        {
            if (Multiplayer.IsServer())
            {
                GD.Print($"Send seed to client, seed: {_variables.Seed}");
                RpcId(id, nameof(SyncSeed), id, _variables.Seed);
            }
            else
            {
                GD.Print($"Client receive seed: {seed}");
                _variables.Seed = seed;     // Should emit signal
            }
        }
    }

}