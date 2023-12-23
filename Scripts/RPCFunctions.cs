using Godot;
using System;

namespace MC
{
    public partial class RPCFunctions : Node
    {
        Global _global;

        [Signal] public delegate void ReceivedBlockVaryRequestEventHandler(Vector2I chunkPos, Vector3I blockLocalPos, int blockType);

        [Signal] public delegate void ReceivedBlockVariationEventHandler(Vector2I chunkPos, Vector3I blockLocalPos, int blockType, bool shallCompareTimeStamp, uint timeStamp);

        [Signal] public delegate void ReceivedSendSyncChunkRequestEventHandler(int id, Vector2I chunkPos);

        [Signal] public delegate void ReceivedChunkVariationEventHandler(Vector2I chunkPos, int[] array);


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

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
        public void SendVaryBlockRequest(int id, Vector2I chunkPos, Vector3I blockLocalPos, BlockType blockType)
        {
            if (Multiplayer.IsServer())
            {
                EmitSignal(SignalName.ReceivedBlockVaryRequest, chunkPos, blockLocalPos, (int)blockType);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
        public void SyncBlockVariation(Vector2I chunkPos, Vector3I blockLocalPos, BlockType blockType, uint timeStamp)
        {
            //GD.Print($"Sync block variation: chunkPos: {chunkPos}, blockLocalPos: {blockLocalPos}, blockType: {blockType}, timeStamp: {timeStamp}");
            EmitSignal(SignalName.ReceivedBlockVariation, chunkPos, blockLocalPos, (int)blockType, true, timeStamp);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
        public void SendSyncChunkRequest(int id, Vector2I chunkPos)
        {
            if (Multiplayer.IsServer())
            {
                //GD.Print($"Received sync chunk request, id: {id}, chunkPos: {chunkPos}");
                EmitSignal(SignalName.ReceivedSendSyncChunkRequest, id, chunkPos);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
        public void SyncChunkVariation(Vector2I chunkPos, int[] array)
        {
            //GD.Print($"Sync chunk variation, chunkPos: {chunkPos}");
            EmitSignal(SignalName.ReceivedChunkVariation, chunkPos, array);
        }
    }

}