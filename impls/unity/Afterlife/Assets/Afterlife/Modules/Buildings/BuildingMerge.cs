using System.Collections.Generic;
namespace Afterlife.Buildings
{
    public readonly struct MergeCandidate
    {
        public readonly int Id, Kind, Target, X, Z;
        public MergeCandidate(int id,int kind,int target,int x,int z) { Id=id;Kind=kind;Target=target;X=x;Z=z; }
    }
    public static class BuildingMerge
    {
        public static int[] FindSquare(IReadOnlyList<MergeCandidate> candidates)
        {
            var ordered=new List<MergeCandidate>(candidates);
            ordered.Sort((a,b)=>a.Z!=b.Z?a.Z.CompareTo(b.Z):a.X.CompareTo(b.X));
            foreach(var origin in ordered)
            {
                var ids=new int[4];int count=0;
                for(int dz=0;dz<2;dz++)for(int dx=0;dx<2;dx++)
                    foreach(var item in ordered)
                        if(item.Kind==origin.Kind&&item.Target==origin.Target&&item.X==origin.X+dx&&item.Z==origin.Z+dz){ids[count++]=item.Id;break;}
                if(count==4)return ids;
            }
            return null;
        }
        public static float AdjacencySpeed(int neighbors) => 1+.25f*System.Math.Min(4,System.Math.Max(0,neighbors));
    }
}
