using Godot;
using Godot.Collections;
using System;
using System.Linq;

namespace MC
{
    public enum ServerState
    {
        
    }

    public partial class Server : Node
    {
        [Export] MultiplayerSpawner _multiplayerSpawner;

        Global _global;
        SceneMultiplayer _multiplayer;
        ENetMultiplayerPeer _peer;

        Dictionary<int, Node> _playerDict = new();

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
        }

        public bool CreateServer(int port)
        {
            Reset();

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
                return false;
            }

            _multiplayer.MultiplayerPeer = _peer;

            GD.Print($"Create server on port {port}!");

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
    }

}