using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public interface IWizard : IEmbodyModule
{
    WizardStatusChangedEvent statusChanged { get; }
    JSONStorableString statusJSON { get; }
    bool isRunning { get; }
    void StartWizard();
    void StopWizard(string message);
    void Next();
    void Skip();
}

public class WizardStatusChangedEvent : UnityEvent<bool> { }

public class WizardModule : EmbodyModuleBase, IWizard
{
    public const string Label = "Wizard";
    public override string storeId => "Wizard";
    public override string label => Label;
    public override bool skipChangeEnabledWhenActive => true;

    public WizardStatusChangedEvent statusChanged { get; } = new WizardStatusChangedEvent();
    public JSONStorableString statusJSON { get; } = new JSONStorableString("WizardStatus", "");
    public bool isRunning => _coroutine != null;

    private Coroutine _coroutine;
    private IWizardStep _step;
    private bool _next;
    private bool _skip;

    public override void Awake()
    {
        base.Awake();

        statusJSON.val = "Select 'Start' to launch the wizard.";
    }

    public void StartWizard()
    {
        if (_coroutine != null) StopWizard("");
        enabledJSON.val = true;
        _coroutine = StartCoroutine(StartWizardCo());
        statusChanged.Invoke(true);
    }

    public void StopWizard(string message)
    {
        if (!enabledJSON.val) return;

        StopCoroutine(_coroutine);
        _coroutine = null;

        if (_step != null)
        {
            _step.Leave();
            _step = null;
        }

        statusJSON.val = message;
        _next = false;
        _skip = false;
        statusChanged.Invoke(false);

        // TODO: This ugly fix is for when you're canceling during Snug setup, we need some way to restore the hands after. Maybe a cleanup step would be better.
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;

        enabledJSON.val = false;
    }

    public void Next()
    {
        _next = true;
    }

    public void Skip()
    {
        _skip = true;
    }

    private IEnumerator StartWizardCo()
    {
        context.embody.activeJSON.val = false;
        context.Initialize();

        yield return 0;

        SuperController.singleton.worldScale = 1f;

        context.embody.presetsJSON.val = "Improved Possession";

        var steps = BuildSteps();

        for (var i = 0; i < steps.Count; i++)
        {
            _step = steps[i];
            statusJSON.val = $"Step {i + 1} / {steps.Count}\n\n{_step.helpText}";
            _step.Enter();
            while (!AreAnyStartRecordKeysDown())
            {
                ReopenIfClosed();

                if (_skip)
                    break;

                yield return 0;
                _step.Update();
            }

            ReopenIfClosed();

            try
            {
                if (_skip)
                    _skip = false;
                else
                    _step.Apply();
            }
            finally
            {
                _step.Leave();
            }

            yield return 0;
        }

        StopWizard("All done! You can now activate Embody.\n\nYou can tweak your settings or start this wizard again. You can make this setup your default in the Import/Export screen. Default settings will automatically apply whenever you load this plugin on an atom.");
    }

    private void ReopenIfClosed()
    {
        if (context.plugin.UITransform.gameObject.activeInHierarchy) return;
        SuperController.singleton.SelectController(containingAtom.mainController);
        SuperController.singleton.ShowMainHUDMonitor();
        var selector = containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
        selector.SetActiveTab("Plugins");
        context.plugin.UITransform.gameObject.SetActive(true);
    }

    private List<IWizardStep> BuildSteps()
    {
        var steps = new List<IWizardStep>();

        // NOTE: We use Count because we want to sync all available motion controls, not only the first one
        var useViveTrackers = context.trackers.viveTrackers.Count(t => t.SyncMotionControl());

        steps.Add(new ResetPoseStep(context, useViveTrackers == 0));

        steps.Add(new RecordPlayerHeightStep(context.worldScale));

        if (useViveTrackers > 0)
        {
            steps.Add(new ActivateHeadAndHandsStep(context));
            if (useViveTrackers > 1)
                steps.Add(new RecordViveTrackersFeetStep(context));
            if (useViveTrackers == 1 || useViveTrackers > 2)
                steps.Add(new RecordViveTrackersStep(context));
            steps.Add(new DeactivateStep(context));
        }

        steps.Add(new AskSnugStep(context, steps));

        steps.Add(new MakeDefaultsStep(context));

        return steps;
    }


    private bool AreAnyStartRecordKeysDown()
    {
        if (_next || _skip)
        {
            _next = false;
            return true;
        }
        if (Input.GetKey(KeyCode.Escape))
        {
            _skip = true;
            return true;
        }
        var sc = SuperController.singleton;
        if (sc.isOVR)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.Touch)) return true;
            if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.Touch)) return true;
        }
        if (sc.isOpenVR)
        {
            if (sc.selectAction.stateDown) return true;
        }
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        return false;
    }
}
