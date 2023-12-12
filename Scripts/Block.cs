using Godot;
using System;

namespace MC
{
    public enum BlockType
    {
        Air,
        Dirt,
        Stone,
        Grass
    }

    [Tool]
    [GlobalClass]
    public partial class Block : Resource
    {
        [Export] public BlockType Type { get; set; }

        [Export] public Texture2D MainTexture { get; set; }

        [Export] public Texture2D TopTexture { get; set; }

        [Export] public Texture2D BottomTexture { get; set; }

        public Texture2D[] Textures => new Texture2D[] { MainTexture, TopTexture, BottomTexture };

        [Export] public bool IsTransparent { get; set; }

        public Block() { }
    }
}
