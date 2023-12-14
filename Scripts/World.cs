using Godot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MC
{
    public delegate void TerrainGenerator(Vector2I chunkPos, BlockType[,,] blockArray);

    public delegate BlockType BlockTypeGetter(Vector3I blockWorldPos);

    public partial class World : Node3D
    {
        GameVariables _variables;
        BlockManager _blockManager;

        [Export] PackedScene _chunkScene;
        List<Chunk> _chunks = new();
        Dictionary<Vector2I, Chunk> _chunkPosMap = new();

        List<Vector2I> _renderOrder = new();

        [Export] FastNoiseLite _fastNoiseLite;

        public override void _Ready()
        {
            _variables = GetNode<GameVariables>("/root/GameVariables");
            _blockManager = GetNode<BlockManager>("/root/BlockManager");

            for (int i = 0; i < GameVariables.RenderChunkCount; i++)
            {
                var chunk = _chunkScene.Instantiate<Chunk>();
                AddChild(chunk);
                _chunks.Add(chunk);
            }

            _variables.SeedSet += (uint seed) =>
            {
                GD.Print($"Noise seed set: {seed}");
                _fastNoiseLite.Seed = (int)seed;
            };

            var dis = GameVariables.RenderChunkDistance;
            for (int x = 0; x <= dis; x++)
            {
                for (int y = 0; y <= dis; y++)
                {
                    for (int sx = -1; sx <= ((x > 0) ? 1 : 0); sx += 2)
                    {
                        for (int sy = -1; sy <= ((y > 0) ? 1 : 0); sy += 2)
                        {
                            _renderOrder.Add(new Vector2I(x * sx, y * sy));
                        }
                    }
                }
            }
        }

        public static Vector2I WorldPosToChunkPos(Vector3 worldPos)
        {
            return new Vector2I(Mathf.FloorToInt(worldPos.X / GameVariables.ChunkShape.X), Mathf.FloorToInt(worldPos.Z / GameVariables.ChunkShape.Z));
        }

        public static Vector3I ChunkPosToChunkWorldPos(Vector2I chunkPos)
        {
            return new Vector3I(chunkPos.X * GameVariables.ChunkShape.X, 0, chunkPos.Y * GameVariables.ChunkShape.Z);
        }

        public static Vector3I BlockWorldPosToBlockLocalPos(Vector3I worldPos)
        {
            return worldPos - ChunkPosToChunkWorldPos(WorldPosToChunkPos(worldPos));
        }

        public static bool IsBlockOutOfBound(Vector3I blockLocalPos)
        {
            return blockLocalPos.X < 0 || blockLocalPos.X >= GameVariables.ChunkShape.X ||
                blockLocalPos.Y < 0 || blockLocalPos.Y >= GameVariables.ChunkShape.Y ||
                blockLocalPos.Z < 0 || blockLocalPos.Z >= GameVariables.ChunkShape.Z;
        }

        public async Task<bool> Create()
        {
            var centerChunkPos = WorldPosToChunkPos(GameVariables.PlayerSpawnPosition);

            _chunkPosMap.Clear();

            List<Task> tasks = new();
            for (int i = 0; i < _chunks.Count; i++)
            {
                var chunkPos = centerChunkPos + _renderOrder[i];
                var chunk = _chunks[i];

                _chunkPosMap[chunkPos] = chunk;
                var task = chunk.SyncData(chunkPos, GenerateSimpleTerrain);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            tasks.Clear();
            for (int i = 0; i < _chunks.Count; i++)
            {
                var chunkPos = centerChunkPos + _renderOrder[i];
                var chunk = _chunkPosMap[chunkPos];
                var task = chunk.SyncMesh(GetBlockType);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            GD.Print("World create done");

            return true;
        }

        void GenerateSimpleTerrain(Vector2I chunkPos, BlockType[,,] blockArray)
        {
            var shape = GameVariables.ChunkShape;
            var chunkWorldPos = chunkPos * new Vector2I(shape.X, shape.Z);

            for (int x = 0; x < shape.X; x++)
            {
                for (int z = 0; z < shape.Z; z++)
                {
                    var blockWorldPos = chunkWorldPos + new Vector2I(x, z);

                    int groundHeight = (int)((_fastNoiseLite.GetNoise2D(blockWorldPos.X, blockWorldPos.Y) + 1f) / 2f * shape.Y * 0.9f);
                    int stoneHeight = (int)(groundHeight * 2f / 3f);

                    for (int y = 0; y < stoneHeight; y++)
                        blockArray[x, y, z] = BlockType.Stone;

                    for (int y = stoneHeight; y < groundHeight; y++)
                        blockArray[x, y, z] = BlockType.Dirt;

                    blockArray[x, groundHeight - 1, z] = BlockType.Grass;

                    for (int y = groundHeight; y < shape.Y; y++)
                        blockArray[x, y, z] = BlockType.Air;
                }
            }
        }

        BlockType GetBlockType(Vector3I blockWorldPos)
        {
            var chunkPos = WorldPosToChunkPos(blockWorldPos);
            
            if (!_chunkPosMap.TryGetValue(chunkPos, out var chunk))
                return BlockType.Air;

            var blockLocalPos = BlockWorldPosToBlockLocalPos(blockWorldPos);

            return chunk.GetLocalBlockType(blockLocalPos);
        }


    }
}
