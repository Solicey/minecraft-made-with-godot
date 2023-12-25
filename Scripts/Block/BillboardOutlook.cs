using Godot;
using System;

namespace MC
{
    public class BillboardOutlook : IBlockOutlook
    {
        public readonly float Offset = (Mathf.Sqrt2 - 1f) / (2f * Mathf.Sqrt2);

        public readonly Vector3[] Vertices;

        BillboardOutlook() 
        {
            Vertices = new Vector3[]
            {
                new Vector3(Offset, 0f, Offset),
                new Vector3(1f - Offset, 0f, Offset),
                new Vector3(Offset, 1f, Offset),
                new Vector3(1f - Offset, 1f, Offset),
                new Vector3(Offset, 0f, 1f - Offset),
                new Vector3(1f - Offset, 0f, 1f - Offset),
                new Vector3(Offset, 1f, 1f - Offset),
                new Vector3(1f - Offset, 1f, 1f - Offset),
            };
        }

        public void Draw(Vector3I blockLocalPos, Vector3I blockWorldPos, Vector3 offset, Vector3 scale, Block block, SurfaceTool surfaceTool, BlockTypeGetter typeGetter, BlockGetter blockGetter, RectDrawer rectDrawer)
        {
            
        }
    }
}
