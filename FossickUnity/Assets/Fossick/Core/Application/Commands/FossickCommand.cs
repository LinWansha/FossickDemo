namespace Fossick.Core.Application.Commands
{
    public abstract class FossickCommand
    {
        protected FossickCommand(string commandId)
        {
            CommandId = string.IsNullOrEmpty(commandId) ? GetType().Name : commandId;
        }

        public string CommandId { get; }
    }
}
