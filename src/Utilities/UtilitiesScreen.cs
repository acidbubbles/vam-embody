using System.Collections;
using System.Linq;

public class UtilitiesScreen : ScreenBase, IScreen
{
    public const string ScreenName = "Utilities";

    public UtilitiesScreen(EmbodyContext context)
        : base(context)
    {
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Some useful functions, hopefully!"), true);

        CreateButton("Create Mirror").button.onClick.AddListener(CreateMirror);

        CreateButton("Arm Possessed Controllers & Record").button.onClick.AddListener(StartRecord);

    }

    private void CreateMirror()
    {
        SuperController.singleton.StartCoroutine(Utilities.CreateMirror(context.eyeTarget, context.containingAtom));
    }

    private void StartRecord()
    {
        context.embody.activeJSON.val = true;
        SuperController.singleton.motionAnimationMaster.StopPlayback();
        SuperController.singleton.motionAnimationMaster.ResetAnimation();
        foreach (var controller in context.plugin.containingAtom.freeControllers.Where(fc => fc.possessed))
        {
            var mac = controller.GetComponent<MotionAnimationControl>();
            mac.ClearAnimation();
            mac.armedForRecord = true;
        }
        SuperController.singleton.SelectModeAnimationRecord();
        SuperController.singleton.StartCoroutine(WaitForRecordComplete());
    }

    private IEnumerator WaitForRecordComplete()
    {
        while (!string.IsNullOrEmpty(SuperController.singleton.helpText))
            yield return 0;
        SuperController.singleton.motionAnimationMaster.StopPlayback();
        SuperController.singleton.motionAnimationMaster.ResetAnimation();
    }
}
