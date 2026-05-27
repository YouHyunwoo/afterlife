namespace Afterlife.Dev.Game
{
    public enum SelectType { None, Select, Deselect }

    public struct SelectCommand
    {
        public SelectType Type;
    }
}