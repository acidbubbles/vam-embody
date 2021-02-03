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
    public override bool alwaysEnabled => true;

    public WizardStatusChangedEvent statusChanged { get; } = new WizardStatusChangedEvent();
    public JSONStorableString statusJSON { get; } = new JSONStorableString("WizardStatus", "");
    public bool isRunning => _coroutine != null;

    private Coroutine _coroutine;
    private bool _next;
    private bool _skip;

    public override void Awake()
    {
        base.Awake();

        statusJSON.val = "";
    }

    public void StartWizard()
    {
        if (_coroutine != null) StopWizard("");
        _coroutine = StartCoroutine(StartWizardCo());
        statusChanged.Invoke(true);
    }

    public void StopWizard(string message)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        statusJSON.val = message;
        _next = false;
        _skip = false;
        statusChanged.Invoke(false);
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

        var autoSetup = new SnugAutoSetup(context.containingAtom, context.snug);
        autoSetup.AutoSetup();

        // ReSharper disable once UseObjectOrCollectionInitializer
        var steps = new List<IWizardStep>();

        steps.Add(new RecordPlayerHeightStep(context.worldScale));

        // NOTE: We use Count because we want to sync all available motion controls, not only the first one
        // ReSharper disable once ReplaceWithSingleCallToCount UseMethodAny.0
        if (context.trackers.viveTrackers.Where(t => t.SyncMotionControl()).Count() > 0)
        {
            steps.Add(new ActivateHeadAndHandsStep(context));
            steps.Add(new RecordViveTrackersStep(context));
            steps.Add(new DeactivateStep(context));
        }

        // TODO: Implement Snug wizard
        // TODO: Load pose
        steps.Add(new AskSnugStep(context));
        steps.Add(new ActivateWithoutSnugStep(context));
        steps.Add(new MeasureHandsPaddingStep(context));
        foreach (var anchor in context.snug.anchorPoints.Where(a => !a.locked && a.active))
        {
            steps.Add(new MeasureAnchorWidthStep(context, anchor));
            steps.Add(new MeasureAnchorDepthAndOffsetStep(context, anchor));
        }
        steps.Add(new DeactivateAndRestoreSnugStep(context));
        // steps.Add(new EnableSnugStep(context.embody, context.snug));

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            statusJSON.val = $"Step {i + 1} / {steps.Count}\n\n{step.helpText}";
            while (!AreAnyStartRecordKeysDown())
            {
                if (_skip)
                {
                    StopWizard("Wizard canceled");
                    yield break;
                }

                yield return 0;
                step.Update();
            }

            step.Apply();
        }

        StopWizard("All done! You can now activate Embody.\n\nYou can tweak your settings or start this wizard again. You can make this setup your default in the Import/Export screen. Default settings will automatically apply whenever you load this plugin on an atom.");
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
