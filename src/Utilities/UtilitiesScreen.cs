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

        CreateButton("Create mirror").button.onClick.AddListener(() => SuperController.singleton.StartCoroutine(Utilities.CreateMirror(context.containingAtom)));
    }
}
