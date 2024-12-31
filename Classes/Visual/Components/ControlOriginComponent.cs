// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share — copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution — You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial — You may not use the material for commercial purposes.
// - NoDerivatives — If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using Godot;

public partial class ControlOriginComponent : Node
{
    [Export]
    private Control targetControl = null!;

    [Export]
    private Anchor anchor = Anchor.TopRight;

    /// <summary>
    /// Absolute position of anchor relative to target's parent. This is what will be changed when external classes change target's <see cref="Control.Position"/>
    /// </summary>
    private Vector2 anchorPosition = new Vector2(0, 0);

    private Vector2 modifiedPosition = new Vector2(0, 0);

    private enum Anchor
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        GetViewport().Ready += OnTreeReady;
	}

    private void OnTreeReady()
    {
        targetControl.ItemRectChanged += OnItemRectChanged;
    }

    private void OnItemRectChanged()
    {
        if (targetControl.Position == modifiedPosition)
            return; // Position change came from this class. 

        anchorPosition = targetControl.Position;

        AdjustTransform();
    }

    /// <summary>
    /// Adjust Target Control's Position and Pivot Offset according to chosen <see cref="Anchor"/>.
    /// Godot works by first setting the top left corner of the <see cref="Control"/> to <see cref="Control.Position"/>, then the coordinates of <see cref="Control.PivotOffset"/> is relative to this position.
    /// We compensate for this such that the PivotOffset stays at (0,0) in final coordinates, and this pivot point corresponds with the chosen <see cref="Anchor"/>. 
    /// </summary>
    private void AdjustTransform()
    {
        float scaleX = GetXMultiplier(anchor);
        float scaleY = GetYMultiplier(anchor);
        var positionOffset = new Vector2(targetControl.Size.X * scaleX, targetControl.Size.Y * scaleY);
        modifiedPosition = positionOffset + anchorPosition;
        //GD.Print($"Changed control from {targetControl.Position} to {modifiedPosition}");
        targetControl.Position = modifiedPosition;
        targetControl.PivotOffset = -positionOffset;
    }

    private float GetXMultiplier(Anchor origin)
    {
        if (origin == Anchor.TopLeft || origin == Anchor.Left || origin == Anchor.BottomLeft)
            return 0;
        else if (origin == Anchor.Top || origin == Anchor.Center || origin == Anchor.Bottom)
            return -0.5f;
        else if (origin == Anchor.TopRight || origin == Anchor.Right || origin == Anchor.BottomRight)
            return -1;
        return 0;
    }

    private float GetYMultiplier(Anchor origin)
    {
        if (origin == Anchor.TopLeft || origin == Anchor.Top || origin == Anchor.TopRight)
            return 0;
        else if (origin == Anchor.Left || origin == Anchor.Center || origin == Anchor.Right)
            return -0.5f;
        else if (origin == Anchor.BottomLeft || origin == Anchor.Bottom || origin == Anchor.BottomRight)
            return -1;
        return 0;
    }
}
