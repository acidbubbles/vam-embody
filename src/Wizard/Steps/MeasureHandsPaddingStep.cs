using System.Linq;
using UnityEngine;

public class MeasureHandsPaddingStep : WizardStepBase, IWizardStep
{
    public string helpText => "Put your hands together like you're praying, and press Next when ready.";
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
    private readonly Rigidbody _head;

    public MeasureHandsPaddingStep(EmbodyContext context)
        : base(context)
    {
        _head = context.containingAtom.rigidbodies.First(fc => fc.name == "head");
        _leftHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        _rightHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
    }

    public override void Update()
    {
        _leftHandControl.control.position = _head.position + _head.transform.forward * 0.3f + Vector3.down * 0.2f + _head.transform.right * -0.01f;
        _leftHandControl.control.eulerAngles = _head.rotation.eulerAngles;
        _leftHandControl.control.Rotate(new Vector3(180, 0, 90 + 10));

        _rightHandControl.control.position = _head.position + _head.transform.forward * 0.3f + Vector3.down * 0.2f + _head.transform.right * 0.01f;
        _rightHandControl.control.eulerAngles = _head.rotation.eulerAngles;
        _rightHandControl.control.Rotate(new Vector3(180, 0, 270 - 10));
    }

    public bool Apply()
    {
        SuperController.LogMessage($"Hands distance: {Vector3.Distance(context.LeftHand().position, context.RightHand().position)}");

        return true;
    }
}
