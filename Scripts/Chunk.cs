using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MC
{
    public partial class ChunkVariation : GodotObject
    {
        public Dictionary<Vector3I, BlockType> BlockTypeDict = new();
        public Dictionary<Vector3I, uint> TimeStampDict = new();
    }

    public partial class Chunk : StaticBody3D
    {
        [Signal] public delegate void SyncDataArrivedEventHandler(int[] array);

        public Vector2I ChunkPosition
        {
            get
            {
                return _chunkPosition;
            }
            set
            {
                _chunkPosition = value;
                Position = World.ChunkPosToChunkWorldPos(value);
            }
        }
        Vector2I _chunkPosition;

        public bool IsDirty { get; private set; } = false;

        public bool IsSyncDataArrived { get; set; } = false;

        Vector3I _shape;

        [Export] CollisionShape3D _collisionShape;

        [Export] MeshInstance3D _meshInstance;

        BlockType[,,] _blockTypeArray;

        SurfaceTool _surfaceTool = new();

        BlockManager _blockManager;
        RPCFunctions _rpcFunctions;

        ChunkVariation _chunkVariation = new();

        TerrainGenerator _generator = null;
        BlockTypeGetter _typeGetter = null;

        public override void _Ready()
        {
            _blockManager = GetNode<BlockManager>("/root/BlockManager");
            _rpcFunctions = GetNode<RPCFunctions>("/root/RpcFunctions");

            _shape = Global.ChunkShape;
            _blockTypeArray = new BlockType[_shape.X, _shape.Y, _shape.Z];
        }

        public void Init(TerrainGenerator generator, BlockTypeGetter typeGetter)
        {
            _generator = generator;
            _typeGetter = typeGetter;
        }

        public async void SyncData(Vector2I chunkPos)
        {
            _meshInstance.Mesh = null;
            ChunkPosition = chunkPos;

            await Task.Run(() =>
            {
                _generator(chunkPos, _blockTypeArray);
            });

            IsSyncDataArrived = false;
            
            _rpcFunctions.RpcId(Global.ServerId, nameof(_rpcFunctions.SendSyncChunkRequest), Multiplayer.GetUniqueId(), chunkPos);

            IsDirty = true;

            if (IsSyncDataArrived)
                return;

            GD.Print($"Chunk {chunkPos} await for remote data");

            var awaiter = ToSignal(this, SignalName.SyncDataArrived);
            await awaiter;
            int[] array = awaiter.GetResult()[0].As<int[]>();

            for (int i = 0; i < array.Length; i += Global.RpcBlockVariantUnitCount)
            {
                var blockLocalPos = new Vector3I(array[i], array[i + 1], array[i + 2]);
            }

            GD.Print($"Chunk {chunkPos} finish await");
        }

        public async Task SyncMesh()
        {
            ArrayMesh mesh = new();

            await Task.Run(() =>
            {
                _surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

                var chunkWorldPos = World.ChunkPosToChunkWorldPos(ChunkPosition);

                for (int x = 0; x < _shape.X; x++)
                {
                    for (int y = 0; y < _shape.Y; y++)
                    {
                        for (int z = 0; z < _shape.Z; z++)
                        {
                            var blockLocalPos = new Vector3I(x, y, z);
                            var blockWorldPos = chunkWorldPos + blockLocalPos;

                            _blockManager.DrawBlock(_blockTypeArray[x, y, z], blockLocalPos, blockWorldPos, _surfaceTool, _typeGetter);
                        }
                    }
                }

                //GD.Print("Reach here!");

                _surfaceTool.SetMaterial(_blockManager.Material);
                mesh = _surfaceTool.Commit();

            });

            //GD.Print("Sync mesh done!");

            _meshInstance.Mesh = mesh;
            _collisionShape.Shape = mesh.CreateTrimeshShape();

            IsDirty = false;
        }

        public BlockType GetLocalBlockType(Vector3I blockLocalPos)
        {
            if (World.IsBlockLocalPosOutOfBound(blockLocalPos))
                return BlockType.Stone;

            return _blockTypeArray[blockLocalPos.X, blockLocalPos.Y, blockLocalPos.Z];
        }

        public bool ApplyBlockVariation(Vector3I blockLocalPos, BlockType blockType, bool shallCompareTimeStamp, uint timeStamp)
        {
            if (World.IsBlockLocalPosOutOfBound(blockLocalPos))
                return IsDirty;

            if (shallCompareTimeStamp && _chunkVariation.TimeStampDict.TryGetValue(blockLocalPos, out uint oldTimeStamp) && (oldTimeStamp >= timeStamp && oldTimeStamp <= timeStamp + Global.MaxTimeStampDelta))
                return IsDirty;

            var oldBlockType = _blockTypeArray[blockLocalPos.X, blockLocalPos.Y, blockLocalPos.Z];
            _blockTypeArray[blockLocalPos.X, blockLocalPos.Y, blockLocalPos.Z] = blockType;
            
            if (shallCompareTimeStamp)
                _chunkVariation.TimeStampDict[blockLocalPos] = timeStamp;
            
            IsDirty = (oldBlockType != blockType) || IsDirty;
            return IsDirty;
        }

        async void WaitForSyncDataArrivedSignal()
        {
            await ToSignal(this, SignalName.SyncDataArrived);
        }
    }

}