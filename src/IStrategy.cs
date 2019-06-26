namespace Acidbubbles.ImprovedPoV
{
    public interface IStrategy
    {
        string Name { get; }
        void Apply(DAZSkinV2 skin);
        void Restore(DAZSkinV2 skin);
    }
}