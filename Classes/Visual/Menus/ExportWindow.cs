using Godot;
using System;
using Tempora.Classes.Utility;

public partial class ExportWindow : Window
{
    [Export]
    LineEdit exportOffsetEdit = null!;

    [Export]
    CheckBox unsupportedTimeSignatures = null!;

    [Export]
    CheckBox removePointsThatChangeNothing = null!;

    [Export]
    CheckBox addExtraPointsOnDownbeats= null!;

    [Export]
    CheckBox addExtraPointsOnQuarterNotes= null!;

    [Export]
    CheckBox omitBarlines = null!;

    [Export]
    CheckBox preventDoubleBarlines = null!;

    [Export]
    Button okButton = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        exportOffsetEdit.Text = Settings.Instance.ExportOffsetMs.ToString();
        unsupportedTimeSignatures.ButtonPressed = Settings.Instance.unsupportedTimeSignatures;
        removePointsThatChangeNothing.ButtonPressed = Settings.Instance.removePointsThatChangeNothing;
        addExtraPointsOnDownbeats.ButtonPressed = Settings.Instance.addExtraPointsOnDownbeats;
        addExtraPointsOnQuarterNotes.ButtonPressed = Settings.Instance.addExtraPointsOnQuarterNotes;
        omitBarlines.ButtonPressed = Settings.Instance.omitBarlines;
        preventDoubleBarlines.ButtonPressed = Settings.Instance.preventDoubleBarlines;

        okButton.Pressed += OnOkButtonPressed;
	}

	private void OnOkButtonPressed()
    {
        SaveSettings();
    }

    private void SaveSettings()
    {
        bool exportOffsetParsed = int.TryParse(exportOffsetEdit.Text, out int exportOffset);
        if (exportOffsetParsed)
        {
            Settings.Instance.ExportOffsetMs = exportOffset;
        }
        Settings.Instance.unsupportedTimeSignatures = unsupportedTimeSignatures.ButtonPressed;
        Settings.Instance.removePointsThatChangeNothing = removePointsThatChangeNothing.ButtonPressed;
        Settings.Instance.addExtraPointsOnDownbeats = addExtraPointsOnDownbeats.ButtonPressed;
        Settings.Instance.addExtraPointsOnQuarterNotes = addExtraPointsOnQuarterNotes.ButtonPressed;
        Settings.Instance.omitBarlines = omitBarlines.ButtonPressed;
        Settings.Instance.preventDoubleBarlines = preventDoubleBarlines.ButtonPressed;
    }
}
