using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Model
{
    [Serializable]
    public class Light
    {
        public Vector2Int Location;
        public float Intensity;
        public float Range;
        public bool IsActive = true;
    }

    public class Fog
    {
        public Vector2Int Size;
        public float[,] FogGrid;
        public Transform[,] TransformGrid;
        public SpriteRenderer[,] SpriteRendererGrid;
        public List<Light> Lights = new();
        public bool IsDirty;
        public LinkedList<SpriteRenderer> PreviousActiveSpriteRenderers = new();
        public event Action<float[,]> OnFogUpdated;

        public void AddLight(Light light)
        {
            Lights.Add(light);
        }

        public void RemoveLight(Light light)
        {
            Lights.Remove(light);
        }

        public void Invalidate() => IsDirty = true;
        public void Update()
        {
            if (!IsDirty) { return; }

            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    FogGrid[x, y] = 1f;
                }
            }

            foreach (var spriteRenderer in PreviousActiveSpriteRenderers)
            {
                spriteRenderer.color = new Color(0, 0, 0, 1);
            }
            PreviousActiveSpriteRenderers.Clear();

            for (int i = 0; i < Lights.Count; i++)
            {
                var light = Lights[i];
                if (!light.IsActive) { continue; }

                var location = light.Location;
                var range = light.Range;
                var intensity = light.Intensity;

                for (var x = Mathf.FloorToInt(location.x - range); x <= location.x + range; x++)
                {
                    for (var y = Mathf.FloorToInt(location.y - range); y <= location.y + range; y++)
                    {
                        if (x < 0 || Size.x <= x || y < 0 || Size.y <= y) { continue; }

                        var distance = EuclideanDistance(location, new Vector2Int(x, y));
                        if (distance > range) { continue; }

                        float brightness;
                        if (distance < intensity - 1)
                        {
                            brightness = 1;
                        }
                        else
                        {
                            brightness = 1 - (distance - intensity + 1) / (range - intensity + 1);
                        }

                        var fog = 1 - brightness;
                        FogGrid[x, y] = Mathf.Min(FogGrid[x, y], fog);
                        SpriteRendererGrid[x, y].color = new Color(0, 0, 0, FogGrid[x, y]);
                        PreviousActiveSpriteRenderers.AddLast(SpriteRendererGrid[x, y]);
                    }
                }
            }

            IsDirty = false;
            OnFogUpdated?.Invoke(FogGrid);
        }

        float ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        float EuclideanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2));
        }

        float ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        public void FillFog()
        {
            for (var x = 0; x < Size.x; x++)
            {
                for (var y = 0; y < Size.y; y++)
                {
                    FogGrid[x, y] = 1;
                }
            }
        }

        public void ClearFog()
        {
            for (var x = 0; x < Size.x; x++)
            {
                for (var y = 0; y < Size.y; y++)
                {
                    FogGrid[x, y] = 0;
                }
            }
        }

        public void Dispose()
        {
            var mapWidth = FogGrid.GetLength(0);
            var mapHeight = FogGrid.GetLength(1);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (TransformGrid[x, y] != null)
                    {
                        UnityEngine.Object.Destroy(TransformGrid[x, y].gameObject);
                    }
                    TransformGrid[x, y] = null;
                    FogGrid[x, y] = 0;
                    SpriteRendererGrid[x, y] = null;
                }
            }

            OnFogUpdated = null;
            PreviousActiveSpriteRenderers.Clear();
            IsDirty = false;
            Lights.Clear();
            Lights = null;
            FogGrid = null;
            TransformGrid = null;
            SpriteRendererGrid = null;
            Size = Vector2Int.zero;
        }
    }
}