using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Model
{
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
        public event Action<float[,]> OnFogUpdated;
        public LinkedList<SpriteRenderer> PreviousActiveSpriteRenderers = new();

        public void AddLight(Light light)
        {
            Lights.Add(light);
        }

        public void RemoveLight(Light light)
        {
            Lights.Remove(light);
        }

        public void Invalidate() => IsDirty = true;
        public void Update() {
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

                        var distance = Mathf.Abs(location.x - x) + Mathf.Abs(location.y - y);
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
    }
}