namespace Handlers
{
    public class ClothingHandler : IHandler
    {
        private readonly DAZClothingItem _clothing;

        public ClothingHandler(DAZClothingItem clothing)
        {
            _clothing = clothing;
        }

        public bool Prepare()
        {
            return true;
        }

        public void Dispose()
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
