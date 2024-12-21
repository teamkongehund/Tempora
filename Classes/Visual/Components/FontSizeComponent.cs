using Godot;
using System;
using Tempora.Classes.Utility;

public partial class FontSizeComponent : Node
{
    [Export]
    Control? Control;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        GlobalEvents.Instance.FontSizeUpdated += OnFontSizeUpdated;
        UpdateTargetFontSize(Settings.Instance.FontSize);
	}

    private void OnFontSizeUpdated(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<int> intArg)
            return;
        UpdateTargetFontSize(intArg.Value);
    }

    private void UpdateTargetFontSize(int size) => Control?.AddThemeFontSizeOverride("font_size", size);

}
