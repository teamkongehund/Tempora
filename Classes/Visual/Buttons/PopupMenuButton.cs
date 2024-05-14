using Godot;
using System;

public partial class PopupMenuButton : Button
{
    [Export]
    private PopupMenu popupMenu = null!;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        ActionMode = ActionModeEnum.Press;
        Pressed += PopupOrHide;
	}

    private void PopupOrHide()
    {
        // This assumes button position is already in global coordinates. Maybe fix later if design changes.
        popupMenu.Position = (Vector2I)(Position + new Vector2(0, Size.Y));
        
        if (popupMenu.Visible)
            popupMenu.Hide();
        else
        {
            popupMenu.Show();
        }
    }
}
