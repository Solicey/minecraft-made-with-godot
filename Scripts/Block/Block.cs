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

    public enum Outlook
    {
        Cubic,
        Billboard,
        Stair
    }

    public enum TransparentType
    {
        AlphaClip,
        Blend
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

        [Export] public TransparentType TransparentType { get; set; }

        [Export] public bool HasCollider { get; set; }

        // 如果没有collider，需要定义triggerbox的形状和位置
        [Export] public Vector3 TriggerBoxScale { get; set; }
        [Export] public bool IsTriggerBoxStaysAtCenter { get; set; }

        public Block() { }
    }
}
