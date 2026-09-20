using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class BuildingLocatorComponent
    {
        public Func<IEnumerable<Building>> GetBuildings;

        public bool FindNearest(Vector3 position, BuildingType buildingType, out Building nearestBuilding)
        {
            nearestBuilding = null;

            var buildings = GetBuildings?.Invoke();
            if (buildings == null) return false;

            var minDistance = float.MaxValue;
            foreach (var building in buildings)
            {
                if (building.BuildingType != buildingType) continue;

                var distance = Vector3.Distance(position, building.Position);
                if (distance < minDistance)
                {
                    nearestBuilding = building;
                    minDistance = distance;
                }
            }

            return nearestBuilding != null;
        }
    }
}
