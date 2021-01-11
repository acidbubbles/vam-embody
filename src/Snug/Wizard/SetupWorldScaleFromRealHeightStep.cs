using System.Linq;

public class SetupWorldScaleFromRealHeightStep : IWizardStep
{
    public string helpText => "Stand straight, and press select when ready. Make sure the VR person is also standing straight.";

    private readonly Atom _containingAtom;

    public SetupWorldScaleFromRealHeightStep(Atom containingAtom)
    {
        _containingAtom = containingAtom;
    }

    public void Run(SnugWizardContext context)
    {
        // TODO: Replace by the WorldScaleModule setup (same as the WorldScaleScreen)
        var headControl = _containingAtom.freeControllers.First(fc => fc.name == "headControl");
        var lFootControl = _containingAtom.freeControllers.First(fc => fc.name == "lFootControl");
        var rFootControl = _containingAtom.freeControllers.First(fc => fc.name == "rFootControl");

        var realHeight = SuperController.singleton.heightAdjustTransform.InverseTransformPoint(SuperController.singleton.centerCameraTarget.transform.position).y;
        // NOTE: Floor is more precise but foot allows to be at non-zero height for calibration
        // TODO: Try and make the model stand straight, not sure how I can do that
        var gameHeight = headControl.transform.position.y - ((lFootControl.transform.position.y + rFootControl.transform.position.y) / 2f);
        var scale = gameHeight / realHeight;
        // TODO: Use WorldScaleModule instead (so this wizard is really not snug-specific)
        SuperController.singleton.worldScale = scale;
        SuperController.LogMessage($"Player height: {realHeight}, model height: {gameHeight}, scale: {scale}");
    }
}
