namespace Handlers
{
    public interface IHandler
    {
        bool Configure();
        void Restore();
        void BeforeRender();
        void AfterRender();
    }
}
