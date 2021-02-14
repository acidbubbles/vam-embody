using MVR.FileManagementSecure;

public class MakeDefaultsStep : WizardStepBase, IWizardStep
{
    public string helpText => "Do you want to save make these settings the defaults? When you add Embody to other persons, these settings will automatically be applied. Select Next to save as default, select Skip to keep the defaults untouched.";

    public MakeDefaultsStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
        FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
        var jc = context.plugin.GetJSON();
        context.plugin.SaveJSON(jc, SaveFormat.DefaultsPath);
    }
}
