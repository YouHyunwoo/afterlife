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

    public class SceneManager : ManagerBase
    {
        [SerializeField] UI.Controller[] controllers;

        public Dictionary<SceneState, UI.Controller> SceneControllerMap = new();
        public SceneState CurrentState;

        void Awake()
        {
            SceneControllerMap[SceneState.None] = null;

            foreach (var controller in controllers)
            {
                if (controller.SceneState == SceneState.None)
                {
                    throw new System.Exception($"Controller: {controller.name} does not have a valid SceneState.");
                }

                SceneControllerMap[controller.SceneState] = controller;
            }
        }

        public void ChangeState(SceneState nextState)
        {
            if (nextState == CurrentState)
            {
                Debug.LogWarning($"SceneManager: Attempted to change to the same state: {nextState}");
                return;
            }

            var previousSceneState = CurrentState;

            if (previousSceneState != SceneState.None)
            {
                SceneControllerMap[previousSceneState].OnSceneExited(nextState, SceneControllerMap[nextState]);
                SceneControllerMap[previousSceneState].Screen.Hide();
            }

            CurrentState = nextState;

            if (nextState != SceneState.None)
            {
                SceneControllerMap[nextState].Screen.Show();
                SceneControllerMap[nextState].OnSceneEntered(previousSceneState, SceneControllerMap[previousSceneState]);
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