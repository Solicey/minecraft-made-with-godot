using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MC
{
    public static class Cubic
    {
        public static readonly Vector3[] Vertices = new Vector3[]
        {
            new Vector3I(0, 0, 0),
            new Vector3I(1, 0, 0),
            new Vector3I(0, 1, 0),
            new Vector3I(1, 1, 0),
            new Vector3I(0, 0, 1),
            new Vector3I(1, 0, 1),
            new Vector3I(0, 1, 1),
            new Vector3I(1, 1, 1)
        };

        public static readonly Vector2[] UVCoords = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        };

        public static readonly int[] Top = new int[] { 2, 3, 7, 6 };
        public static readonly int[] Bottom = new int[] { 0, 4, 5, 1 };
        public static readonly int[] Left = new int[] { 6, 4, 0, 2 };
        public static readonly int[] Right = new int[] { 3, 1, 5, 7 };
        public static readonly int[] Back = new int[] { 7, 5, 4, 6 };
        public static readonly int[] Front = new int[] { 2, 0, 1, 3 };
        public static readonly int[] UVIndices = new int[] { 0, 1, 2, 3 };
    }

    public enum BlockFace
    {
        Unknown,
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    public partial class BlockManager : Node
    {
        public StandardMaterial3D Material { get; private set; }

        Dictionary<BlockType, Block> _blockDict = new();
        Dictionary<Texture2D, Vector2> _textureUVOffsetDict = new();

        const int _atlasWidth = 4;
        int _atlasHeight = 0;

        float _uvWidth { get { return 1f / _atlasWidth; } }
        float _uvHeight { get { return 1f / _atlasHeight; } }
        Vector2 _uvSize { get { return new Vector2(_uvWidth, _uvHeight); } }

        const int _blockTextureSize = 16;

        [Export] ImageTexture _atlasTexture;

        Dictionary<Vector3I, BlockFace> _blockFaceDict = new Dictionary<Vector3I, BlockFace>
        {
            {Vector3I.Up, BlockFace.Top},
            {Vector3I.Down, BlockFace.Bottom},
            {Vector3I.Left, BlockFace.Left},
            {Vector3I.Right, BlockFace.Right},
            {Vector3I.Forward, BlockFace.Front},
            {Vector3I.Back, BlockFace.Back}
        };
        
        public override void _Ready()
        {
            LoadBlocks();
            DrawAtlas();
            CreateMaterial();
        }

        public Block GetBlock(BlockType type)
        {
            if (!_blockDict.TryGetValue(type, out var block))
                return null;
            return block;
        }

        public bool IsTransparent(BlockType type)
        {
            if (!_blockDict.TryGetValue(type, out var block))
                return true;
            return block.IsTransparent;
        }

        public void DrawBlock(BlockType blockType, Vector3I blockLocalPos, Vector3I blockWorldPos, SurfaceTool surfaceTool, BlockTypeGetter typeGetter)
        {
            if (blockType == BlockType.Air)
                return;

            var block = GetBlock(blockType);
            if (block == null)
                return;

            switch (block.Outlook)
            {
                case Outlook.Cubic:
                    DrawCubic(blockLocalPos, blockWorldPos, block, surfaceTool, typeGetter);
                    break;
            }
        }

        public bool IsBreakable(BlockType blockType)
        {
            return true;
        }

        public bool IsPlacable(BlockType hitBlockType, BlockType newBlockType, Vector3I hitNormal)
        {
            return true;
        }

        public BlockFace NormalToBlockFace(Vector3I normal)
        {
            if (!_blockFaceDict.TryGetValue(normal, out BlockFace blockFace))
                return BlockFace.Unknown;
            return blockFace;
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
                
                _textureUVOffsetDict[blockTextureArray[i]] = new Vector2(x / (float)(_atlasWidth), y / (float)(_atlasHeight));

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

        void DrawCubic(Vector3I blockLocalPos, Vector3I blockWorldPos, Block block, SurfaceTool surfaceTool, BlockTypeGetter typeGetter)
        {
            bool isTransparent = block.IsTransparent;

            if (isTransparent || IsTransparent(typeGetter(blockWorldPos + Vector3I.Up)))
                DrawRect(Cubic.Vertices, Cubic.Top, Cubic.UVCoords, Cubic.UVIndices, blockLocalPos, block.TopTexture ?? block.MainTexture, surfaceTool);

            if (isTransparent || IsTransparent(typeGetter(blockWorldPos + Vector3I.Down)))
                DrawRect(Cubic.Vertices, Cubic.Bottom, Cubic.UVCoords, Cubic.UVIndices, blockLocalPos, block.BottomTexture ?? block.MainTexture, surfaceTool);

            if (isTransparent || IsTransparent(typeGetter(blockWorldPos + Vector3I.Left)))
                DrawRect(Cubic.Vertices, Cubic.Left, Cubic.UVCoords, Cubic.UVIndices, blockLocalPos, block.MainTexture, surfaceTool);

            if (isTransparent || IsTransparent(typeGetter(blockWorldPos + Vector3I.Right)))
                DrawRect(Cubic.Vertices, Cubic.Right, Cubic.UVCoords, Cubic.UVIndices, blockLocalPos, block.MainTexture, surfaceTool);

            if (isTransparent || IsTransparent(typeGetter(blockWorldPos + Vector3I.Forward)))
                DrawRect(Cubic.Vertices, Cubic.Front, Cubic.UVCoords, Cubic.UVIndices, blockLocalPos, block.MainTexture, surfaceTool);

            if (isTransparent || IsTransparent(typeGetter(blockWorldPos + Vector3I.Back)))
                DrawRect(Cubic.Vertices, Cubic.Back, Cubic.UVCoords, Cubic.UVIndices, blockLocalPos, block.MainTexture, surfaceTool);
        }

        void DrawRect(Vector3[] vertices, int[] indices, Vector2[] uvs, int[] uvIndices, Vector3I blockLocalPos, Texture2D texture, SurfaceTool surfaceTool)
        {
            if (!_textureUVOffsetDict.TryGetValue(texture, out var uvOffset))
                return;

            var a = vertices[indices[0]] + blockLocalPos;
            var b = vertices[indices[1]] + blockLocalPos;
            var c = vertices[indices[2]] + blockLocalPos;
            var d = vertices[indices[3]] + blockLocalPos;

            var uvA = uvs[uvIndices[0]] * _uvSize + uvOffset;
            var uvB = uvs[uvIndices[1]] * _uvSize + uvOffset;
            var uvC = uvs[uvIndices[2]] * _uvSize + uvOffset;
            var uvD = uvs[uvIndices[3]] * _uvSize + uvOffset;

            var triangle1 = new Vector3[] { a, b, c };
            var triangle2 = new Vector3[] { a, c, d };

            var uvTriangle1 = new Vector2[] { uvA, uvB, uvC };
            var uvTriangle2 = new Vector2[] { uvA, uvC, uvD };

            var normal = (((Vector3)(c - a)).Cross((Vector3)(b - a))).Normalized();
            var normals = new Vector3[] { normal, normal, normal };

            surfaceTool.AddTriangleFan(triangle1, uvTriangle1, normals: normals);
            surfaceTool.AddTriangleFan(triangle2, uvTriangle2, normals: normals);
        }
    }

}