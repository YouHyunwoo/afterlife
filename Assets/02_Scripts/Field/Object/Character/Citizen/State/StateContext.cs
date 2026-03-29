using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenStateContext : State.StateContext
    {
        public CitizenVisible CitizenVisible;
        public BuildSystem BuildSystem;

        public Func<Vector2Int, Vector2Int, bool> IsPassable;
        public Func<List<Vector2Int>> GetAllInfluencedPositions;
    }
}