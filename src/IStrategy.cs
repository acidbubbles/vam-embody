namespace Acidbubbles.ImprovedPoV
{
    public interface IStrategy
    {
        string Name { get; }
        void Apply(MemoizedPerson person);
        void Restore();
    }
}