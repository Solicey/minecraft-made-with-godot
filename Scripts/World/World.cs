using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MC
{
    public delegate void TerrainGenerator(Vector2I chunkPos, BlockType[,,] blockArray);

    public delegate BlockType BlockTypeGetter(Vector3I blockWorldPos);

    public partial class World : Node3D
    {
        Global _global;
        BlockManager _blockManager;
        RPCFunctions _rpcFunctions;

        [Export] PackedScene _chunkScene;
        Dictionary<Vector2I, Chunk> _chunkPosMap = new();

        List<Vector2I> _renderOrder = new();

        [Export] FastNoiseLite _heightNoise;
        [Export] FastNoiseLite _plantNoise;

        [Export] float _chunkUpdateInterval = 0.5f;
        Vector2I _oldCenterChunkPos = new();
        Timer _chunkUpdateTimer = new();

        bool _isUpdating = false;

        [Signal] public delegate void UpdateChunkDoneEventHandler();
        [Signal] public delegate void ReceivedBlockVariationEventHandler(Vector2I chunkPos, Vector3I blockLocalPos, int blockType, bool shallCompareTimeStamp, uint timeStamp);

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
            _blockManager = GetNode<BlockManager>("/root/BlockManager");
            _rpcFunctions = GetNode<RPCFunctions>("/root/RpcFunctions");

            _global.SeedSet += (uint seed) =>
            {
                GD.Print($"Noise seed set: {seed}");
                _heightNoise.Seed = (int)seed;
                _plantNoise.Seed = (int)(seed + seed);
            };

            _global.LocalPlayerSet += () =>
            {
                _global.LocalPlayer.LocalPlayerBreakBlock += OnLocalPlayerBreakBlock;
                _global.LocalPlayer.LocalPlayerPlaceBlock += OnLocalPlayerPlaceBlock;
            };

            _rpcFunctions.ReceivedBlockVariation += OnReceivedBlockVariation;
            ReceivedBlockVariation += OnReceivedBlockVariation;

            _rpcFunctions.ReceivedChunkVariation += OnReceivedChunkVariation;

            AddChild(_chunkUpdateTimer);
            _chunkUpdateTimer.Timeout += OnChunkUpdateTimerTimeout;
            _chunkUpdateTimer.OneShot = true;

        }

        public static Vector2I WorldPosToChunkPos(Vector3 worldPos)
        {
            return new Vector2I(Mathf.FloorToInt(worldPos.X / Global.ChunkShape.X), Mathf.FloorToInt(worldPos.Z / Global.ChunkShape.Z));
        }

        public static Vector3I WorldPosToBlockWorldPos(Vector3 worldPos)
        {
            return new Vector3I(Mathf.FloorToInt(worldPos.X), Mathf.FloorToInt(worldPos.Y), Mathf.FloorToInt(worldPos.Z));
        }

        public static Vector3I ChunkPosToChunkWorldPos(Vector2I chunkPos)
        {
            return new Vector3I(chunkPos.X * Global.ChunkShape.X, 0, chunkPos.Y * Global.ChunkShape.Z);
        }

        public static Vector3I BlockWorldPosToBlockLocalPos(Vector3I worldPos)
        {
            return worldPos - ChunkPosToChunkWorldPos(WorldPosToChunkPos(worldPos));
        }

        public static bool IsBlockLocalPosOutOfBound(Vector3I blockLocalPos)
        {
            return blockLocalPos.X < 0 || blockLocalPos.X >= Global.ChunkShape.X ||
                blockLocalPos.Y < 0 || blockLocalPos.Y >= Global.ChunkShape.Y ||
                blockLocalPos.Z < 0 || blockLocalPos.Z >= Global.ChunkShape.Z;
        }

        public static int ChunkManhattanDistance(Vector2I chunkA, Vector2I chunkB)
        {
            return Math.Abs(chunkA.X - chunkB.X) + Math.Abs(chunkA.Y - chunkB.Y);
        }

        public async Task<bool> Init()
        {
            while (_isUpdating)
                await Task.Run(() => { CallDeferred(nameof(WaitForUpdateChunkDone)); });
            _isUpdating = true;
            _chunkUpdateTimer.Stop();

            UpdateRenderOrder();

            var centerChunkPos = WorldPosToChunkPos(Global.PlayerSpawnPosition);

            foreach (var chunk in _chunkPosMap.Values)
                chunk?.QueueFree();
            _chunkPosMap.Clear();

            List<Task> tasks = new();
            for (int i = 0; i < Global.RenderChunkCount; i++)
            {
                var chunkPos = centerChunkPos + _renderOrder[i];
                var chunk = _chunkScene.Instantiate<Chunk>();
                AddChild(chunk);
                chunk.Init(GenerateSimpleTerrain, GetBlockType);

                _chunkPosMap[chunkPos] = chunk;
                var task = Task.Run(() =>
                {
                    chunk.CallDeferred(nameof(chunk.SyncData), chunkPos);
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            tasks.Clear();
            for (int i = 0; i < Global.RenderChunkCount; i++)
            {
                var chunkPos = centerChunkPos + _renderOrder[i];
                var chunk = _chunkPosMap[chunkPos];
                var task = chunk.SyncMesh();
                tasks.Add(task);

                if (ChunkManhattanDistance(Vector2I.Zero, _renderOrder[i]) > Global.FastChunkUpdateMaxManhattanDistance)
                    continue;

                task = chunk.SyncCollider();
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            GD.Print("World create done");

            _isUpdating = false;
            EmitSignal(SignalName.UpdateChunkDone);
            _chunkUpdateTimer.Start(_chunkUpdateInterval);

            return true;
        }

        void GenerateSimpleTerrain(Vector2I chunkPos, BlockType[,,] blockArray)
        {
            var shape = Global.ChunkShape;
            var chunkWorldPos = chunkPos * new Vector2I(shape.X, shape.Z);

            for (int x = 0; x < shape.X; x++)
            {
                for (int z = 0; z < shape.Z; z++)
                {
                    var blockWorldPos = chunkWorldPos + new Vector2I(x, z);

                    int groundHeight = Math.Min((int)((_heightNoise.GetNoise2D(blockWorldPos.X, blockWorldPos.Y) + 1f) / 2f * shape.Y), shape.Y - 3);
                    int stoneHeight = (int)(groundHeight * 2f / 3f);

                    for (int y = 0; y < stoneHeight; y++)
                        blockArray[x, y, z] = BlockType.Stone;

                    for (int y = stoneHeight; y < groundHeight; y++)
                        blockArray[x, y, z] = BlockType.Dirt;

                    blockArray[x, groundHeight - 1, z] = BlockType.Grass;

                    for (int y = groundHeight; y < shape.Y; y++)
                        blockArray[x, y, z] = BlockType.Air;

                    float plantValue = (_plantNoise.GetNoise2D(blockWorldPos.X, blockWorldPos.Y) + 1f) / 2f;
                    if (plantValue > 0.7f)
                        blockArray[x, groundHeight, z] = BlockType.ShortGrass;
                    else if (plantValue < 0.2f)
                        blockArray[x, groundHeight, z] = BlockType.Dandelion;
                    else if (plantValue < 0.25f)
                        blockArray[x, groundHeight, z] = BlockType.Rose;
                }
            }
        }

        BlockType GetBlockType(Vector3I blockWorldPos)
        {
            var chunkPos = WorldPosToChunkPos(blockWorldPos);

            if (!_chunkPosMap.TryGetValue(chunkPos, out var chunk))
                return BlockType.Stone;

            var blockLocalPos = BlockWorldPosToBlockLocalPos(blockWorldPos);

            return chunk.GetLocalBlockType(blockLocalPos);
        }

        async Task<bool> Update(Vector2I centerChunkPos, bool isCenterChunkPosNew)
        {
            GD.Print("Begin update!");
            //GD.Print($"Center chunk pos: {centerChunkPos}");

            List<Task> tasks = new();

            if (isCenterChunkPosNew)
            {
                //GD.Print("Begin sync data!");

                List<Vector2I> chunkPositionsToUpdate = new();
                Dictionary<Vector2I, Chunk> newChunkPosMap = new();
                foreach (var delta in _renderOrder)
                {
                    var chunkPos = centerChunkPos + delta;
                    if (_chunkPosMap.ContainsKey(chunkPos))
                    {
                        newChunkPosMap[chunkPos] = _chunkPosMap[chunkPos];
                        _chunkPosMap.Remove(chunkPos);
                    }
                    else
                    {
                        chunkPositionsToUpdate.Add(chunkPos);
                    }
                }

                int i = 0;
                foreach (var chunk in _chunkPosMap.Values)
                {
                    var newChunkPos = chunkPositionsToUpdate[i++];
                    newChunkPosMap[newChunkPos] = chunk;
                }
                _chunkPosMap = newChunkPosMap;

                //GD.Print($"Chunks positions to update: {chunkPositionsToUpdate.Count}");

                foreach (var chunkPos in chunkPositionsToUpdate)
                {
                    var chunk = _chunkPosMap[chunkPos];
                    var task = Task.Run(() =>
                    {
                        chunk.CallDeferred(nameof(chunk.SyncData), chunkPos);
                    });
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);
            }

            //GD.Print("Update sync data done!");

            var dirtyChunkPositions = new HashSet<Vector2I>();
            foreach (var chunk in _chunkPosMap.Values)
            {
                if (chunk.IsDirty)
                    dirtyChunkPositions.Add(chunk.ChunkPosition);
            }

            //GD.Print($"Dirty chunks: {dirtyChunkPositions.Count}");

            tasks.Clear();
            foreach (var delta in _renderOrder)
            {
                var chunkPos = centerChunkPos + delta;

                if (!_chunkPosMap.ContainsKey(chunkPos))
                {
                    //GD.PrintErr($"Chunk not included: {chunkPos}");
                    continue;
                }

                var chunk = _chunkPosMap[chunkPos];

                if (dirtyChunkPositions.Contains(chunkPos) ||
                    dirtyChunkPositions.Contains(chunkPos + Vector2I.Up) ||
                    dirtyChunkPositions.Contains(chunkPos + Vector2I.Down) ||
                    dirtyChunkPositions.Contains(chunkPos + Vector2I.Left) ||
                    dirtyChunkPositions.Contains(chunkPos + Vector2I.Right))
                {
                    chunk.IsColliderUpToDate = false;
                    var task = chunk.SyncMesh();
                    //GD.Print($"Sync mesh: {chunkPos}");
                    tasks.Add(task);
                }

                if (!chunk.IsColliderUpToDate && ChunkManhattanDistance(Vector2I.Zero, delta) <= Global.FastChunkUpdateMaxManhattanDistance)
                {
                    var task = chunk.SyncCollider();
                    //GD.Print($"Sync collider: {chunkPos}");
                    tasks.Add(task);
                }
            }
            await Task.WhenAll(tasks);

            GD.Print("End update!");

            return true;
        }

        async void OnChunkUpdateTimerTimeout()
        {
            if (_isUpdating)
                return;
            _isUpdating = true;
            //GD.Print("Timer update begin!");

            if (_global.LocalPlayer == null)
                return;

            var newCenterChunkPos = _global.LocalPlayer.CurrentChunkPos;
            var isCenterChunkPosNew = newCenterChunkPos != _oldCenterChunkPos;

            if (isCenterChunkPosNew)
            {
                await Update(newCenterChunkPos, isCenterChunkPosNew);
                _oldCenterChunkPos = newCenterChunkPos;
            }
            else
            {
                bool hasDirtyChunks = false;
                foreach (var chunk in _chunkPosMap.Values)
                {
                    if (chunk.IsDirty)
                    {
                        hasDirtyChunks = true;
                        break;
                    }
                }
                if (hasDirtyChunks)
                    await Update(newCenterChunkPos, isCenterChunkPosNew);
            }

            _isUpdating = false;
            EmitSignal(SignalName.UpdateChunkDone);
            _chunkUpdateTimer.Start(_chunkUpdateInterval);

            //GD.Print("Timer update end!");
        }

        async void OnReceivedBlockVariation(Vector2I chunkPos, Vector3I blockLocalPos, int blockType, bool shallCompareTimeStamp, uint timeStamp = 0)
        {
            BlockType type = (BlockType)blockType;
            while (_isUpdating)
                await Task.Run(() => { CallDeferred(nameof(WaitForUpdateChunkDone)); });
            _isUpdating = true;

            if (!_chunkPosMap.TryGetValue(chunkPos, out Chunk chunk) ||
                !chunk.ApplyBlockVariation(blockLocalPos, type, shallCompareTimeStamp, timeStamp) ||
                _global.LocalPlayer == null)
            {
                _isUpdating = false;
                EmitSignal(SignalName.UpdateChunkDone);
                _chunkUpdateTimer.Start(_chunkUpdateInterval);
                return;
            }

            var newCenterChunkPos = _global.LocalPlayer.CurrentChunkPos;
            var isCenterChunkPosNew = newCenterChunkPos != _oldCenterChunkPos;

            if (ChunkManhattanDistance(newCenterChunkPos, chunkPos) <= Global.FastChunkUpdateMaxManhattanDistance)
            {
                await Update(newCenterChunkPos, isCenterChunkPosNew);
                _oldCenterChunkPos = newCenterChunkPos;
            }

            _isUpdating = false;
            EmitSignal(SignalName.UpdateChunkDone);
            _chunkUpdateTimer.Start(_chunkUpdateInterval);
        }

        void OnReceivedChunkVariation(Vector2I chunkPos, int[] array)
        {
            if (!_chunkPosMap.TryGetValue(chunkPos, out Chunk chunk))
                return;

            chunk.IsSyncDataArrived = true;
            chunk.EmitSignal(Chunk.SignalName.SyncDataArrived, array);
        }

        async void OnLocalPlayerBreakBlock(RayCastHitBlockInfo info)
        {
            while (_isUpdating)
                await Task.Run(() => { CallDeferred(nameof(WaitForUpdateChunkDone)); });

            if (!_chunkPosMap.TryGetValue(info.ChunkPos, out Chunk chunk))
                return;

            if (IsBlockLocalPosOutOfBound(info.BlockLocalPos))
                return;

            var oldBlockType = chunk.GetLocalBlockType(info.BlockLocalPos);
            if (!_blockManager.IsBreakable(oldBlockType))
                return;

            EmitSignal(SignalName.ReceivedBlockVariation, info.ChunkPos, info.BlockLocalPos, (int)BlockType.Air, false, 0);
            _rpcFunctions.RpcId(Global.ServerId, nameof(_rpcFunctions.SendVaryBlockRequest), Multiplayer.GetUniqueId(), info.ChunkPos, info.BlockLocalPos, (int)BlockType.Air);
        }

        async void OnLocalPlayerPlaceBlock(RayCastHitBlockInfo info)
        {
            while (_isUpdating)
                await Task.Run(() => { CallDeferred(nameof(WaitForUpdateChunkDone)); });

            if (!_chunkPosMap.TryGetValue(info.ChunkPos, out Chunk chunk))
                return;

            var hitBlockLocalPos = info.BlockLocalPos;
            if (IsBlockLocalPosOutOfBound(hitBlockLocalPos))
                return;

            var hitBlockWorldPos = info.BlockWorldPos;
            Vector3I norm = (Vector3I)info.HitFaceNormal.Normalized();

            var newChunkPos = WorldPosToChunkPos(hitBlockWorldPos + norm);
            if (!_chunkPosMap.ContainsKey(newChunkPos))
                return;

            var newBlockWorldPos = WorldPosToBlockWorldPos(hitBlockWorldPos + norm);
            var newBlockLocalPos = BlockWorldPosToBlockLocalPos(newBlockWorldPos);
            if (IsBlockLocalPosOutOfBound(newBlockLocalPos))
                return;

            var hitBlockType = chunk.GetLocalBlockType(hitBlockLocalPos);
            if (!_blockManager.IsPlacable(hitBlockType, BlockType.Stone, norm))
                return;

            var playerList = GetTree().GetNodesInGroup(Global.PlayerGroup);
            foreach (var node in playerList)
            {
                var player = (Player)node;
                if (player.OccupiedBlockWorldPositions.Contains(newBlockWorldPos))
                    return;
            }

            EmitSignal(SignalName.ReceivedBlockVariation, newChunkPos, newBlockLocalPos, (int)BlockType.Stone, false, 0);
            _rpcFunctions.RpcId(Global.ServerId, nameof(_rpcFunctions.SendVaryBlockRequest), Multiplayer.GetUniqueId(), newChunkPos, newBlockLocalPos, (int)BlockType.Stone);
        }

        async void WaitForUpdateChunkDone()
        {
            await ToSignal(this, SignalName.UpdateChunkDone);
        }

        void UpdateRenderOrder()
        {
            _renderOrder.Clear();
            var dis = Global.RenderChunkDistance;
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
    }
}