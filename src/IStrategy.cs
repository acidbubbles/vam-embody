namespace Acidbubbles.ImprovedPoV
{
    public interface IStrategy
    {
        string Name { get; }
        void Apply(PersonReference person);
        void Restore();
        IMirrorStrategy GetMirrorStrategy(object data);
    }
}