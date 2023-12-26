using Godot;
using System;
using System.Threading.Tasks;

namespace MC
{
    public partial class Global : Node
    {
        public static uint DefaultSeed { get; private set; } = 20031231;

        public static uint DefaultPort { get; private set; } = 8848;

        public static string DefaultAddress { get; private set; } = "127.0.0.1";

        public static string DefaultName { get; private set; } = "Steve";

        public static byte[] AuthData { get; private set; } = new byte[] { 20, 3, 12, 31 };

        public static int ServerId { get; private set; } = 1;

        public static int RenderChunkCount { get { return (RenderChunkDistance * 2 + 1) * (RenderChunkDistance * 2 + 1); } }
        
        public static int RenderChunkDistance { get; private set; } = 4;

        public static Vector3I ChunkShape { get; private set; } = new Vector3I(16, 64, 16);

        public static Vector3 PlayerSpawnPosition { get; private set; } = new Vector3(0, 65, 0);

        public static float Gravity { get; private set; } = 9.8f;

        public static string PlayerGroup { get; private set; } = "PlayerGroup";

        public static string WorldGroup { get; private set; } = "WorldGroup";

        public static uint MaxTimeStampValue { get; private set; } = 1024;

        public static uint MaxTimeStampDelta { get; private set; } = 512;

        public static int RpcBlockVariantUnitCount { get; private set; } = 5;

        public static int FastChunkUpdateMaxManhattanDistance { get; private set; } = 2;

        public static int ColliderLayer { get; private set; } = 1;
        public static int NonColliderLayer { get; private set; } = 2;
        public static int PlayerLayer { get; private set; } = 3;

        public GameStartInfo GameStartInfo
        {
            get {  return _gameStartInfo; }
            set
            {
                _gameStartInfo = value;
                Seed = value.Seed;
            }
        }
        GameStartInfo _gameStartInfo;

        [Signal] public delegate void LocalPlayerSetEventHandler();
        public Player LocalPlayer
        {
            get { return _localPlayer; }
            set
            {
                _localPlayer = value;
                EmitSignal(SignalName.LocalPlayerSet);
            }
        }
        Player _localPlayer = null;

        [Signal] public delegate void SeedSetEventHandler(uint seed);
        public uint Seed
        {
            get { return GameStartInfo.Seed; }
            set
            {
                GameStartInfo.Seed = value;
                EmitSignal(SignalName.SeedSet, value);
            }
        }

        [Signal] public delegate void GameStateChangedEventHandler(int state);
        public GameState GameState
        {
            get { return _gameState; }
            set
            {
                _gameState = value;
                EmitSignal(SignalName.GameStateChanged, (int)value);
            }
        }
        GameState _gameState = GameState.InMainMenu;

        public async Task<bool> WaitForNewGameState(GameState targetState)
        {
            if (GameState == targetState)
            {
                return true;
            }
            else
            {
                await ToSignal(this, SignalName.GameStateChanged);
            }
            return GameState == targetState;
        }

    }
}
