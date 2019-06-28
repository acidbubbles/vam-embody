#define POV_DIAGNOSTICS
using System;

namespace Acidbubbles.ImprovedPoV
{
    public interface IStrategyFactory
    {
        IStrategy Create(string name);
        IStrategy None();
    }
}
