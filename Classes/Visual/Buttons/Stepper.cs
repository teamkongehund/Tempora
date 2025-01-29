using Godot;
using System;

public partial class Stepper : CenterContainer

{
    [Export]
    Button incrementButton = null!;

    [Export]
    Label valueLabel = null!;
    
    [Export]
    Button decrementButton = null!;

    [Export]
    VBoxContainer vBoxContainer = null!;

    [Export]
    protected int increment = 1;

    [Export]
    protected int value = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        UpdateValue(value);
        incrementButton.Pressed += OnIncrementButtonPressed;
        decrementButton.Pressed += OnDecrementButtonPressed;
        //vBoxContainer.MouseExited += OnMouseExited;
        //vBoxContainer.MouseEntered += OnMouseEntered;
    }

    protected virtual void OnIncrementButtonPressed()
    {
        UpdateValueAndTarget(value + 1);
    }
    protected virtual void OnDecrementButtonPressed()
    {
        UpdateValueAndTarget(value - 1);
    }

    protected virtual void OnMouseExited()
    {
        incrementButton.Visible = false;
        decrementButton.Visible = false;
    }

    protected virtual void OnMouseEntered()
    {
        incrementButton.Visible = true;
        decrementButton.Visible = true;
    }

    protected void UpdateValueAndTarget(int value)
    {
        UpdateValue(value);
        UpdateTarget();
    }

    protected virtual void UpdateValue(int value)
    {
        this.value = value;
        valueLabel.Text = value.ToString();
    }

    protected virtual void UpdateTarget() => GD.Print("No value updated. UpdateValue() must be overridden.");
}
