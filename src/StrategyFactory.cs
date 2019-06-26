using System;

namespace Acidbubbles.ImprovedPoV
{
    public static class StrategyFactory
    {
        public static IStrategy CreateStrategy(string val)
        {
            switch (val)
            {
                case MaterialsEnabledStrategy.Name:
                    return new MaterialsEnabledStrategy();
                case ShaderStrategy.Name:
                    return new ShaderStrategy();
                case NoStrategy.Name:
                    return new NoStrategy();
                default:
                    throw new InvalidOperationException("Invalid strategy: '" + val + "'");
            }
        }
    }
}
