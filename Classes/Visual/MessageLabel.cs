using Godot;
using OsuTimer.Classes.Utility;
using System;

namespace OsuTimer.Classes.Visual;

public partial class MessageLabel : Label
{
    [Export]
    private Timer timer = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        timer.Timeout += HideMessage;
        Project.Instance.NotificationMessageChanged += OnNotificationMessageChanged;
	}

    private void OnNotificationMessageChanged(object? sender, EventArgs e) => DisplayMessage();

    private void DisplayMessage()
    {
        Text = Project.Instance.NotificationMessage;
        timer.Start();
        Visible = true;
    }

    private void HideMessage()
    {
        Visible = false;
    }
}
