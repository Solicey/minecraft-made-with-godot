using Godot;
using System;

namespace MC
{
    public partial class OptionsUI : Control
    {
        [Signal] public delegate void ReturnButtonPressedEventHandler(bool shouldReset);

        [Export] HScrollBar _chunkRenderDistanceScrollBar;
        [Export] Button _returnButton;

        public override void _Ready()
        {
            _returnButton.Pressed += () => 
            {
                bool shouldReset = ((int)_chunkRenderDistanceScrollBar.Value != Global.ChunkRenderDistance);

                Global.ChunkRenderDistance = (int)_chunkRenderDistanceScrollBar.Value;

                EmitSignal(SignalName.ReturnButtonPressed, shouldReset);
            };

        }

        public override void _EnterTree()
        {
            _chunkRenderDistanceScrollBar.Value = Global.ChunkRenderDistance;
        }
    }
}
