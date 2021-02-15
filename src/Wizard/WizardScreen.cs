using UnityEngine;
using UnityEngine.Events;

public class WizardScreen : ScreenBase, IScreen
{
    public const string ScreenName = WizardModule.Label;
    private readonly IWizard _wizard;
    private UnityAction<bool> _onStatusChanged;

    public WizardScreen(EmbodyContext context, IWizard wizard)
        : base(context)
    {
        _wizard = wizard;
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Automatically configures optimal settings for the current person and the selected modules."), true);

        var startButton = CreateButton("Start Wizard", true);
        startButton.button.onClick.AddListener(() => _wizard.StartWizard());
        startButton.buttonColor = Color.green;

        var statusText = CreateText(_wizard.statusJSON, true);
        statusText.height = 600;

        var nextButton = CreateButton("Next Step >", true);
        nextButton.button.onClick.AddListener(() => _wizard.Next());
        nextButton.buttonColor = Color.green;

        var skipButton = CreateButton("Skip Step >", true);
        skipButton.button.onClick.AddListener(() => _wizard.Skip());
        skipButton.buttonColor = Color.gray;

        var stopButton = CreateButton("Stop Wizard", true);
        stopButton.button.onClick.AddListener(() => _wizard.StopWizard("The wizard was stopped."));
        stopButton.buttonColor = Color.red;

        _onStatusChanged = isRunning =>
        {
            startButton.button.interactable = !isRunning;
            stopButton.button.interactable = isRunning;
            nextButton.button.interactable = isRunning;
            skipButton.button.interactable = isRunning;
        };
        _wizard.statusChanged.AddListener(_onStatusChanged);
        _onStatusChanged(_wizard.isRunning);
    }

    public override void Hide()
    {
        base.Hide();

        _wizard.StopWizard();
        _wizard.statusChanged.RemoveListener(_onStatusChanged);
    }
}
