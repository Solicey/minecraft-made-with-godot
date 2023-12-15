using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MC
{
    public enum ClientState
    {
        Closed,
        Connecting,
        Connected,
        CanNotCreate,
        TimeOut,
        Disconnected,
        PlayerSynced_SyncingWorldSeed,
        WorldSeedSynced_CreatingWorld,
    }

    public partial class Client : Node
    {
        [Signal] public delegate void ClientLatestStateChangedEventHandler(int state);
        public ClientState ClientLatestState
        {
            get { return _clientLatestState; }
            private set
            {
                _clientLatestState = value;
                EmitSignal(SignalName.ClientLatestStateChanged, (int)_clientLatestState);
            }
        }
        ClientState _clientLatestState;

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
                if (ClientLatestState == ClientState.Connecting) 
                    ClientLatestState = ClientState.TimeOut;
            };

            _global.LocalPlayerSet += () =>
            {
                if (ClientLatestState == ClientState.Connected)
                    ClientLatestState = ClientState.PlayerSynced_SyncingWorldSeed;
            };

            _global.SeedSet += (uint seed) =>
            {
                GD.Print("Seed set!");
                if (ClientLatestState == ClientState.PlayerSynced_SyncingWorldSeed)
                    ClientLatestState = ClientState.WorldSeedSynced_CreatingWorld;
            };
            
        }

        public async Task<bool> CreateClient(string address, int port)
        {
            Reset();

            ClientLatestState = ClientState.Connecting;

            _multiplayer = new(); 
            _multiplayer.AuthCallback = new Callable(this, nameof(OnAuthReceived));
            _multiplayer.ConnectedToServer += OnConnectedToServer;
            _multiplayer.ServerDisconnected += OnServerDisconnected;
            _multiplayer.PeerAuthenticating += OnPeerAuthenticating;
            GetTree().SetMultiplayer(_multiplayer);

            _peer = new();

            var error = await Task.Run(() =>
            {
                return _peer.CreateClient(address, port);
            });
            if (error != Error.Ok)
            {
                GD.PrintErr($"Create client failed: {error}");
                ClientLatestState = ClientState.CanNotCreate;
                return false;
            }

            _multiplayer.MultiplayerPeer = _peer;

            _timeOutTimer.Start(_timeOutTime);

            return await WaitForNewClientState(ClientState.Connected);
        }

        public async Task<bool> SyncSeed()
        {
            _rpcFunctions.RpcId(Global.ServerId, nameof(SyncSeed), Multiplayer.GetUniqueId(), 0);

            return await WaitForNewClientState(ClientState.WorldSeedSynced_CreatingWorld);
        }

        public async Task<bool> WaitForNewClientState(ClientState targetState)
        {
            //GD.Print($"Wait for client state: {targetState}");
            if (ClientLatestState == targetState)
            {
                //GD.Print("Wait for client state: return immediately");
                return true;
            }
            else
            {
                //GD.Print("Wait for client state: have to wait");
                await ToSignal(this, SignalName.ClientLatestStateChanged);
            }
            //GD.Print("Wait for client state: end waiting");
            return ClientLatestState == targetState;
        }

        void OnConnectedToServer()
        {
            GD.Print($"Connected to server!");
            ClientLatestState = ClientState.Connected;
        }

        void OnServerDisconnected()
        {
            GD.Print($"Disconnected from server!");
            ClientLatestState = ClientState.Disconnected;
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

            ClientLatestState = ClientState.Closed;
        }
    }
}
