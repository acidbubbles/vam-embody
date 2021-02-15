using System;
using System.Collections.Generic;
using System.Linq;

public class AskSnugStep : WizardStepBase, IWizardStep
{
    public string helpText => "Do you want to setup Snug?\n\nSnug dynamically adjusts your hands so despite body proportion differences, your in-game hands position will match your own in relation to your body. In other words, you can touch yourself in the real world and it should feel right in VR!\n\nSelect Next to activate possession and configure Snug (heads will be disabled), or Skip to disable Snug.";

    private readonly List<IWizardStep> _steps;

    public AskSnugStep(EmbodyContext context, List<IWizardStep> steps)
        : base(context)
    {
        _steps = steps;
    }

    public void Apply()
    {
        var autoSetup = new SnugAutoSetup(context.containingAtom, context.snug);
        autoSetup.AutoSetup();

        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = false;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = false;
        context.embody.activeJSON.val = true;

        var idx = _steps.IndexOf(this);
        if (idx == -1) throw new InvalidOperationException($"{nameof(AskSnugStep)} was not found in the steps list");
        // _steps.Insert++idx, (new MeasureHandsPaddingStep(context));
        {
            var anchor = context.snug.anchorPoints.First(a => a.id == "Head");
            _steps.Insert(++idx, new MeasureAnchorWidthStep(context, anchor, 100));
            _steps.Insert(++idx, new MeasureAnchorDepthAndOffsetStep(context, anchor, -10));
        }
        {
            var anchor = context.snug.anchorPoints.First(a => a.id == "Chest");
            _steps.Insert(++idx, new MeasureAnchorWidthStep(context, anchor, -20));
            _steps.Insert(++idx, new MeasureAnchorDepthAndOffsetStep(context, anchor, -10));
        }
        {
            var anchor = context.snug.anchorPoints.First(a => a.id == "Abdomen");
            _steps.Insert(++idx, new MeasureAnchorWidthStep(context, anchor, -20));
            _steps.Insert(++idx, new MeasureAnchorDepthAndOffsetStep(context, anchor, 10));
        }
        {
            var anchor = context.snug.anchorPoints.First(a => a.id == "Hips");
            _steps.Insert(++idx, new MeasureAnchorWidthStep(context, anchor, -60));
            _steps.Insert(++idx, new MeasureAnchorDepthAndOffsetStep(context, anchor, 70));
        }
        // TODO: Hands at rest height step
        _steps.Insert(++idx, new FinishSnugSetupStep(context));
    }
}
