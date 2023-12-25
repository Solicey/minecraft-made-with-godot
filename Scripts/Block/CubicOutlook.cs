using Godot;
using System;

namespace MC
{
    public class CubicOutlook : IBlockOutlook
    {
        public readonly Vector3[] Vertices = new Vector3[]
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

        public readonly Vector2[] UVCoords = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        };

        public readonly int[] Top = new int[] { 2, 3, 7, 6 };
        public readonly int[] Bottom = new int[] { 0, 4, 5, 1 };
        public readonly int[] Left = new int[] { 6, 4, 0, 2 };
        public readonly int[] Right = new int[] { 3, 1, 5, 7 };
        public readonly int[] Back = new int[] { 7, 5, 4, 6 };
        public readonly int[] Front = new int[] { 2, 0, 1, 3 };
        public readonly int[] UVIndices = new int[] { 0, 1, 2, 3 };

        public void Draw(Vector3I blockLocalPos, Vector3I blockWorldPos, Vector3 offset, Vector3 scale, Block block, SurfaceTool surfaceTool, BlockTypeGetter typeGetter, BlockGetter blockGetter, RectDrawer rectDrawer)
        {
            bool isTransparent = block.IsTransparent;

            var isUpTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Up)).IsTransparent;
            var isDownTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Down)).IsTransparent;
            var isLeftTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Left)).IsTransparent;
            var isRightTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Right)).IsTransparent;
            var isForwardTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Forward)).IsTransparent;
            var isBackTransparent = blockGetter(typeGetter(blockWorldPos + Vector3I.Back)).IsTransparent;

            if (!isUpTransparent && !isDownTransparent && !isLeftTransparent && !isRightTransparent && !isForwardTransparent && !isBackTransparent)
                return;

            if (isTransparent || isUpTransparent)
                rectDrawer(Vertices, Top, UVCoords, UVIndices, blockLocalPos + offset, scale, block.TopTexture ?? block.MainTexture, surfaceTool);

            if (isTransparent || isDownTransparent)
                rectDrawer(Vertices, Bottom, UVCoords, UVIndices, blockLocalPos + offset, scale, block.BottomTexture ?? block.MainTexture, surfaceTool);

            if (isTransparent || isLeftTransparent)
                rectDrawer(Vertices, Left, UVCoords, UVIndices, blockLocalPos + offset, scale, block.MainTexture, surfaceTool);

            if (isTransparent || isRightTransparent)
                rectDrawer(Vertices, Right, UVCoords, UVIndices, blockLocalPos + offset, scale, block.MainTexture, surfaceTool);

            if (isTransparent || isForwardTransparent)
                rectDrawer(Vertices, Front, UVCoords, UVIndices, blockLocalPos + offset, scale, block.MainTexture, surfaceTool);

            if (isTransparent || isBackTransparent)
                rectDrawer(Vertices, Back, UVCoords, UVIndices, blockLocalPos + offset, scale, block.MainTexture, surfaceTool);
        }
    }
}
