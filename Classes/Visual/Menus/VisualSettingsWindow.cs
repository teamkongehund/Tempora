using Godot;
using System;
using System.Runtime.CompilerServices;
using Tempora.Classes.Utility;

public partial class VisualSettingsWindow : Window
{
    [Export]
    LineEdit stepSizeLineEdit = null!;

    [Export]
    LineEdit fftSizeLineEdit = null!;

    [Export]
    LineEdit maxFreqLineEdit = null!;

    [Export]
    LineEdit intensityLineEdit = null!;

    [Export]
    CheckBox dBCheckbox = null!;

    [Export]
    Button okButton = null!;

    [Export]
    Button defaultsButton = null!;

    [Export]
    Button applyButton = null!;


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
        stepSizeLineEdit.Text = settings.ExportOffsetMs.ToString();
        fftSizeLineEdit.Text = settings.SpectrogramFftSize.ToString();
        maxFreqLineEdit.Text = settings.SpectrogramMaxFreq.ToString();
        intensityLineEdit.Text = settings.SpectrogramIntensity.ToString();
        dBCheckbox.ButtonPressed = settings.SpectrogramUseDb;
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
        bool stepSizeParsed = int.TryParse(stepSizeLineEdit.Text, out int stepSize);
        if (stepSizeParsed)
            settings.SpectrogramStepSize = stepSize;

        bool fftSizeParsed = int.TryParse(fftSizeLineEdit.Text, out int fftSize);
        if (fftSizeParsed)
            settings.SpectrogramFftSize = fftSize;

        bool maxFreqParsed = int.TryParse(maxFreqLineEdit.Text, out int maxFreq);
        if (maxFreqParsed)
            settings.SpectrogramMaxFreq = maxFreq;

        bool intensityParsed = int.TryParse(intensityLineEdit.Text, out int intensity);
        if (intensityParsed)
            settings.SpectrogramIntensity = intensity;

        settings.SpectrogramUseDb = dBCheckbox.ButtonPressed;
    }

    private void ResetToDefaults()
    {
        stepSizeLineEdit.Text = 64.ToString();
        fftSizeLineEdit.Text = 256.ToString();
        maxFreqLineEdit.Text = 2200.ToString();
        intensityLineEdit.Text = 5.ToString();
        dBCheckbox.ButtonPressed = true;
    }

    private void CloseWithoutSaving()
    {
        Hide();
        Project.Instance.NotificationMessage = "Export options were not saved.";
    }
}
