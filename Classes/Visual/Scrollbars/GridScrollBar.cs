// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System.Collections.Generic;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class GridScrollBar : LabeledScrollbarHorizontal
{
    protected static readonly Dictionary<int, string> GridDivisorToLabelDict = new() {
        { 1, "4/4" },
        { 2, "2/4" },
        { 3, "1/3" },
        { 4, "1/4" },
        { 6, "1/6" },
        { 8, "1/8" },
        { 12, "1/12" },
        { 16, "1/16" }
    };

    protected override void UpdateValueLabel()
    {
        var divisor = Settings.GridSliderToDivisorDict[(int)hScrollBar.Value];
        var label = GridDivisorToLabelDict[divisor];
        valueLabel.Text = label;
    }

    protected override void UpdateTarget() => Settings.Instance.GridDivisor = Settings.GridSliderToDivisorDict[(int)hScrollBar.Value];

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.GridDivisor;
}