using Godot;
using System;

namespace MC
{
    public enum BlockType
    {
        Air,
        Dirt,
        Stone,
        Grass,
        ShortGrass,

    }

    public enum Outlook
    {
        Cubic,
        Billboard,
        Stair
    }

    public enum MaterialType
    {
        Opaque,
        AlphaClip,
        AlphaBlend
    }

    public enum ColliderType
    {
        Collidable,
        NotCollidable
    }

    [GlobalClass]
    public partial class Block : Resource
    {
        [Export] public BlockType Type { get; set; }

        [Export] public Outlook Outlook { get; set; }

        [Export] public Texture2D MainTexture { get; set; }

        [Export] public Texture2D TopTexture { get; set; }

        [Export] public Texture2D BottomTexture { get; set; }

        public Texture2D[] Textures => new Texture2D[] { MainTexture, TopTexture, BottomTexture };

        [Export] public bool IsTransparent { get; set; }

        [Export] public MaterialType MaterialType { get; set; }

        [Export] public ColliderType ColliderType { get; set; }

        [Export] public bool UseCustomCollider { get; set; }
        [Export] public Outlook CustomColliderOutlook { get; set; }
        [Export] public Vector3 CustomColliderScale { get; set; }

        public Block() { }
    }
}
