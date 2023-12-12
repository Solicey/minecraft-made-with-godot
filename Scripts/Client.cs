using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MC
{
    public enum ClientJoinGameStep
    {
        NotJoined,
        Joining,
        Joined
    }

    public partial class ClientJoinGameState : GodotObject
    {
        public ClientJoinGameStep CurrentStep { get; set; }
        public string Message { get; set; }
    }

    public partial class Client : Node
    {
        GameVariables _variables;
        SceneMultiplayer _multiplayer;
        ENetMultiplayerPeer _peer; 

        [Export] float _timeOutTime = 3.0f;
        Timer _timeOutTimer = new Timer();

        [Signal] public delegate void CreateClientFinishedEventHandler();

        public bool IsConnectedToServer()
        {
            return _peer != null && _peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
        }

        public override void _Ready()
        {
            _variables = GetNode<GameVariables>("/root/GameVariables");

            AddChild(_timeOutTimer);
            _timeOutTimer.OneShot = true;
            _timeOutTimer.Timeout += () =>
            {
                if (IsConnectedToServer())
                    return;
                SetClientJoinGameState(ClientJoinGameStep.NotJoined, "Failed to connect to server: Timeout");
                EmitSignal(SignalName.CreateClientFinished);
            };
            
        }

        public async Task<bool> CreateClient(string address, int port)
        {
            SetClientJoinGameState(ClientJoinGameStep.Joining, "Connecting to server...");

            Reset();

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
                SetClientJoinGameState(ClientJoinGameStep.NotJoined, $"Failed to connect to server: {error}");
                return false;
            }

            _multiplayer.MultiplayerPeer = _peer;

            _timeOutTimer.Start(_timeOutTime);

            if (IsConnectedToServer())
                return true;

            await ToSignal(this, SignalName.CreateClientFinished);

            return IsConnectedToServer();
        }

        void OnConnectedToServer()
        {
            GD.Print($"Connected to server!");
            SetClientJoinGameState(ClientJoinGameStep.Joining, "Connected to server");
            EmitSignal(SignalName.CreateClientFinished);
        }

        void OnServerDisconnected()
        {
            SetClientJoinGameState(ClientJoinGameStep.NotJoined, "Disconnected from server");
            GD.Print($"Disconnected from server!");
        }

        void OnPeerAuthenticating(long id)
        {
            var error = _multiplayer.SendAuth((int)id, _variables.AuthData);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Send auth failed: {error}");
            }
        }

        void OnAuthReceived(int id, byte[] data)
        {
            if (!_variables.AuthData.SequenceEqual(data))
            {
                GD.PrintErr($"Auth not match!");
                return;
            }
            GD.Print($"Receive auth from {id}!");
            _multiplayer.CompleteAuth(id);
        }

        void Reset()
        {
            if (_peer != null)
            {
                _peer.Host?.Destroy();
                _peer = null;
            }
            _multiplayer = null;
        }

        void SetClientJoinGameState(ClientJoinGameStep step, string msg)
        {
            _variables.ClientJoinGameState = new ClientJoinGameState
            {
                CurrentStep = step,
                Message = msg
            };
        }
    }
}
