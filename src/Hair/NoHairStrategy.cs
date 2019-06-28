#define POV_DIAGNOSTICS

namespace Acidbubbles.ImprovedPoV.Hair
{
    public class NoHairStrategy : IStrategy
    {
        public const string Name = "None (hair visible)";

        string IStrategy.Name
        {
            get { return Name; }
        }

        public void Apply(MemoizedPerson person)
        {
        }

        public void Restore()
        {
        }
    }
}