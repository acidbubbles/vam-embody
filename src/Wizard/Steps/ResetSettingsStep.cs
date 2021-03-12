using System.Linq;

public class ResetSettingsStep : WizardStepBase, IWizardStep
{
    public string helpText => (@"
Before starting, try <b>activating Embody with the default settings</b>, it might work without further adjustments for you." + (SuperController.singleton.isOVR ? "" : @"

You should also <b>adjust your hands</b> in the <i>Configure Trackers...</i> menu first, if they don't feel right. Stop the wizard and do that now if you didn't check.") + @"

Press <b>Next</b> when ready; all Embody settings will be <b>reset</b> except for hand adjustments.

Only skip this if you know what you are doing!").TrimStart();

    public ResetSettingsStep(EmbodyContext context)
        : base(context)
    {
    }

    public bool Apply()
    {
        var leftOffsetControllerCustom = context.trackers.leftHandMotionControl.offsetControllerCustom;
        var leftRotateControllerCustom = context.trackers.leftHandMotionControl.rotateControllerCustom;
        var leftRotateAroundTrackerCustom = context.trackers.leftHandMotionControl.rotateAroundTrackerCustom;
        var rightOffsetControllerCustom = context.trackers.rightHandMotionControl.offsetControllerCustom;
        var rightRotateControllerCustom = context.trackers.rightHandMotionControl.rotateControllerCustom;
        var rightRotateAroundTrackerCustom = context.trackers.rightHandMotionControl.rotateAroundTrackerCustom;
        Utilities.ResetToDefaults(context);
        context.trackers.leftHandMotionControl.offsetControllerCustom = leftOffsetControllerCustom;
        context.trackers.leftHandMotionControl.rotateControllerCustom = leftRotateControllerCustom;
        context.trackers.leftHandMotionControl.rotateAroundTrackerCustom = leftRotateAroundTrackerCustom;
        context.trackers.rightHandMotionControl.offsetControllerCustom = rightOffsetControllerCustom;
        context.trackers.rightHandMotionControl.rotateControllerCustom = rightRotateControllerCustom;
        context.trackers.rightHandMotionControl.rotateAroundTrackerCustom = rightRotateAroundTrackerCustom;
        return true;
    }
}
