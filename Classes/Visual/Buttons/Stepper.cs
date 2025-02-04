using Godot;
using System;
using Tempora.Classes.Utility;

public partial class Stepper : Control
{
    [Export]
    Button incrementButton = null!;

    [Export]
    protected Label valueLabel = null!;
    
    [Export]
    Button decrementButton = null!;

    [Export]
    Control mouseAreaControl = null!;

    [Export]
    protected int increment = 1;

    [Export]
    public virtual int DisplayedValue
    {
        get => displayedValue;
        set
        {
            displayedValue = value;
            DisplayValue(value);
        }
    }

    public event EventHandler? ValueModified = null;

    private int displayedValue = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        DisplayValue(displayedValue);
        incrementButton.Pressed += OnIncrementButtonPressed;
        decrementButton.Pressed += OnDecrementButtonPressed;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        Vector2 localMousePosition = GetLocalMousePosition();
        Rect2 rectangle = mouseAreaControl.GetRect();
        bool hasMouseInside = rectangle.HasPoint(localMousePosition + Position);

        decrementButton.Visible = hasMouseInside;
        incrementButton.Visible = hasMouseInside;
    }

    protected virtual void OnIncrementButtonPressed()
    {
        int newValue = displayedValue + 1;
        ModifyValue(newValue);
        
    }
    protected virtual void OnDecrementButtonPressed()
    {
        int newValue = displayedValue - 1;
        ModifyValue(newValue);
    }

    protected void ModifyValue(int value)
    {
        DisplayedValue = value;
        ValueModified?.Invoke(this, new GlobalEvents.ObjectArgument<int>(value));
    }

    protected virtual void DisplayValue(int value) => valueLabel.Text = value.ToString();
}
