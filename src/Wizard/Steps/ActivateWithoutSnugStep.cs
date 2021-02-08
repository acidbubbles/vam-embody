using System.Linq;

public class ActivateWithoutSnugStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now start possession (except hands). Press next when ready.";

    public ActivateWithoutSnugStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
        var autoSetup = new SnugAutoSetup(context.containingAtom, context.snug);
        autoSetup.AutoSetup();

        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = false;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = false;
        context.embody.activeJSON.val = false;
    }
}
