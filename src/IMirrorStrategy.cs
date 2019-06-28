namespace Acidbubbles.ImprovedPoV
{
    public interface IMirrorStrategy
    {
        string OwnerStrategyName { get; }
        object ToBroadcastable();
        bool BeforeMirrorRender();
        void AfterMirrorRender();
    }
}