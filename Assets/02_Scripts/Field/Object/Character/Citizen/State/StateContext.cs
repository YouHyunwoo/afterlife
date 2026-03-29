using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenStateContext : State.StateContext
    {
        public CitizenVisible CitizenVisible;
        public TownAreaSystem TownAreaSystem;
        public BuildSystem BuildSystem;

        public Func<Vector2Int, Vector2Int, bool> IsPassable;
    }
}