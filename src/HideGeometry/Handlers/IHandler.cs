namespace Handlers
{
    public interface IHandler
    {
        void Restore();
        void BeforeRender();
        void AfterRender();
    }
}
