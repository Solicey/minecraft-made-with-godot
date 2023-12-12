using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MC
{
    [Tool]
    public partial class BlockManager : Node
    {
        public StandardMaterial3D Material { get; set; }

        Dictionary<BlockType, Block> _blockDict = new();
        Dictionary<Texture2D, Vector2> _atlasLookUp = new();

        const int _atlasWidth = 4;
        int _atlasHeight = 0;

        const int _blockTextureSize = 16;

        [Export] ImageTexture _atlasTexture;

        public override void _Ready()
        {
            LoadBlocks();
            DrawAtlas();
            CreateMaterial();
        }

        void LoadBlocks()
        {
            foreach (BlockType blockType in Enum.GetValues(typeof(BlockType)))
            {
                var block = GD.Load<Block>($"res://Resources/Blocks/{blockType}.tres");
                if (block == null)
                    continue;
                GD.Print($"Load {blockType}");

                _blockDict[blockType] = block;
            }
        }

        void DrawAtlas()
        {
            var blockTextureArray = _blockDict.Values.SelectMany(block => block.Textures).Where(texture => texture != null).Distinct().ToArray();

            int count = blockTextureArray.Length;
            GD.Print($"Texture count: {count}");

            _atlasHeight = Mathf.CeilToInt(count / (float)_atlasWidth);

            var atlasImage = Image.Create(_atlasWidth * _blockTextureSize, _atlasHeight * _blockTextureSize, false, Image.Format.Rgba8);

            for (int i = 0; i < count; i++)
            {
                int x = (i % _atlasWidth);
                int y = (i / _atlasWidth);
                
                _atlasLookUp[blockTextureArray[i]] = new Vector2(x / (float)(_atlasWidth), y / (float)(_atlasHeight));

                var blockImage = blockTextureArray[i].GetImage();
                blockImage.Convert(Image.Format.Rgba8);

                atlasImage.BlitRect(blockImage, new Rect2I(Vector2I.Zero, _blockTextureSize, _blockTextureSize), new Vector2I(x, y) * _blockTextureSize);
            }

            _atlasTexture = ImageTexture.CreateFromImage(atlasImage);

            if (_atlasTexture == null)
                GD.PrintErr("Atlas texture is null!");
        }

        void CreateMaterial()
        {
            Material = new StandardMaterial3D()
            {
                AlbedoTexture = _atlasTexture,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
            };
        }
    }

}