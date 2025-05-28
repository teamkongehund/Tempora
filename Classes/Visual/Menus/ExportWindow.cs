using Godot;
using System;
using System.Runtime.CompilerServices;
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
    CheckBox addExtraPointsOnDownbeats = null!;

    [Export]
    CheckBox addExtraPointsOnQuarterNotes = null!;

    [Export]
    CheckBox omitBarlines = null!;

    [Export]
    CheckBox preventDoubleBarlines = null!;

    [Export]
    OptionButton beatSaberExportFormat = null!;

    [Export]
    Button okButton = null!;

    [Export]
    Button defaultsButton = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        okButton.Pressed += OnOkButtonPressed;
        defaultsButton.Pressed += OnResetButtonPressed;

        CloseRequested += CloseWithoutSaving;
        AboutToPopup += OnAboutToPopup;
    }

    private void LoadValuesFromSettings(Settings settings)
    {
        exportOffsetEdit.Text = settings.ExportOffsetMs.ToString();
        unsupportedTimeSignatures.ButtonPressed = settings.MeasureResetsOnUnsupportedTimeSignatures;
        removePointsThatChangeNothing.ButtonPressed = settings.RemovePointsThatChangeNothing;
        addExtraPointsOnDownbeats.ButtonPressed = settings.AddExtraPointsOnDownbeats;
        addExtraPointsOnQuarterNotes.ButtonPressed = settings.AddExtraPointsOnQuarterNotes;
        omitBarlines.ButtonPressed = settings.OmitBarlines;
        preventDoubleBarlines.ButtonPressed = settings.PreventDoubleBarlines;
        beatSaberExportFormat.Select(beatSaberExportFormat.GetItemIndex(settings.BeatSaberExportFormat));
    }

    private void OnAboutToPopup()
    {
        LoadValuesFromSettings(Settings.Instance);
    }

    private void OnOkButtonPressed()
    {
        SaveSettings(Settings.Instance);
        Hide();
        Settings.Instance.SaveSettings();
        Project.Instance.NotificationMessage = "Saved export options.";
    }

    private void OnResetButtonPressed()
    {
        ResetToDefaults();
    }

    private void SaveSettings(Settings settings)
    {
        bool exportOffsetParsed = int.TryParse(exportOffsetEdit.Text, out int exportOffset);
        if (exportOffsetParsed)
        {
            settings.ExportOffsetMs = exportOffset;
        }

        settings.MeasureResetsOnUnsupportedTimeSignatures = unsupportedTimeSignatures.ButtonPressed;
        settings.RemovePointsThatChangeNothing = removePointsThatChangeNothing.ButtonPressed;
        settings.AddExtraPointsOnDownbeats = addExtraPointsOnDownbeats.ButtonPressed;
        settings.AddExtraPointsOnQuarterNotes = addExtraPointsOnQuarterNotes.ButtonPressed;
        settings.OmitBarlines = omitBarlines.ButtonPressed;
        settings.PreventDoubleBarlines = preventDoubleBarlines.ButtonPressed;
        settings.BeatSaberExportFormat = beatSaberExportFormat.GetSelectedId();
    }

    private void ResetToDefaults()
    {
        exportOffsetEdit.Text = (-14).ToString();
        unsupportedTimeSignatures.ButtonPressed = true;
        removePointsThatChangeNothing.ButtonPressed = true;
        addExtraPointsOnDownbeats.ButtonPressed = true;
        addExtraPointsOnQuarterNotes.ButtonPressed = true;
        omitBarlines.ButtonPressed = true;
        preventDoubleBarlines.ButtonPressed = true;
        beatSaberExportFormat.Selected = 0;
    }

    private void CloseWithoutSaving()
    {
        Hide();
        Project.Instance.NotificationMessage = "Export options were not saved.";
    }
}
