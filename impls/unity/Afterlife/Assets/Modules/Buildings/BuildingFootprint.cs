using System;
namespace Afterlife.Buildings
{
    public readonly struct BuildingFootprint
    {
        public readonly int X, Z, Width, Depth;
        public int Right => X + Width - 1;
        public int Top => Z + Depth - 1;
        public BuildingFootprint(int x,int z,int width,int depth) { X=x;Z=z;Width=Math.Max(1,width);Depth=Math.Max(1,depth); }
        public BuildingFootprint Expand(int radius) => new BuildingFootprint(X-radius,Z-radius,Width+2*radius,Depth+2*radius);
        public bool Contains(int x,int z) => x>=X&&x<=Right&&z>=Z&&z<=Top;
        public bool Touches(BuildingFootprint other)
        {
            bool overlapX=X<=other.Right&&Right>=other.X, overlapZ=Z<=other.Top&&Top>=other.Z;
            return overlapX&&Z<=other.Top+1&&Top+1>=other.Z || overlapZ&&X<=other.Right+1&&Right+1>=other.X;
        }
    }
    // Unity 비의존 거리 규칙. 좌표와 반경은 동일한 월드 단위를 사용한다.
    public static class CircularRange
    {
        public static bool Contains(float x, float z, float centerX, float centerZ, float radius)
        {
            if (radius <= 0) return false;
            float dx = x - centerX, dz = z - centerZ;
            return dx * dx + dz * dz <= radius * radius;
        }

        public static float SightStrength(float x, float z, float centerX, float centerZ, float radius)
        {
            if (!Contains(x, z, centerX, centerZ, radius)) return 0;
            float dx = x - centerX, dz = z - centerZ;
            return Math.Max(0, Math.Min(1, (radius - (float)Math.Sqrt(dx * dx + dz * dz)) / .65f));
        }
    }

    public readonly struct TownSource
    {
        public readonly float CenterX, CenterZ, Radius, SpeedBonus;
        public TownSource(BuildingFootprint footprint, float radius, float speedBonus)
        {
            CenterX = footprint.X + (footprint.Width - 1) * .5f;
            CenterZ = footprint.Z + (footprint.Depth - 1) * .5f;
            Radius = Math.Max(0, radius);
            SpeedBonus = Math.Max(0, speedBonus);
        }

        public System.Collections.Generic.IEnumerable<(int X, int Z)> Tiles()
        {
            if (Radius <= 0) yield break;
            for (int x = (int)Math.Ceiling(CenterX - Radius); x <= Math.Floor(CenterX + Radius); x++)
            for (int z = (int)Math.Ceiling(CenterZ - Radius); z <= Math.Floor(CenterZ + Radius); z++)
                if (CircularRange.Contains(x, z, CenterX, CenterZ, Radius)) yield return (x, z);
        }
    }
}
