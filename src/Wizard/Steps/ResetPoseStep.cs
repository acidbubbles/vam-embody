using System.Linq;

public class ResetPoseStep : WizardStepBase, IWizardStep
{
    private readonly bool _preferToes;
    public string helpText => @"
We will now <b>reset all Embody settings</b>, and <b>apply a pose</b> so the model is standing straight.

A mirror is recommended to see what you're doing.

Press <b>Next</b> when ready.

Skip if you'd like to run the wizard using the current settings and pose instead.".TrimStart();

    public ResetPoseStep(EmbodyContext context, bool preferToes)
        : base(context)
    {
        _preferToes = preferToes;
    }

    public bool Apply()
    {
        Utilities.ResetToDefaults(context);

        new PossessionPose(context).Apply();

        return true;
    }
}
