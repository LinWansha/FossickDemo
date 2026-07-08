namespace Fossick.Core.Mine.Objects
{
    public sealed class FossickFogState
    {
        public FossickFogState(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public bool IsVisible { get; private set; }

        public bool Reveal()
        {
            if (IsVisible)
            {
                return false;
            }

            IsVisible = true;
            return true;
        }

        public void Cover()
        {
            IsVisible = false;
        }
    }
}
