public class EmbodyScaleChangeReceiver : ScaleChangeReceiver
{
    public EmbodyContext context { get; set; }

    public override void ScaleChanged(float s)
    {
        base.ScaleChanged(s);
        context.snug?.ScaleChanged();
    }
}
