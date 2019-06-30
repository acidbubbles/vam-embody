public class ForceEditMode : MVRScript
{
    public override void Init()
    {
        SuperController.singleton.gameMode = SuperController.GameMode.Edit;
    }
}