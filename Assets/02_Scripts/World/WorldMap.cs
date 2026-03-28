using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class WorldMap
    {
        private readonly Vector2Int _size; // * 맵 크기
        private readonly Dictionary<WorldMapLayerType, IWorldMapLayer> _layers; // * 레이어

        public Vector2Int Size => _size;

        public WorldMap(Vector2Int size, Dictionary<WorldMapLayerType, IWorldMapLayer> layers)
        {
            _size = size;
            _layers = layers;
        }

        public bool GetLayerByType<TLayer>(WorldMapLayerType layerType, out TLayer layer) where TLayer : IWorldMapLayer
        {
            layer = default;
            if (!_layers.ContainsKey(layerType)) return false;
            if (_layers[layerType] is not TLayer l) return false;
            layer = l;
            return true;
        }
    }
}