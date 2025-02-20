using Godot;
using System;

public partial class BpmLabel : Label
{
    public event EventHandler? DoubleClicked = null;

    public override void _GuiInput(InputEvent @event)
    {
        //base._GuiInput(@event);
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.DoubleClick)
            {
                DoubleClicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
