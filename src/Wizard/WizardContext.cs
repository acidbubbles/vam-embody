public class WizardContext
{
    public EmbodyContext context;
    public Atom containingAtom => context.containingAtom;
    public IEmbody embody { get; set; }

    public float handsDistance;
}
