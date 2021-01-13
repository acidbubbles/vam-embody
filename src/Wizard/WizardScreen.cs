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

        var stopButton = CreateButton("Stop Wizard", true);
        stopButton.button.onClick.AddListener(() => _wizard.StopWizard("Stopped"));

        var statusText = CreateText(_wizard.statusJSON, true);
        statusText.height = 600;

        var nextButton = CreateButton("Next", true);
        nextButton.button.onClick.AddListener(() => _wizard.Next());

        _onStatusChanged = isRunning =>
        {
            startButton.button.interactable = !isRunning;
            stopButton.button.interactable = isRunning;
            nextButton.button.interactable = isRunning;
        };
        _wizard.statusChanged.AddListener(_onStatusChanged);
        _onStatusChanged(_wizard.isRunning);
    }

    public override void Hide()
    {
        base.Hide();

        _wizard.statusChanged.RemoveListener(_onStatusChanged);
    }
}
