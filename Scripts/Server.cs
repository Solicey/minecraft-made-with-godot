using Godot;
using Godot.Collections;
using System;
using System.Linq;

namespace MC
{
    public partial class Server : Node
    {
        [Export] MultiplayerSpawner _multiplayerSpawner;

        Global _global;
        SceneMultiplayer _multiplayer;
        ENetMultiplayerPeer _peer;
        RPCFunctions _rpcFunctions;

        Dictionary<int, Node> _playerDict = new();

        Dictionary<Vector2I, ChunkVariation> _chunkVariationDict = new();

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
            _rpcFunctions = GetNode<RPCFunctions>("/root/RpcFunctions");

            _global.GameStateChanged += OnGameStateChanged;

            _rpcFunctions.ReceivedBlockBreakRequest += OnReceivedBlockBreakRequest;
            _rpcFunctions.ReceivedSendSyncChunkRequest += OnReceivedSendSyncChunkRequest;
        }

        public bool CreateServer(int port)
        {
            Reset();

            _global.GameState = GameState.ServerCreating;

            _multiplayer = new();
            _multiplayer.AuthCallback = new Callable(this, nameof(OnAuthReceived));
            _multiplayer.PeerConnected += OnPeerConnected;
            _multiplayer.PeerDisconnected += OnPeerDisconnected;
            _multiplayer.PeerAuthenticating += OnPeerAuthenticating;
            _multiplayer.PeerAuthenticationFailed += OnPeerAuthFailed;
            GetTree().SetMultiplayer(_multiplayer);

            _peer = new(); 

            var error = _peer.CreateServer(port);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Create server failed: {error}");
                _global.GameState = GameState.ServerCantCreate;
                return false;
            }

            _multiplayer.MultiplayerPeer = _peer;

            GD.Print($"Create server on port {port}!");

            _global.GameState = GameState.ServerCreated_SyncingPlayer;
            _multiplayerSpawner.Spawn(Global.ServerId);

            return true;
        }

        void Reset()
        {
            if (_peer != null )
            {
                _peer.Host.Destroy();
                _peer = null;
            }
            _multiplayer = null;
            _playerDict.Clear();
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

        void OnReceivedBlockBreakRequest(Vector2I chunkPos, Vector3I blockLocalPos, Vector3 hitFaceNormal)
        {
            if (World.IsBlockLocalPosOutOfBound(blockLocalPos))
                return;

            GD.Print($"Receive block break request! {chunkPos} {blockLocalPos}");

            var blockType = BlockType.Air;
            uint timeStamp = 0;

            if (_chunkVariationDict.TryGetValue(chunkPos, out var chunkVariation))
            {
                chunkVariation.BlockTypeDict[blockLocalPos] = blockType;
                if (chunkVariation.TimeStampDict.ContainsKey(blockLocalPos))
                    timeStamp = chunkVariation.TimeStampDict[blockLocalPos] = (chunkVariation.TimeStampDict[blockLocalPos] + 1) % Global.MaxTimeStampValue;
                else
                    chunkVariation.TimeStampDict[blockLocalPos] = 0;
            }
            else
            {
                chunkVariation = new ChunkVariation();
                chunkVariation.BlockTypeDict[blockLocalPos] = blockType;
                chunkVariation.TimeStampDict[blockLocalPos] = 0;
                _chunkVariationDict.Add(chunkPos, chunkVariation);
            }

            _rpcFunctions.Rpc(nameof(_rpcFunctions.SyncBlockVariation), chunkPos, blockLocalPos, (int)blockType, timeStamp);
        }

        void OnGameStateChanged(int state)
        {
            GameState gameState = (GameState)state;
            switch (gameState)
            {
                case GameState.ServerPlayerSynced_InitingWorld:
                    _global.LocalPlayer.LocalPlayerBreakBlock += (RayCastHitBlockInfo info) =>
                    {
                        OnReceivedBlockBreakRequest(info.ChunkPos, info.BlockLocalPos, info.HitFaceNormal);
                    };
                    break;
            }
        }

        void OnReceivedSendSyncChunkRequest(int id, Vector2I chunkPos)
        {

        }
    }

}