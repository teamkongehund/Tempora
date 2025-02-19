using Godot;
using System;
using Tempora.Classes.Utility;

public partial class Stepper : HBoxContainer
{
    [Export]
    Button incrementButton = null!;

    [Export]
    public Label ValueLabel = null!;
    
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

    public bool HasMouseInside
    {
        get => hasMouseInside;
        set
        {
            if (hasMouseInside == value)
                return;
            hasMouseInside = value;
            if (!value) 
                MouseExitedStepper?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ValueModified = null;

    public event EventHandler? ValueIncremented = null;

    public event EventHandler? ValueDecremented = null;

    private int displayedValue = 0;

    private bool hasMouseInside = false;

    public event EventHandler? MouseExitedStepper = null;

    public bool HandleChangesElsewhere = false;

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
        HasMouseInside = rectangle.HasPoint(localMousePosition + Position);

        decrementButton.Visible = HasMouseInside;
        incrementButton.Visible = HasMouseInside;
    }

    protected virtual void OnIncrementButtonPressed()
    {
        if (!HandleChangesElsewhere)
        {
            int newValue = displayedValue + 1;
            DisplayedValue = newValue;
        }
        ValueIncremented?.Invoke(this, EventArgs.Empty);

    }
    protected virtual void OnDecrementButtonPressed()
    {
        if (!HandleChangesElsewhere)
        {
            int newValue = displayedValue - 1;
            DisplayedValue = newValue;
        }
        ValueDecremented?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void DisplayValue(int value) => ValueLabel.Text = value.ToString();
}
