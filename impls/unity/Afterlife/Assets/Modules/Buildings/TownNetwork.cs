using System.Collections.Generic;
namespace Afterlife.Buildings
{
    public sealed class TownNetwork
    {
        private readonly Dictionary<(int X,int Z),int> _townByTile=new Dictionary<(int,int),int>();
        private readonly List<float> _bonuses=new List<float>();
        public IEnumerable<(int X,int Z)> Tiles => _townByTile.Keys;
        public void Rebuild(IReadOnlyList<TownSource> sources)
        {
            _townByTile.Clear();_bonuses.Clear();var visited=new bool[sources.Count];
            var tilesBySource = new List<HashSet<(int X, int Z)>>();
            foreach (var source in sources) tilesBySource.Add(new HashSet<(int X, int Z)>(source.Tiles()));
            for(int start=0;start<sources.Count;start++)
            {
                if(visited[start]||tilesBySource[start].Count==0)continue;
                int town=_bonuses.Count;float bonus=0;
                var queue=new Queue<int>();queue.Enqueue(start);visited[start]=true;
                while(queue.Count>0)
                {
                    int current=queue.Dequeue();var source=sources[current];bonus+=source.SpeedBonus;
                    foreach(var tile in tilesBySource[current])_townByTile[tile]=town;
                    for(int next=0;next<sources.Count;next++)if(!visited[next]&&Touches(tilesBySource[current],tilesBySource[next])){visited[next]=true;queue.Enqueue(next);}
                }
                _bonuses.Add(bonus);
            }
        }
        private static bool Touches(HashSet<(int X, int Z)> first, HashSet<(int X, int Z)> second)
        {
            if (first.Count > second.Count) return Touches(second, first);
            foreach (var tile in first)
                if (second.Contains(tile) || second.Contains((tile.X - 1, tile.Z)) ||
                    second.Contains((tile.X + 1, tile.Z)) || second.Contains((tile.X, tile.Z - 1)) ||
                    second.Contains((tile.X, tile.Z + 1))) return true;
            return false;
        }

        public float ConstructionMultiplier(BuildingFootprint footprint)
        {
            int town=-1;
            for(int x=footprint.X;x<=footprint.Right;x++)for(int z=footprint.Z;z<=footprint.Top;z++)
            {
                if(!_townByTile.TryGetValue((x,z),out int current)||town>=0&&town!=current)return 1;
                town=current;
            }
            return town<0?1:1+_bonuses[town];
        }
    }
}
