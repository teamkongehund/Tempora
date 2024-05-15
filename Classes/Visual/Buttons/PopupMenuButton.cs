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
        if (popupMenu.Visible)
            popupMenu.Hide();
        else
        {
            popupMenu.Show();
            popupMenu.Position = (Vector2I)(GlobalPosition + new Vector2(0, Size.Y));
        }
    }
}
