using Godot;
using System;

namespace MC
{
    public partial class OptionsUI : Control
    {
        [Signal] public delegate void ReturnButtonPressedEventHandler();

        [Export] HScrollBar _chunkRenderDistanceScrollBar;
        [Export] Button _returnButton;
    }
}
