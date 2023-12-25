using Godot;
using System;

namespace MC
{
    public interface IBlockOutlook
    {
        public void Draw(Vector3I blockLocalPos, Vector3I blockWorldPos, Vector3 offset, Vector3 scale, Block block, SurfaceTool surfaceTool, BlockTypeGetter typeGetter, BlockGetter blockGetter, RectDrawer rectDrawer);
    }

}