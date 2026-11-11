using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Core
{
    public enum SceneState
    {
        None,
        Title,
        Introduction,
        Main,
        InGame,
        GameClear,
        GameOver,
        Demo,
    }

    [System.Serializable]
    public class SceneStateScreenGroup
    {
        public SceneState SceneState;
        public UI.Screen Screen;
    }

    public class SceneManager : ManagerBase
    {
        [SerializeField] SceneStateScreenGroup[] sceneStateScreenGroups;
        public SceneState CurrentState;

        readonly Dictionary<SceneState, UI.Screen> sceneScreenMap = new();

        void Awake()
        {
            foreach (var group in sceneStateScreenGroups)
            {
                if (group.Screen == null)
                {
                    Debug.LogError($"SceneStateScreenGroup: {group.SceneState} has no screen assigned.");
                    continue;
                }

                sceneScreenMap[group.SceneState] = group.Screen;
            }

            sceneScreenMap[SceneState.None] = null;
        }

        public void ChangeState(SceneState nextState)
        {
            if (nextState == CurrentState)
            {
                Debug.LogWarning($"SceneManager: Attempted to change to the same state: {nextState}");
                return;
            }

            var previousState = CurrentState;

            if (previousState != SceneState.None)
            {
                var previousScreen = sceneScreenMap[previousState];
                var nextController = nextState == SceneState.None ? null : sceneScreenMap[nextState].Controller;
                previousScreen.Controller.OnSceneExited(nextState, nextController);
                previousScreen.Hide();
            }

            CurrentState = nextState;

            if (nextState != SceneState.None)
            {
                var nextScreen = sceneScreenMap[nextState];
                var previousController = previousState == SceneState.None ? null : sceneScreenMap[previousState].Controller;
                nextScreen.Show();
                nextScreen.Controller.OnSceneEntered(previousState, previousController);
            }
        }

        public void ChangeState(string nextStateName)
        {
            if (System.Enum.TryParse(nextStateName, out SceneState nextState))
            {
                ChangeState(nextState);
            }
            else
            {
                Debug.LogError($"SceneManager: Invalid SceneState name: {nextStateName}");
            }
        }
    }
}