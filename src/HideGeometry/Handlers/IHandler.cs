using System;

namespace Handlers
{
    public interface IHandler : IDisposable
    {
        bool Prepare();
        void BeforeRender();
        void AfterRender();
    }
}
