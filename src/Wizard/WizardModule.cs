using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IWizard : IEmbodyModule
{
    IWorldScaleModule worldScale { get; set; }
    ISnugModule snug { get; set; }
    ITrackersModule trackers { get; set; }
    void StartWizard();
    void StopWizard();
}

public class WizardModule : EmbodyModuleBase, IWizard
{
    public const string Label = "Wizard";
    public override string storeId => "Wizard";
    public override string label => Label;
    public override bool alwaysEnabled => true;

    public IWorldScaleModule worldScale { get; set; }
    public ISnugModule snug { get; set; }
    public ITrackersModule trackers { get; set; }
    private Coroutine _coroutine;

    public void StartWizard()
    {
        if (_coroutine != null) context.StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(StartWizardCo());
    }

    public void StopWizard()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    public IEnumerator StartWizardCo()
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
            SuperController.singleton.helpText = step.helpText;
            while (!AreAnyStartRecordKeysDown())
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SuperController.singleton.helpText = "";
                    yield break;
                }

                yield return 0;
            }
            yield return 0;
            step.Run(wizardContext);
        }

        // TODO: Instead use the edit screen OR an overlay, prefer the overlay
        SuperController.singleton.helpText = "All done! You can now activate Embody.";
        yield return new WaitForSeconds(3);
        SuperController.singleton.helpText = "";

        StopWizard();
    }


    private static bool AreAnyStartRecordKeysDown()
    {
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
