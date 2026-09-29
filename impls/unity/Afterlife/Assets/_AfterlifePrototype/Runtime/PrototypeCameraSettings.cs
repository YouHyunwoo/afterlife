using Afterlife.Modules.CameraDistance;
using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Settings/Camera")]
    public sealed class PrototypeCameraSettings : ScriptableObject
    {
        [Header("Ground distance")]
        [Tooltip("새 게임·새 생의 시작 거리. 카메라 시선과 기준 지면(y=0) 사이 거리입니다."), Min(1f)]
        public float CameraGroundDistance = CameraDistanceProgression.DefaultStartDistance;
        [Tooltip("카메라가 멀어질 수 있는 최대 거리. 시작 거리보다 작으면 이 값을 우선합니다."), Min(1f)]
        public float CameraMaximumGroundDistance = CameraDistanceProgression.DefaultMaximumDistance;
        [Tooltip("초기 집 영역에서 이만큼 타일이 늘어나면 최대 거리에 도달합니다."), Min(1)]
        public int CameraExpansionTilesToMaximum = CameraDistanceProgression.DefaultExpansionTilesToMaximum;
        [Tooltip("영역 변화 후 목표 거리까지 이동하는 초당 속도. 0이면 현재 거리에서 멈춥니다."), Min(0f)]
        public float CameraDistanceFollowSpeed = CameraDistanceProgression.DefaultFollowSpeed;
        [Header("Edge movement")]
        public float CameraEdgeBand = 160;
        public float CameraFastBand = 35;
        public float CameraMediumBand = 85;
        public float CameraSlowSpeed = 4;
        public float CameraMediumSpeed = 10;
        public float CameraMaxSpeed = 20;
        public float DefaultUiSize=1, DefaultEdgeFraction=.25f, DefaultSpeedFactor=1;
    }
}
