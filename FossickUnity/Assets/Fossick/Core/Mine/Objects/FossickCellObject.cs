namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickCellObject
    {
        protected FossickCellObject(string objectId, FossickVisualLayer layer, FossickPosition position)
        {
            ObjectId = string.IsNullOrEmpty(objectId) ? layer.ToString() : objectId;
            Layer = layer;
            Position = position;
            Visible = true;
        }

        public string ObjectId { get; }
        public FossickVisualLayer Layer { get; }
        public FossickPosition Position { get; private set; }
        public bool Visible { get; set; }

        public void MoveTo(FossickPosition position)
        {
            Position = position;
        }
    }
}
