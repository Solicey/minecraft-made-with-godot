using Godot;
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

        GameVariables _variables;
        SceneMultiplayer _multiplayer;
        ENetMultiplayerPeer _peer;

        public override void _Ready()
        {
            _variables = GetNode<GameVariables>("/root/GameVariables");
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

            _multiplayerSpawner.Spawn(GameVariables.ServerId);

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
        }

        void OnPeerConnected(long id)
        {
            GD.Print($"{id} connected to server!");
            _multiplayerSpawner.Spawn(id);
        }

        void OnPeerDisconnected(long id)
        {
            GD.Print($"{id} disconnected!");
        }

        void OnPeerAuthenticating(long id)
        {
            var error = _multiplayer.SendAuth((int)id, GameVariables.AuthData);
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
            if (!GameVariables.AuthData.SequenceEqual(data))
            {
                GD.PrintErr($"Auth not match!");
                return;
            }
            GD.Print($"Receive auth from {id}!");
            _multiplayer.CompleteAuth(id);
        }
    }

}