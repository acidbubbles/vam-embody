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
}

public class WizardStatusChangedEvent : UnityEvent<bool> { }

public class WizardModule : EmbodyModuleBase, IWizard
{

    public const string Label = "Wizard";
    public override string storeId => "Wizard";
    public override string label => Label;
    public override bool alwaysEnabled => true;

    public IWorldScaleModule worldScale { get; set; }
    public ISnugModule snug { get; set; }
    public ITrackersModule trackers { get; set; }
    public WizardStatusChangedEvent statusChanged { get; } = new WizardStatusChangedEvent();
    public JSONStorableString statusJSON { get; } = new JSONStorableString("WizardStatus", "");
    public bool isRunning => _coroutine != null;

    private Coroutine _coroutine;
    private bool _next;

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
        statusChanged.Invoke(false);
    }

    public void Next()
    {
        _next = true;
    }

    private IEnumerator StartWizardCo()
    {
        yield return 0;

        var wizardContext = new WizardContext
        {
            realLeftHand = SuperController.singleton.leftHand,
            realRightHand = SuperController.singleton.rightHand,
            trackers = trackers
        };

        if (worldScale.selectedJSON.val)
        {
            SuperController.singleton.worldScale = 1f;
        }

        if (snug.selectedJSON.val)
        {
            var autoSetup = new SnugAutoSetup(context.containingAtom, snug);
            autoSetup.AutoSetup();
        }

        // TODO: Add headControl to eyes center offset to the _trackers configuration so the head-to-eyes is perfect

        var steps = new List<IWizardStep>();

        if (worldScale.selectedJSON.val)
        {
            steps.Add(new SetupWorldScaleFromRealHeightStep(context.containingAtom));
        }

        if (snug.selectedJSON.val)
        {
            steps.Add(new MeasureHandsPaddingStep());
            steps.Add(new StartPossessionStep());
            var hipsAnchor = snug.anchorPoints.First(a => a.Label == "Hips");
            steps.Add(new MeasureAnchorWidthStep("hips", hipsAnchor));
            steps.Add(new MeasureAnchorDepthAndOffsetStep("hips", hipsAnchor));
        }

        foreach (var step in steps)
        {
            // TODO: Use overlays instead
            statusJSON.val = step.helpText;
            while (!AreAnyStartRecordKeysDown())
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    StopWizard("Wizard canceled");
                    yield break;
                }

                yield return 0;
            }
            yield return 0;
            step.Run(wizardContext);
        }

        StopWizard("All done! You can now activate Embody.");
    }


    private bool AreAnyStartRecordKeysDown()
    {
        if (_next) return true;
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
