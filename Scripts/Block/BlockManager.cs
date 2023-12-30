using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MC
{
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

    public delegate void RectDrawer(Vector3[] vertices, int[] indices, Vector2[] uvs, int[] uvIndices, Vector3 offset, Vector3 scale, Texture2D texture, SurfaceTool surfaceTool);

    public delegate Block BlockGetter(BlockType type);

    public partial class BlockManager : Node
    {
        public Dictionary<MaterialType, StandardMaterial3D> MaterialMap { get; private set; } = new();

        Dictionary<BlockType, Block> _blockDict = new();
        Dictionary<Texture2D, Vector2> _textureUVOffsetDict = new();

        const int _atlasWidth = 4;
        int _atlasHeight = 0;

        float UVWidth { get { return 1f / _atlasWidth; } }
        float UVHeight { get { return 1f / _atlasHeight; } }
        Vector2 UVSize { get { return new Vector2(UVWidth, UVHeight); } }

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

        Dictionary<Outlook, IBlockOutlook> _outlookDict = new();

        public override void _Ready()
        {
            LoadBlocks();
            DrawAtlas();
            CreateMaterial();
            FillOutlookDict();
        }

        public Block GetBlock(BlockType type)
        {
            if (!_blockDict.TryGetValue(type, out var block))
                return _blockDict.FirstOrDefault().Value;
            return block;
        }

        public void DrawMesh(BlockType blockType, Vector3I blockLocalPos, Vector3I blockWorldPos, Vector3 offset, Vector3 scale, Dictionary<MaterialType, SurfaceTool> surfaceTools, BlockTypeGetter typeGetter)
        {
            if (blockType == BlockType.Air)
                return;

            var block = GetBlock(blockType);
            if (block == null)
                return;

            if (!_outlookDict.TryGetValue(block.Outlook, out var outlook))
                return;

            if (!surfaceTools.TryGetValue(block.MaterialType, out var surfaceTool)) 
                return;

            outlook.Draw(blockLocalPos, blockWorldPos, offset, scale, block, surfaceTool, typeGetter, GetBlock, DrawRect);
        }

        public void DrawCollider(BlockType blockType, Vector3I blockLocalPos, Vector3I blockWorldPos, Vector3 offset, Vector3 scale, Dictionary<ColliderType, SurfaceTool> surfaceTools, BlockTypeGetter typeGetter)
        {
            if (blockType == BlockType.Air)
                return;

            var block = GetBlock(blockType);
            if (block == null)
                return;

            if (!_outlookDict.TryGetValue(block.UseCustomCollider ? block.CustomColliderOutlook : block.Outlook, out var outlook))
                return;

            if (!surfaceTools.TryGetValue(block.ColliderType, out var surfaceTool))
                return;

            outlook.Draw(blockLocalPos, blockWorldPos, offset, scale, block, surfaceTool, typeGetter, GetBlock, DrawRect);
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
            MaterialMap.Add(MaterialType.Opaque, new StandardMaterial3D()
            {
                AlbedoTexture = _atlasTexture,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled,
            });
            MaterialMap.Add(MaterialType.AlphaClip, new StandardMaterial3D()
            {
                AlbedoTexture = _atlasTexture,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled,
                Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            });
            MaterialMap.Add(MaterialType.AlphaBlend, new StandardMaterial3D()
            {
                AlbedoTexture = _atlasTexture,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            });
        }

        void DrawRect(Vector3[] vertices, int[] indices, Vector2[] uvs, int[] uvIndices, Vector3 offset, Vector3 scale, Texture2D texture, SurfaceTool surfaceTool)
        {
            if (!_textureUVOffsetDict.TryGetValue(texture, out var uvOffset))
                return;

            var a = vertices[indices[0]] + offset;
            var b = vertices[indices[1]] + offset;
            var c = vertices[indices[2]] + offset;
            var d = vertices[indices[3]] + offset;

            var uvA = uvs[uvIndices[0]] * UVSize + uvOffset;
            var uvB = uvs[uvIndices[1]] * UVSize + uvOffset;
            var uvC = uvs[uvIndices[2]] * UVSize + uvOffset;
            var uvD = uvs[uvIndices[3]] * UVSize + uvOffset;

            var triangle1 = new Vector3[] { a, b, c };
            var triangle2 = new Vector3[] { a, c, d };

            var uvTriangle1 = new Vector2[] { uvA, uvB, uvC };
            var uvTriangle2 = new Vector2[] { uvA, uvC, uvD };

            var normal = (((Vector3)(c - a)).Cross((Vector3)(b - a))).Normalized();
            var normals = new Vector3[] { normal, normal, normal };

            for (int i = 0; i < triangle1.Length; i++)
            {
                surfaceTool.SetNormal(normals[i]);
                surfaceTool.SetUV(uvTriangle1[i]);
                surfaceTool.AddVertex(triangle1[i]);
            }

            for (int i = 0; i < triangle2.Length; i++)
            {
                surfaceTool.SetNormal(normals[i]);
                surfaceTool.SetUV(uvTriangle2[i]);
                surfaceTool.AddVertex(triangle2[i]);
            }
        }

        void AddSurfaceToArray(Vector3[] verts, Vector2[] uvs, Vector3[] normals)
        {
            var surfaceArray = new Godot.Collections.Array();
            surfaceArray.Resize((int)Mesh.ArrayType.Max);
        }

        // TODO
        void FillOutlookDict()
        {
            _outlookDict.Add(Outlook.Cubic, new CubicOutlook());
            _outlookDict.Add(Outlook.Billboard, new BillboardOutlook());
        }
    }

}