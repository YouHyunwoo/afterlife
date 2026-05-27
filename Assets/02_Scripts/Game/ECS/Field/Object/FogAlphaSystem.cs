using System;
using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    // FogLayer 값을 읽어 엔티티 스프라이트 렌더러의 투명도를 갱신
    public class FogAlphaSystem : ISystem<ComponentManager>
    {
        public Func<float[,]> GetFogCells;

        public void Update(float deltaTime, ComponentManager cm)
        {
            var cells = GetFogCells();
            var width  = cells.GetLength(0);
            var height = cells.GetLength(1);

            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasFogAlpha[i] || !cm.HasEntity[i]) continue;

                var pos = cm.Entities[i].transform.position;
                var x = Mathf.FloorToInt(pos.x);
                var y = Mathf.FloorToInt(pos.y);

                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                cm.FogAlphas[i].SetFogAlpha(cells[x, y]);
            }
        }
    }
}
