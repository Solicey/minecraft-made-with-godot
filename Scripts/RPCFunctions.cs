using Godot;
using System;

namespace MC
{
    public partial class RPCFunctions : Node
    {
        Global _global;

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void SyncSeed(int id, uint seed)
        {
            if (Multiplayer.IsServer())
            {
                GD.Print($"Send seed to client, seed: {_global.Seed}");
                RpcId(id, nameof(SyncSeed), id, _global.Seed);
            }
            else
            {
                GD.Print($"Client receive seed: {seed}");
                _global.Seed = seed;     // Should emit signal
            }
        }
    }

}