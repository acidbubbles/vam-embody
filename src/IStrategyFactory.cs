namespace Acidbubbles.ImprovedPoV
{
    public interface IStrategyFactory
    {
        IStrategy Create(string name);
        IStrategy None();
    }
}
