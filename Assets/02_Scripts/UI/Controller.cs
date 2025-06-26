using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI
{
    public abstract class Controller : MonoBehaviour
    {
        public Screen Screen;
        public SceneState SceneState;

        public virtual void OnSceneEntered(SceneState previousSceneState, Controller previousScreen) { }
        public virtual void OnSceneExited(SceneState nextSceneState, Controller nextScreen) { }

        public virtual void SetUp() { }
        public virtual void TearDown() { }
        public virtual void RefreshView() { }
    }
}