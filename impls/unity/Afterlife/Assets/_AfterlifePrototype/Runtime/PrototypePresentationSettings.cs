using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Settings/Presentation")]
    public sealed class PrototypePresentationSettings : ScriptableObject
    {
        public bool VisionBlur = true;
        public float TreeModelScale = 1.28f/1.8f;
        public float RockModelScale = 1.20f/1.8f;
        public int PixelHeight = 360;
        public float GrassSizeScale = 1f/1.8f;
        public bool GrassUsesTerrainColor = true;
        public Color GrassBaseColor = new Color(.30f,.40f,.23f);
        public Color GrassBeigeColor = new Color(.65f,.57f,.41f);
        public Color GrassDarkColor = new Color(.14f,.17f,.15f);
        public Color GrassPaleColor = new Color(.91f,.93f,.89f);
        public Color GrassTealColor = new Color(.24f,.48f,.42f);
        public Color GrassMossColor = new Color(.36f,.43f,.20f);
        public Color GrassPurpleColor = new Color(.47f,.30f,.48f);
        public Color GrassFlowerPink = new Color(.82f,.66f,.78f);
        public Color GrassFlowerYellow = new Color(.95f,.92f,.78f);
    }
}
