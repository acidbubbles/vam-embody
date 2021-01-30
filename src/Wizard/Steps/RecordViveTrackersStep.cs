public class RecordViveTrackersStep : IWizardStep, IWizardUpdate
{
    public string helpText => "Vive trackers detected. Take the same pose as the person you want to possess, and press Next";

    public void Run()
    {
        // TODO: Snap all motion control position using TrackersAutoSetup
    }

    public void Update()
    {
        // TODO: Enable and disable the preview
        // TODO: Draw lines connecting trackers with their closest target
    }
}
