using System;
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
    private EmbodySelectionSnapshot _snapshot;
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

        if (_snapshot != null)
        {
            _snapshot.Restore();
            _snapshot = null;
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
        _snapshot = EmbodySelectionSnapshot.Snap(context);

        yield return 0;

        if (_snapshot.worldScale)
        {
            SuperController.singleton.worldScale = 1f;
        }

        if (_snapshot.snug)
        {
            if (_snapshot.passenger)
            {
                StopWizard("You cannot run the wizard with Passenger selected.");
                yield break;
            }

            var autoSetup = new SnugAutoSetup(context.containingAtom, context.snug);
            autoSetup.AutoSetup();
        }

        _snapshot.DisableAll();

        var steps = new List<IWizardStep>();

        if (_snapshot.worldScale)
        {
            steps.Add(new RecordPlayerHeightStep(context.worldScale));
        }

        if (_snapshot.trackers)
        {
            // NOTE: We use Count because we want to sync all available motion controls, not only the first one
            // ReSharper disable once ReplaceWithSingleCallToCount UseMethodAny.0
            if (context.trackers.viveTrackers.Where(t => t.SyncMotionControl()).Count() > 0)
            {
                steps.Add(new ActivateHeadAndHandsStep(context, _snapshot));
                steps.Add(new RecordViveTrackersStep(context));
                steps.Add(new DeactivateStep(context));
            }
        }

        // TODO: Implement Snug wizard
        if (_snapshot.snug && false)
        {
            // TODO: Load pose
            // steps.Add(new MeasureHandsPaddingStep(context));
            steps.Add(new ActivateWithoutSnugStep(context.embody, context.snug));
            var hipsAnchor = context.snug.anchorPoints.First(a => a.label == "Hips");
            steps.Add(new MeasureAnchorWidthStep(context, "hips", hipsAnchor));
            steps.Add(new MeasureAnchorDepthAndOffsetStep(context, "hips", hipsAnchor));
            // steps.Add(new EnableSnugStep(context.embody, context.snug));
        }
        else
        {
            // steps.Add(new ActivateStep(context.embody));
        }

        if (steps.Count == 0)
        {
            StopWizard("None of the selected modules use the wizard.\n\nNothing to setup, moving on!");
            yield break;
        }

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

        StopWizard("All done! You can now activate Embody.");
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
