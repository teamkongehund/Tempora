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

using System;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class TimeSignatureLineEdit : LineEdit
{
    public event EventHandler TimeSignatureSubmitted = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => TextSubmitted += OnTextSubmitted;

    private void OnTextSubmitted(string text)
    {
        string[] textSplit = text.Split("/", 2);

        if (textSplit.Length != 2)
            return;

        bool upperIsInt = int.TryParse(textSplit[0], out int upper);
        bool lowerIsInt = int.TryParse(textSplit[1], out int lower);

        if (!upperIsInt || !lowerIsInt)
            return;

        if (upper < 1)
            return;

        int[] timeSignature = [upper, lower];

        ReleaseFocus();
        TimeSignatureSubmitted?.Invoke(this, new GlobalEvents.ObjectArgument<int[]>(timeSignature));
    }
}