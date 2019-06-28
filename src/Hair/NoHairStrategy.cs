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

        public void Apply(PersonReference person)
        {
        }

        public void Restore()
        {
        }

        public IMirrorStrategy GetMirrorStrategy(object data)
        {
            return null;
        }
    }
}