using Godot;
using System;

namespace MC
{
    public partial class RPCFunctions : Node
    {
        Global _global;

        [Signal] public delegate void ReceivedBlockBreakRequestEventHandler(Vector2I chunkPos, Vector3I blockLocalPos, Vector3 hitFaceNormal);

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

        [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
        public void SendBreakBlockRequest(int id, Vector2I chunkPos, Vector3I blockLocalPos, Vector3 hitFaceNormal)
        {
            if (Multiplayer.IsServer())
            {
                EmitSignal(SignalName.ReceivedBlockBreakRequest, chunkPos, blockLocalPos, hitFaceNormal);
            }
        }

        public void SyncBlockVariation()
        {

        }
    }

}