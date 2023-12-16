using Godot;
using System;
using System.Threading.Tasks;

namespace MC
{
    public partial class Chunk : StaticBody3D
    {
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

        Vector3I _shape;

        [Export] CollisionShape3D _collisionShape;

        [Export] MeshInstance3D _meshInstance;

        BlockType[,,] _blockTypeArray;



        SurfaceTool _surfaceTool = new();

        BlockManager _blockManager;

        public override void _Ready()
        {
            _blockManager = GetNode<BlockManager>("/root/BlockManager");

            _shape = Global.ChunkShape;
            _blockTypeArray = new BlockType[_shape.X, _shape.Y, _shape.Z];
        }

        public async Task SyncData(Vector2I chunkPos, TerrainGenerator generator)
        {
            _meshInstance.Mesh = null;
            ChunkPosition = chunkPos;

            await Task.Run(() =>
            {
                generator(chunkPos, _blockTypeArray);
            });

            IsDirty = true;
        }

        public async Task SyncMesh(BlockTypeGetter typeGetter)
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

                            _blockManager.DrawBlock(_blockTypeArray[x, y, z], blockLocalPos, blockWorldPos, _surfaceTool, typeGetter);

                        }
                    }
                }

                _surfaceTool.SetMaterial(_blockManager.Material);
                mesh = _surfaceTool.Commit();

            });

            _meshInstance.Mesh = mesh;
            _collisionShape.Shape = mesh.CreateTrimeshShape();

            IsDirty = false;
        }

        public BlockType GetLocalBlockType(Vector3I blockLocalPos)
        {
            if (World.IsBlockOutOfBound(blockLocalPos))
                return BlockType.Stone;

            return _blockTypeArray[blockLocalPos.X, blockLocalPos.Y, blockLocalPos.Z];
        }
    }

}