using Godot;
using System;

public partial class HelpButton : Button
{
    [Export]
    private Window helpWindow = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        helpWindow.Borderless = false;

        Pressed += OnPressed;
        helpWindow.CloseRequested += OnCloseRequested;
	}

    private void OnPressed()
    {
        helpWindow.Show();

        var viewPortSize = (Vector2I)GetViewport().GetVisibleRect().Size;
        helpWindow.Position = new Vector2I(viewPortSize.X / 2 - helpWindow.Size.X/2, viewPortSize.Y / 2 - helpWindow.Size.Y / 2);
        
        helpWindow.GrabFocus();
    }

    private void OnCloseRequested()
    {
        helpWindow.Hide();
    }
}
