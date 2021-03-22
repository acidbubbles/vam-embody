using System;
using System.Collections.Generic;
using System.Linq;

public class AskSnugStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
Do you want to setup <b>Snug</b>?

Snug dynamically adjusts your hands so despite body proportion differences, your in-game hands position will match your own in relation to your body. In other words, <b>you can touch yourself in the real world and it should feel right in VR</b>!

If it's your first time using Embody, <b>skip Snug until you're comfortable with base possession</b>. Getting Snug right may require multiple tries.

Select <b>Next to use Snug</b> (possession will be activated, but hands will be disabled).

<b>Skip to disable Snug</b>.".TrimStart();

    private readonly List<IWizardStep> _steps;

    public AskSnugStep(EmbodyContext context, List<IWizardStep> steps)
        : base(context)
    {
        _steps = steps;
    }

    public bool Apply()
    {
        var autoSetup = new SnugAutoSetup(context.containingAtom, context.snug);
        autoSetup.AutoSetup();

        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = false;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = false;
        context.trackers.motionControls.First(mc => mc.mappedControllerName == "lElbowControl").enabled = false;
        context.trackers.motionControls.First(mc => mc.mappedControllerName == "rElbowControl").enabled = false;
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
            _steps.Insert(++idx, new MeasureAnchorWidthStep(context, anchor, -20, true));
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
        {
            var anchor = context.snug.anchorPoints.First(a => a.id == "Thighs");
            _steps.Insert(++idx, new MeasureArmsAtRestStep(context, anchor));
        }
        _steps.Insert(++idx, new FinishSnugSetupStep(context));

        return true;
    }

    public override void Leave(bool final)
    {
        base.Leave(final);
        if (final)
        {
            context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
            context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;
            context.trackers.motionControls.First(mc => mc.mappedControllerName == "lElbowControl").enabled = true;
            context.trackers.motionControls.First(mc => mc.mappedControllerName == "rElbowControl").enabled = true;
        }
    }
}
