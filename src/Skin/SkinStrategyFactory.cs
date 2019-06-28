#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;

namespace Acidbubbles.ImprovedPoV.Skin
{
    public class SkinStrategyFactory : IStrategyFactory
    {
        internal static readonly List<string> Names = new List<string> { NoSkinStrategy.Name, SkinMaterialsEnabledStrategy.Name, SkinShaderStrategy.Name };
        internal static readonly string Default = SkinShaderStrategy.Name;

        public IStrategy Create(string name)
        {
            switch (name)
            {
                case SkinMaterialsEnabledStrategy.Name:
                    return new SkinMaterialsEnabledStrategy();
                case SkinShaderStrategy.Name:
                    return new SkinShaderStrategy();
                case NoSkinStrategy.Name:
                    return new NoSkinStrategy();
                default:
                    throw new InvalidOperationException("Invalid skin strategy: '" + name + "'");
            }
        }

        public IStrategy None()
        {
            return new NoSkinStrategy();
        }
    }
}
