namespace Handlers
{
    public class ClothingHandler : IHandler
    {
        private readonly DAZClothingItem _clothing;

        public ClothingHandler(DAZClothingItem clothing)
        {
            _clothing = clothing;
        }

        public bool Configure()
        {
            return true;
        }

        public void Restore()
        {
        }

        public void BeforeRender()
        {
        }

        public void AfterRender()
        {
        }
    }
}
