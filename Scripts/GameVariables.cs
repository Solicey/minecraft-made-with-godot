using Godot;
using System;

namespace MC
{
    public partial class GameVariables : Node
    {
        public byte[] AuthData { get; private set; } = new byte[] { 20, 3, 12, 31 };

        public int ServerId { get; private set; } = 1;

        public GameStartInfo GameStartInfo { get; set; }

        public Player LocalPlayer { get; set; }

        [Signal] public delegate void ClientJoinGameStateChangedEventHandler(ClientJoinGameState state);
        public ClientJoinGameState ClientJoinGameState
        {
            get
            {
                return _clientJoinGameState;
            }
            set
            {
                _clientJoinGameState = value;
                EmitSignal(SignalName.ClientJoinGameStateChanged, _clientJoinGameState);
            }
        }
        ClientJoinGameState _clientJoinGameState;
    }
}
