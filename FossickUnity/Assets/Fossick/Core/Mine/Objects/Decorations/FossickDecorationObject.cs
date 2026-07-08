namespace Fossick.Core.Mine.Objects
{
    public sealed class FossickDecorationObject : FossickCellObject
    {
        public FossickDecorationObject(string decorationId, FossickPosition position)
            : base(decorationId, FossickVisualLayer.Decoration, position)
        {
            DecorationId = decorationId;
        }

        public string DecorationId { get; }
    }
}
