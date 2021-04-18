using System.Linq;

public class ResetPoseStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
We will now <b>apply a pose</b> so the model is standing straight, and only expected nodes are on.

If there is no mirror in the scene, we will create one; stand straight and look forward, and the mirror will be created in front of you.

Press <b>Next</b> when ready.

Skip if you'd like to run the wizard using the current pose instead.".TrimStart();

    public ResetPoseStep(EmbodyContext context)
        : base(context)
    {
    }

    public bool Apply()
    {
        new PossessionPose(context).Apply();
        if (SuperController.singleton.GetAtoms().All(a => a.type != "Glass"))
            SuperController.singleton.StartCoroutine(Utilities.CreateMirror(context.eyeTarget, context.containingAtom));
        return true;
    }
}
