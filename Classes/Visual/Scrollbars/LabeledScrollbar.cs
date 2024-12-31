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

namespace Tempora.Classes.Visual;

public partial class LabeledScrollbar : VBoxContainer
{
    [Export]
    protected bool showTitleInside;
    [Export]
    protected string title = "(Title)";
    [Export]
    protected double minValue = 0;
    [Export]
    protected double maxValue = 1;
    [Export]
    protected double step = 0.01;

    protected Label outsideTitleLabel = null!;
    protected Label insideTitleLabel = null!;
    protected HScrollBar hScrollBar = null!;
    protected Label valueLabel = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        outsideTitleLabel = GetNode<Label>("OutsideTitleLabel");
        hScrollBar = GetNode<HScrollBar>("HScrollBar");
        valueLabel = hScrollBar.GetNode<Label>("HBoxContainer/ValueLabel");
        insideTitleLabel = hScrollBar.GetNode<Label>("HBoxContainer/InsideTitleLabel");

        hScrollBar.ValueChanged += OnValueChanged;

        SetInitialValue();
        UpdateValueLabel();

        hScrollBar.MinValue = minValue;
        hScrollBar.MaxValue = maxValue;
        hScrollBar.Step = step;
        outsideTitleLabel.Text = title;
        insideTitleLabel.Text = $"{title}:";

        outsideTitleLabel.Visible = !showTitleInside;
        insideTitleLabel.Visible = showTitleInside;
    }

    protected virtual void OnValueChanged(double value)
    {
        UpdateValueLabel();
        UpdateTarget();
    }

    protected virtual void UpdateValueLabel() => valueLabel.Text = hScrollBar.Value.ToString("0.00");

    protected virtual void UpdateTarget() => GD.Print("No value updated. UpdateValue() must be overridden.");

    protected virtual void SetInitialValue() => hScrollBar.Value = hScrollBar.MinValue;

    //private void OnHScrollBarChanged()
    //{
    //    GD.Print($"Min = {hScrollBar.MinValue}, Max = {hScrollBar.MaxValue}, Step = {hScrollBar.Step}, Value = {hScrollBar.Value}");
    //}
}