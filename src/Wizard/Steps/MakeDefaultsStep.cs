using MVR.FileManagementSecure;

public class MakeDefaultsStep : WizardStepBase, IWizardStep
{
    public string helpText => "Do you want to <b>use these settings by default<b>? When you add Embody to other persons, these settings will <b>automatically be applied</b>.\n\nSelect <b>Next to save as default</b>.\n\nSkip to keep the defaults untouched.";

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
