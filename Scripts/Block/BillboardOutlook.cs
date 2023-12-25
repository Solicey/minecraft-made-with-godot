using Godot;
using System;

namespace MC
{
    public class BillboardOutlook : IBlockOutlook
    {
        static readonly float _offset = (Mathf.Sqrt2 - 1f) / (2f * Mathf.Sqrt2);

        public readonly Vector3[] Vertices = new Vector3[]
        {
            new Vector3(_offset, 0f, _offset),
            new Vector3(1f - _offset, 0f, _offset),
            new Vector3(_offset, 1f, _offset),
            new Vector3(1f - _offset, 1f, _offset),
            new Vector3(_offset, 0f, 1f - _offset),
            new Vector3(1f - _offset, 0f, 1f - _offset),
            new Vector3(_offset, 1f, 1f - _offset),
            new Vector3(1f - _offset, 1f, 1f - _offset),
        };

        public readonly Vector2[] UVCoords = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        };

        public readonly int[] Face1 = new int[] { 2, 0, 5, 7 };
        public readonly int[] Face2 = new int[] { 3, 1, 4, 6 };
        public readonly int[] UVIndices = new int[] { 0, 1, 2, 3 };

        public void Draw(Vector3I blockLocalPos, Vector3I blockWorldPos, Vector3 offset, Vector3 scale, Block block, SurfaceTool surfaceTool, BlockTypeGetter typeGetter, BlockGetter blockGetter, RectDrawer rectDrawer)
        {
            var isUpTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Up)).IsTransparent;
            var isDownTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Down)).IsTransparent;
            var isLeftTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Left)).IsTransparent;
            var isRightTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Right)).IsTransparent;
            var isForwardTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Forward)).IsTransparent;
            var isBackTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Back)).IsTransparent;

            if (!isUpTransparent && !isDownTransparent && !isLeftTransparent && !isRightTransparent && !isForwardTransparent && !isBackTransparent)
                return;

            rectDrawer(Vertices, Face1, UVCoords, UVIndices, blockLocalPos + offset, scale, block.MainTexture, surfaceTool);
            rectDrawer(Vertices, Face2, UVCoords, UVIndices, blockLocalPos + offset, scale, block.MainTexture, surfaceTool);
        }
    }
}
