using Godot;
using System;
using System.Threading.Tasks;

namespace MC
{
    public partial class Global : Node
    {
        public static byte[] AuthData { get; private set; } = new byte[] { 20, 3, 12, 31 };

        public static int ServerId { get; private set; } = 1;

        public static int RenderChunkCount { get { return (RenderChunkDistance * 2 + 1) * (RenderChunkDistance * 2 + 1); } }
        
        public static int RenderChunkDistance { get; private set; } = 5;

        public static Vector3I ChunkShape { get; private set; } = new Vector3I(16, 64, 16);

        public static Vector3 PlayerSpawnPosition { get; private set; } = new Vector3(0, 64, 0);

        public static float Gravity { get; private set; } = 9.8f;

        public static string PlayerGroupName { get; private set; } = "PlayerGroup";

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

    }
}
