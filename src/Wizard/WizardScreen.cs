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
        var statusText = CreateText(_wizard.statusJSON, true);
        statusText.height = 980;

        var nextButton = CreateButton("Start Wizard >", true);
        nextButton.button.onClick.AddListener(() =>
        {
            if (_wizard.isRunning)
                _wizard.Next();
            else
                _wizard.StartWizard();
        });
        nextButton.buttonColor = Color.green;

        var skipButton = CreateButton("Skip Step >", true);
        skipButton.button.onClick.AddListener(() => _wizard.Skip());
        skipButton.buttonColor = Color.gray;

        var stopButton = CreateButton("Stop Wizard", true);
        stopButton.button.onClick.AddListener(() => _wizard.StopWizard("The wizard was stopped."));
        stopButton.buttonColor = Color.red;

        _onStatusChanged = isRunning =>
        {
            nextButton.label = isRunning ? "Next (Press [A] Anywhere) >" : "Start Wizard >";
            stopButton.button.interactable = isRunning;
            skipButton.button.interactable = isRunning;
            context.embody.activeToggle.toggle.interactable = !isRunning;
        };
        _wizard.statusChanged.AddListener(_onStatusChanged);
        _onStatusChanged(_wizard.isRunning);
    }

    public override void Hide()
    {
        base.Hide();

        _wizard.StopWizard();
        _wizard.statusChanged.RemoveListener(_onStatusChanged);
        context.embody.activeToggle.toggle.interactable = true;
    }
}
