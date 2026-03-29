using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemyStateContext : State.StateContext
    {
        public EnemyVisible EnemyVisible;

        public Func<List<Vector2Int>> GetAllInfluencedPositions;
    }
}