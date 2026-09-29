using Afterlife.Modules.Wind;
using UnityEngine;
namespace Afterlife.Devlog.Day02
{
    // 1번 공간의 바람만 소유한다. 다른 공간으로 이동하면 전역값을 복원한다.
    [ExecuteAlways]
    public sealed class Day02IslandEnvironment : MonoBehaviour
    {
        private WindFieldController _wind;
        private float _clock;
        private void OnEnable(){_wind=new WindFieldController();}
        private void Update(){
            if(Application.isPlaying){var view=FindFirstObjectByType<Day02Showcase>();if(view!=null&&view.Paused)return;_clock+=Time.unscaledDeltaTime;}
            else _clock=Time.realtimeSinceStartup;
            _wind?.Tick(_clock,.65f);
        }
        private void OnDisable(){_wind?.Dispose();_wind=null;}
    }
}
