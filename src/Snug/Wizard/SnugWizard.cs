using System.Collections;
using System.Linq;
using UnityEngine;

public class SnugWizard
{
    private readonly Atom _containingAtom;
    private readonly ISnugModule _snug;
    private readonly SnugAutoSetup _autoSetup;
    private readonly ITrackersModule _trackers;

    public SnugWizard(Atom containingAtom, ISnugModule snug, SnugAutoSetup autoSetup, ITrackersModule trackers)
    {
        _containingAtom = containingAtom;
        _snug = snug;
        _autoSetup = autoSetup;
        _trackers = trackers;
    }
    public IEnumerator Wizard()
    {
        yield return 0;

        // TODO: Helper?
        var hipsAnchor = _snug.anchorPoints.First(a => a.Label == "Hips");
        var context = new SnugWizardContext
        {
            realLeftHand = SuperController.singleton.leftHand,
            realRightHand = SuperController.singleton.rightHand,
            trackers = _trackers
        };

        SuperController.singleton.worldScale = 1f;
        _autoSetup.AutoSetup();

        var steps = new IWizardStep[]
        {
            new SetupWorldScaleFromRealHeightStep(_containingAtom),
            new MeasureHandsPaddingStep(),
            new StartPossessionStep(),
            new MeasureAnchorWidthStep("hips", hipsAnchor),
            new MeasureAnchorDepthAndOffsetStep("hips", hipsAnchor)
        };

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
