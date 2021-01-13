using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EmbodyWizard
{
    private readonly Atom _containingAtom;
    private readonly IWorldScaleModule _worldScale;
    private readonly ISnugModule _snug;
    private readonly ITrackersModule _trackers;

    public EmbodyWizard(Atom containingAtom, IWorldScaleModule worldScale, ISnugModule snug, ITrackersModule trackers)
    {
        _containingAtom = containingAtom;
        _worldScale = worldScale;
        _snug = snug;
        _trackers = trackers;
    }
    public IEnumerator Wizard()
    {
        yield return 0;

        var context = new EmbodyWizardContext
        {
            realLeftHand = SuperController.singleton.leftHand,
            realRightHand = SuperController.singleton.rightHand,
            trackers = _trackers
        };

        if (_worldScale.selectedJSON.val)
        {
            SuperController.singleton.worldScale = 1f;
        }

        if (_snug.selectedJSON.val)
        {
            var autoSetup = new SnugAutoSetup(_containingAtom, _snug);
            autoSetup.AutoSetup();
        }

        var steps = new List<IWizardStep>();

        if (_worldScale.selectedJSON.val)
        {
            steps.Add(new SetupWorldScaleFromRealHeightStep(_containingAtom));
        }

        if (_snug.selectedJSON.val)
        {
            var hipsAnchor = _snug.anchorPoints.First(a => a.Label == "Hips");
            steps.Add(new MeasureHandsPaddingStep());
            steps.Add(new StartPossessionStep());
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
            step.Run(context);
        }

        SuperController.singleton.helpText = "All done! You can now activate Embody.";
        yield return new WaitForSeconds(3);
        SuperController.singleton.helpText = "";
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
