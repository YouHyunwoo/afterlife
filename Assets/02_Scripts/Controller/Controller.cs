using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class Controller : MonoBehaviour
    {
        public StageController StageController;

        public void StartGame() {
            StageController.StartStage();
        }
    }
}