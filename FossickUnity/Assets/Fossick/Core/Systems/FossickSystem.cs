namespace Fossick.Core.Systems
{
    public abstract class FossickSystem
    {
        protected FossickSystem(string systemName)
        {
            SystemName = string.IsNullOrEmpty(systemName) ? GetType().Name : systemName;
        }

        public string SystemName { get; }
    }
}
