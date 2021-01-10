using System.Collections;
using System.Linq;
using UnityEngine;

public class SnugWizard
{
    private readonly Atom _containingAtom;
    private readonly ISnugModule _snug;
    private readonly SnugAutoSetup _autoSetup;
    private readonly ITrackersModule _trackers;

    public SnugWizard(Atom containingAtom, ISnugModule snug, SnugAutoSetup autoSetup, ITrackersModule trackers)
    {
        _containingAtom = containingAtom;
        _snug = snug;
        _autoSetup = autoSetup;
        _trackers = trackers;
    }
    public IEnumerator Wizard()
    {
        yield return 0;

        var realLeftHand = SuperController.singleton.leftHand;
        var realRightHand = SuperController.singleton.rightHand;
        var headControl = _containingAtom.freeControllers.First(fc => fc.name == "headControl");
        var lFootControl = _containingAtom.freeControllers.First(fc => fc.name == "lFootControl");
        var rFootControl = _containingAtom.freeControllers.First(fc => fc.name == "rFootControl");

        SuperController.singleton.worldScale = 1f;

        _autoSetup.AutoSetup();

        // TODO: Use overlays instead
        // TODO: Try and make the model stand straight, not sure how I can do that
        SuperController.singleton.helpText = "Welcome to Embody's Wizard! Stand straight, and press select when ready. Make sure the VR person is also standing straight.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        var realHeight = SuperController.singleton.heightAdjustTransform.InverseTransformPoint(SuperController.singleton.centerCameraTarget.transform.position).y;
        // NOTE: Floor is more precise but foot allows to be at non-zero height for calibration
        var gameHeight = headControl.transform.position.y - ((lFootControl.transform.position.y + rFootControl.transform.position.y) / 2f);
        var scale = gameHeight / realHeight;
        SuperController.singleton.worldScale = scale;
        SuperController.LogMessage($"Player height: {realHeight}, model height: {gameHeight}, scale: {scale}");

        SuperController.singleton.helpText = "World scale adjusted. Now put your hands together like you're praying, and press select when ready.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        var handsDistance = Vector3.Distance(realLeftHand.position, realRightHand.position);
        SuperController.LogMessage($"Hand distance: {handsDistance}");

        SuperController.singleton.helpText = "Hand distance recorded. We will now start possession. Press select when ready.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        // TODO: We should technically enable Embody. Review this.
        _trackers.enabledJSON.val = true;

        SuperController.singleton.helpText = "Possession activated. Now put your hands on your real hips, and press select when ready.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        // TODO: Highlight the ring where we want the hands to be.
        var hipsAnchorPoint = _snug.anchorPoints.First(a => a.Label == "Hips");
        var gameHipsCenter = hipsAnchorPoint.GetInGameWorldPosition();
        // TODO: Check the forward size too, and the offset.
        // TODO: Don't check the _hand control_ distance, instead check the relevant distance (from inside the hands)
        var realHipsWidth = Vector3.Distance(realLeftHand.position, realRightHand.position) - handsDistance;
        var realHipsXCenter = (realLeftHand.position + realRightHand.position) / 2f;
        hipsAnchorPoint.RealLifeSize = new Vector3(realHipsWidth, 0f, hipsAnchorPoint.InGameSize.z);
        hipsAnchorPoint.RealLifeOffset = realHipsXCenter - gameHipsCenter;
        SuperController.LogMessage($"Real Hips height: {realHipsXCenter.y}, Game Hips height: {gameHipsCenter}");
        SuperController.LogMessage($"Real Hips width: {realHipsWidth}, Game Hips width: {hipsAnchorPoint.RealLifeSize.x}");
        SuperController.LogMessage($"Real Hips center: {realHipsXCenter}, Game Hips center: {gameHipsCenter}");

        SuperController.singleton.helpText = "Now put your right hand at the same level as your hips but on the front, squeezed on you.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        var adjustedHipsCenter = hipsAnchorPoint.GetAdjustedWorldPosition();
        var realHipsFront = Vector3.MoveTowards(realRightHand.position, adjustedHipsCenter, handsDistance / 2f);
        hipsAnchorPoint.RealLifeSize = new Vector3(hipsAnchorPoint.RealLifeSize.x, 0f, Vector3.Distance(realHipsFront, adjustedHipsCenter) * 2f);

        hipsAnchorPoint.Update();

        SuperController.singleton.helpText = "All done! You can now activate Embody.";
        yield return new WaitForSeconds(3);
        SuperController.singleton.helpText = "";
    }

    private static bool AreAnyStartRecordKeysDown()
    {
        var sc = SuperController.singleton;
        if (sc.isOVR)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.Touch)) return true;
            if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.Touch)) return true;
        }
        if (sc.isOpenVR)
        {
            if (sc.selectAction.stateDown) return true;
        }
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        return false;
    }
}
