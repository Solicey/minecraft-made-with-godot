using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MC
{
    public partial class Client : Node
    {
        Global _global;
        RPCFunctions _rpcFunctions;
        SceneMultiplayer _multiplayer;
        ENetMultiplayerPeer _peer; 

        [Export] float _timeOutTime = 3.0f;
        Timer _timeOutTimer = new Timer();

        public bool IsConnectedToServer()
        {
            return _peer != null && _peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
        }

        public override void _Ready()
        {
            _global = GetNode<Global>("/root/Global");
            _rpcFunctions = GetNode<RPCFunctions>("/root/RpcFunctions");

            AddChild(_timeOutTimer);
            _timeOutTimer.OneShot = true;
            _timeOutTimer.Timeout += () =>
            {
                if (_global.GameState == GameState.ClientConnecting)
                    _global.GameState = GameState.ClientTimeout;
            };

            _global.GameStateChanged += OnGameStateChanged;
        }

        public async Task<bool> CreateClient(string address, uint port)
        {
            Reset();

            _global.GameState = GameState.ClientConnecting;

            _multiplayer = new(); 
            _multiplayer.AuthCallback = new Callable(this, nameof(OnAuthReceived));
            _multiplayer.ConnectedToServer += OnConnectedToServer;
            _multiplayer.ServerDisconnected += OnServerDisconnected;
            _multiplayer.PeerAuthenticating += OnPeerAuthenticating;
            GetTree().SetMultiplayer(_multiplayer);

            _peer = new();

            var error = await Task.Run(() =>
            {
                return _peer.CreateClient(address, (int)port);
            });
            if (error != Error.Ok)
            {
                GD.PrintErr($"Create client failed: {error}");
                _global.GameState = GameState.ClientCantCreate;
                return false;
            }

            _multiplayer.MultiplayerPeer = _peer;

            _timeOutTimer.Start(_timeOutTime);

            return await _global.WaitForNewGameState(GameState.ClientConnected_SyncingPlayer);
        }

        public bool SyncSeed()
        {
            return _rpcFunctions.RpcId(Global.ServerId, nameof(_rpcFunctions.SyncSeed), Multiplayer.GetUniqueId(), 0) == Error.Ok;
        }

        void OnConnectedToServer()
        {
            GD.Print($"Connected to server!");
            _global.GameState = GameState.ClientConnected_SyncingPlayer;
        }

        void OnServerDisconnected()
        {
            GD.Print($"Disconnected from server!");
            _global.GameState = GameState.ClientDisconnected;
        }

        void OnPeerAuthenticating(long id)
        {
            var error = _multiplayer.SendAuth((int)id, Global.AuthData);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Send auth failed: {error}");
            }
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

        void Reset()
        {
            _multiplayer?.DisconnectPeer(Global.ServerId);
            if (_peer != null)
            {
                _peer.Host?.Destroy();
                _peer = null;
            }
            _multiplayer = null;
        }

        void OnGameStateChanged(int state)
        {
            GameState gameState = (GameState)state;
            switch (gameState)
            {
                
            }
        }
    }
}
