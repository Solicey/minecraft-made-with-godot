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

    public partial class Chunk : Node3D
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

        public bool IsColliderUpToDate { get; set; } = false;

        public bool IsSyncDataArrived { get; set; } = false;

        Vector3I _shape;

        Dictionary<MaterialType, MeshInstance3D> _meshMap = new();
        Dictionary<ColliderType, StaticBody3D> _staticBodyMap = new();
        Dictionary<ColliderType, CollisionShape3D> _colliderMap = new();

        BlockType[,,] _blockTypeArray;

        Dictionary<MaterialType, SurfaceTool> _meshSurfaceTools = new();
        Dictionary<ColliderType, SurfaceTool> _colliderSurfaceTools = new();

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

            foreach (MaterialType type in Enum.GetValues(typeof(MaterialType)))
            {
                var mesh = new MeshInstance3D();
                AddChild(mesh);
                _meshMap[type] = mesh;

                var surfaceTool = new SurfaceTool();
                _meshSurfaceTools[type] = surfaceTool;
            }
            foreach (ColliderType type in Enum.GetValues(typeof(ColliderType)))
            {
                var body = new StaticBody3D();
                AddChild(body);
                body.AddToGroup(Global.WorldGroup);
                _staticBodyMap[type] = body;

                var collider = new CollisionShape3D();
                body.AddChild(collider);
                _colliderMap[type] = collider;
                _colliderSurfaceTools[type] = new();
            }

            _staticBodyMap[ColliderType.NotCollidable].SetCollisionLayerValue(Global.ColliderLayer, false);
            _staticBodyMap[ColliderType.NotCollidable].SetCollisionLayerValue(Global.NonColliderLayer, true);
        }

        public void Init(TerrainGenerator generator, BlockTypeGetter typeGetter)
        {
            _generator = generator;
            _typeGetter = typeGetter;
            IsDirty = false;
            IsColliderUpToDate = false;
        }

        public async Task SyncData(Vector2I chunkPos)
        {
            foreach (var mesh in _meshMap.Values)
                mesh.Mesh = null;

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

            //GD.Print($"Chunk {chunkPos} await for remote data");

            var awaiter = ToSignal(this, SignalName.SyncDataArrived);
            await awaiter;
            int[] array = awaiter.GetResult()[0].As<int[]>();

            _chunkVariation.TimeStampDict.Clear();
            for (int i = 0; i < array.Length; i += Global.RpcBlockVariantUnitCount)
            {
                var blockLocalPos = new Vector3I(array[i], array[i + 1], array[i + 2]);
                var blockType = (BlockType)array[i + 3];
                var timeStamp = (uint)array[i + 4];

                _blockTypeArray[blockLocalPos.X, blockLocalPos.Y, blockLocalPos.Z] = blockType;
                _chunkVariation.TimeStampDict[blockLocalPos] = timeStamp;
            }

            //GD.Print($"Chunk {chunkPos} finish await");
        }

        public async Task SyncMesh()
        {
            Dictionary<MaterialType, ArrayMesh> meshes = new();

            await Task.Run(() =>
            {
                foreach (var tool in _meshSurfaceTools.Values)
                    tool.Begin(Mesh.PrimitiveType.Triangles);

                var chunkWorldPos = World.ChunkPosToChunkWorldPos(ChunkPosition);

                for (int x = 0; x < _shape.X; x++)
                {
                    for (int y = 0; y < _shape.Y; y++)
                    {
                        for (int z = 0; z < _shape.Z; z++)
                        {
                            var blockLocalPos = new Vector3I(x, y, z);
                            var blockWorldPos = chunkWorldPos + blockLocalPos;

                            _blockManager.DrawMesh(_blockTypeArray[x, y, z], blockLocalPos, blockWorldPos, Vector3.Zero, Vector3.One, _meshSurfaceTools, _typeGetter);
                        }
                    }
                }

                //GD.Print("Reach here!");

                foreach (var pair in _meshSurfaceTools)
                {
                    pair.Value.SetMaterial(_blockManager.MaterialMap.GetValueOrDefault(pair.Key));
                    meshes[pair.Key] = pair.Value.Commit();
                }

            });

            //GD.Print("Sync mesh done!");

            foreach (var type in meshes.Keys)
                _meshMap[type].Mesh = meshes[type];

            //_collisionShape.Shape = mesh.CreateTrimeshShape();

            IsDirty = false;
        }

        public async Task SyncCollider()
        {
            Dictionary<ColliderType, ArrayMesh> colliders = new();

            await Task.Run(() =>
            {
                foreach (var tool in _colliderSurfaceTools.Values)
                    tool.Begin(Mesh.PrimitiveType.Triangles);

                var chunkWorldPos = World.ChunkPosToChunkWorldPos(ChunkPosition);

                for (int x = 0; x < _shape.X; x++)
                {
                    for (int y = 0; y < _shape.Y; y++)
                    {
                        for (int z = 0; z < _shape.Z; z++)
                        {
                            var blockLocalPos = new Vector3I(x, y, z);
                            var blockWorldPos = chunkWorldPos + blockLocalPos;

                            _blockManager.DrawCollider(_blockTypeArray[x, y, z], blockLocalPos, blockWorldPos, Vector3.Zero, Vector3.One, _colliderSurfaceTools, _typeGetter);
                        }
                    }
                }

                //GD.Print("Reach here!");

                foreach (var pair in _colliderSurfaceTools)
                    colliders[pair.Key] = pair.Value.Commit();

            });

            //GD.Print("Sync mesh done!");

            foreach (var type in colliders.Keys)
                _colliderMap[type].Shape = colliders[type].CreateTrimeshShape();

            //_collisionShape.Shape = mesh.CreateTrimeshShape();

            IsColliderUpToDate = true;
        }

        public BlockType GetLocalBlockType(Vector3I blockLocalPos)
        {
            if (World.IsBlockLocalPosOutOfBound(blockLocalPos))
            {
                if (blockLocalPos.Y >= Global.ChunkShape.Y)
                    return BlockType.Air;
                return BlockType.Stone;
            }

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
    }

}