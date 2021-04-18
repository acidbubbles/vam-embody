using MVR.FileManagementSecure;
using SimpleJSON;

public class MakeDefaultsStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
Do you want to <b>use these settings by default</b>? When you add Embody to other persons, these settings will <b>automatically be applied</b>.

Select <b>Next to save as default</b>.

Skip to keep the defaults untouched.".TrimStart();

    public MakeDefaultsStep(EmbodyContext context)
        : base(context)
    {
    }

    public override void Enter()
    {
        base.Enter();

        SuperController.singleton.worldScale = 1f;
        if (context.worldScale.worldScaleMethodJSON.val == WorldScaleModule.PlayerHeightMethod)
            context.worldScale.selectedJSON.val = true;
    }

    public bool Apply()
    {
        FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
        var jc = new JSONClass();
        context.embody.StoreJSON(jc, true);
        context.plugin.SaveJSON(jc, SaveFormat.DefaultsPath);
        return true;
    }
}
