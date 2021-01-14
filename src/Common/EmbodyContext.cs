public class EmbodyContext
{
    public readonly MVRScript plugin;
    public readonly IEmbody embody;
    public Atom containingAtom => plugin.containingAtom;

    public EmbodyContext(MVRScript plugin, IEmbody embody)
    {
        this.plugin = plugin;
        this.embody = embody;
    }

    public void Initialize()
    {
    }
}
