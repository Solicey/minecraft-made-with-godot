using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MC
{
    public partial class Server : Node
    {
        [Signal] public delegate void UpdateChunkVariationDictDoneEventHandler();

        [Export] MultiplayerSpawner _multiplayerSpawner;

        Global _global;
        SceneMultiplayer _multiplayer;
        ENetMultiplayerPeer _peer;
        RPCFunctions _rpcFunctions;

        Dictionary<int, Node> _playerDict = new();

        Dictionary<Vector2I, ChunkVariation> _chunkVariationDict = new();
        bool _isUpdatingVarDict = false;

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
            _rpcFunctions = GetNode<RPCFunctions>("/root/RpcFunctions");

            _global.GameStateChanged += OnGameStateChanged;

            _rpcFunctions.ReceivedBlockVaryRequest += OnReceivedBlockVaryRequest;
            _rpcFunctions.ReceivedSendSyncChunkRequest += OnReceivedSendSyncChunkRequest;
        }
        public async Task<bool> CreateServer(uint port)
        {
            await Reset();

            _global.GameState = GameState.ServerCreating;

            _multiplayer = new();
            _multiplayer.AuthCallback = new Callable(this, nameof(OnAuthReceived));
            _multiplayer.PeerConnected += OnPeerConnected;
            _multiplayer.PeerDisconnected += OnPeerDisconnected;
            _multiplayer.PeerAuthenticating += OnPeerAuthenticating;
            _multiplayer.PeerAuthenticationFailed += OnPeerAuthFailed;
            GetTree().SetMultiplayer(_multiplayer);

            _peer = new(); 

            var error = _peer.CreateServer((int)port);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Create server failed: {error}");
                _global.GameState = GameState.ServerCantCreate;
                return false;
            }

            _multiplayer.MultiplayerPeer = _peer;

            GD.Print($"Create server on port {port}!");

            _global.GameState = GameState.ServerCreated_SyncingPlayer;

            _playerDict[Global.ServerId] = _multiplayerSpawner.Spawn(Global.ServerId);

            return true;
        }

        async Task Reset()
        {
            foreach (var player in _playerDict.Values)
                player?.QueueFree();
            foreach (var id in _playerDict.Keys)
                _multiplayer?.DisconnectPeer(id);
            _playerDict.Clear();

            while (_isUpdatingVarDict)
                await Task.Run(() => { CallDeferred(nameof(WaitForUpdateChunkVariationDictDone)); });
            _chunkVariationDict.Clear();
            EmitSignal(SignalName.UpdateChunkVariationDictDone);

            if (_peer != null )
            {
                _peer.Host?.Destroy();
                _peer = null;
            }
            _multiplayer = null;
        }

        void OnPeerConnected(long id)
        {
            GD.Print($"{id} connected to server!");
            _playerDict[(int)id] = _multiplayerSpawner.Spawn(id);
        }

        void OnPeerDisconnected(long id)
        {
            GD.Print($"{id} disconnected!");
            if (_playerDict.TryGetValue((int)id, out var player))
            {
                _playerDict.Remove((int)id);
                player.QueueFree();
            }
        }

        void OnPeerAuthenticating(long id)
        {
            var error = _multiplayer.SendAuth((int)id, Global.AuthData);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Send auth failed: {error}");
            }
        }

        void OnPeerAuthFailed(long id)
        {
            GD.Print($"Client {id} auth failed!");
        }

        void OnAuthReceived(int id, byte[] data)
        {
            if (!Global.AuthData.SequenceEqual(data))
            {
                GD.PrintErr($"Auth not match!");
                return;
            }
            GD.Print($"Receive auth from {id}!");
            _multiplayer.CompleteAuth(id);
        }

        async void OnReceivedBlockVaryRequest(Vector2I chunkPos, Vector3I blockLocalPos, int blockType)
        {
            if (World.IsBlockLocalPosOutOfBound(blockLocalPos))
                return;

            while (_isUpdatingVarDict)
                await Task.Run(() => { CallDeferred(nameof(WaitForUpdateChunkVariationDictDone)); });
            _isUpdatingVarDict = true;

            BlockType type = (BlockType)blockType;
            uint timeStamp = 0;

            if (_chunkVariationDict.TryGetValue(chunkPos, out var chunkVariation))
            {
                chunkVariation.BlockTypeDict[blockLocalPos] = type;
                if (chunkVariation.TimeStampDict.ContainsKey(blockLocalPos))
                    timeStamp = chunkVariation.TimeStampDict[blockLocalPos] = (chunkVariation.TimeStampDict[blockLocalPos] + 1) % Global.MaxTimeStampValue;
                else
                    chunkVariation.TimeStampDict[blockLocalPos] = 0;
            }
            else
            {
                chunkVariation = new ChunkVariation();
                chunkVariation.BlockTypeDict[blockLocalPos] = type;
                chunkVariation.TimeStampDict[blockLocalPos] = 0;
                _chunkVariationDict.Add(chunkPos, chunkVariation);
            }

            _rpcFunctions.Rpc(nameof(_rpcFunctions.SyncBlockVariation), chunkPos, blockLocalPos, blockType, timeStamp);

            _isUpdatingVarDict = false;
            EmitSignal(SignalName.UpdateChunkVariationDictDone);
        }

        async void OnGameStateChanged(int state)
        {
            GameState gameState = (GameState)state;
            switch (gameState)
            {
                case GameState.InMainMenu:
                    await Reset();
                    break;
            }
        }

        async void OnReceivedSendSyncChunkRequest(int id, Vector2I chunkPos)
        {
            while (_isUpdatingVarDict)
                await Task.Run(() => { CallDeferred(nameof(WaitForUpdateChunkVariationDictDone)); });
            _isUpdatingVarDict = true;

            if (!_chunkVariationDict.TryGetValue(chunkPos, out var chunkVariation))
            {
                _rpcFunctions.RpcId(id, nameof(_rpcFunctions.SyncChunkVariation), chunkPos, System.Array.Empty<int>());
                _isUpdatingVarDict = false;
                EmitSignal(SignalName.UpdateChunkVariationDictDone);
                return;
            }

            // X, Y, Z, blockType, timeStamp
            int[] array = new int[chunkVariation.BlockTypeDict.Count * Global.RpcBlockVariantUnitCount];
            await Task.Run(() =>
            {
                int i = 0;
                foreach (var pair in chunkVariation.BlockTypeDict)
                {
                    array[i++] = pair.Key.X;
                    array[i++] = pair.Key.Y;
                    array[i++] = pair.Key.Z;
                    array[i++] = (int)pair.Value;

                    if (!chunkVariation.TimeStampDict.TryGetValue(pair.Key, out var timeStamp))
                        array[i++] = 0;
                    else array[i++] = (int)timeStamp;
                }
            });

            _rpcFunctions.RpcId(id, nameof(_rpcFunctions.SyncChunkVariation), chunkPos, array);

            _isUpdatingVarDict = false;
            EmitSignal(SignalName.UpdateChunkVariationDictDone);
        }

        async void WaitForUpdateChunkVariationDictDone()
        {
            await ToSignal(this, SignalName.UpdateChunkVariationDictDone);
        }
    }

}