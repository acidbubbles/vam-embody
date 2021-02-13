public class FreeControllerV3WithSnapshot
{
    public readonly FreeControllerV3 controller;
    public readonly HandControl handControl;
    public FreeControllerV3Snapshot snapshot;
    public bool active;

    public FreeControllerV3WithSnapshot(FreeControllerV3 controller)
    {
        this.controller = controller;
        handControl = controller.GetComponent<HandControl>() ?? controller.GetComponent<HandControlLink>()?.handControl;
    }
}
