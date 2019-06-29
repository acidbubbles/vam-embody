#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;

namespace Acidbubbles.ImprovedPoV.Hair
{
    public class HairStrategyFactory : IStrategyFactory
    {
        internal static readonly List<string> Names = new List<string> { NoHairStrategy.Name, HairMaterialStrategy.Name };
        internal static readonly string Default = HairMaterialStrategy.Name;

        public IStrategy Create(string name)
        {
            switch (name)
            {
                case HairMaterialStrategy.Name:
                    return new HairMaterialStrategy();
                case NoHairStrategy.Name:
                    return new NoHairStrategy();
                default:
                    throw new InvalidOperationException("Invalid hair strategy: '" + name + "'");
            }
        }

        public IStrategy None()
        {
            return new NoHairStrategy();
        }
    }
}
