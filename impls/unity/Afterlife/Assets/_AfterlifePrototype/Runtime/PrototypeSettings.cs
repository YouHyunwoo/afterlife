using UnityEngine;

namespace Afterlife.Prototype
{
    // 인스펙터 데이터에서 순수 모델 설정으로 복사한다. 모델은 에셋을 참조하지 않는다.
    [CreateAssetMenu(menuName = "Afterlife/Prototype Settings")]
    public sealed class PrototypeSettings : ScriptableObject
    {
        [Header("초기 밸런스 · 검증 전")]
        public float FirstPreparation = 60;
        public float Maintenance = 45;
        public float CursorRadius = .65f;
        public float HouseRadius = 3.8f;
        public float WoodPerSecond = 2;
        public float StonePerSecond = 1;
        public float CursorDamage = 8;
        public float RepairPerSecond = 12;
        public float WarriorSeconds = 10;
        public int WarriorMoney = 10;
        [Header("낮밤 · 시야 · 자동 기여")]
        public float DaySeconds = 35;
        public float DuskSeconds = 5;
        public float NightSeconds = 35;
        public float ContributionSeconds = 1;
        [Min(.1f)] public float ContributionPower = 1;
        public float CursorVision = 7;
        public float VillageVision = 9;
        public float VisionTrailSeconds = 1.4f;
        public float AutoConstruction = .25f;
        [Header("자원 모델 크기 · 새 플레이에 적용")]
        [Min(.1f)] public float TreeModelScale = 1.28f/1.8f;
        [Min(.1f)] public float RockModelScale = 1.20f/1.8f;
        [Header("자원 재생성")]
        public float ResourceSpawnSeconds = 12;
        public int TreeLimit = 780;
        public int RockLimit = 480;
        [Header("지도 생성 · 새 게임에 적용")]
        [Tooltip("0이면 새 게임마다 무작위 지도, 다른 값이면 같은 시드의 지도를 재현합니다.")]
        public int MapSeed = 0;
        public int MapRadius = 45;
        [Range(3,15)] public int PrototypeDays = 6;
        [Header("카메라 거리")]
        [Tooltip("카메라 위치에서 forward 방향으로 지형 기준 평면(y=0)까지의 거리. 플레이 중 변경 가능. 거리에 비례해 직교 표시 범위도 변경합니다(클수록 축소, 작을수록 확대).")]
        [Min(1f)] public float CameraGroundDistance = 22.825424f;
        [Header("화면 가장자리 거리별 카메라 속도 (픽셀)")]
        public float CameraEdgeBand = 160;
        public float CameraFastBand = 35;
        public float CameraMediumBand = 85;
        public float CameraSlowSpeed = 4;
        public float CameraMediumSpeed = 10;
        public float CameraMaxSpeed = 20;
        [Header("표시")]
        [Range(180, 720)] public int PixelHeight = 360;
        [Header("풀 색상 · 플레이 중 변경 가능")]
        [Tooltip("풀의 폭/높이 배율. 0.556은 Day01의 집 1.8배 배치에 대한 상대 크기 보정입니다.")]
        [Range(.1f,1.5f)] public float GrassSizeScale = 1f/1.8f;
        public bool GrassUsesTerrainColor = true;
        public Color GrassBaseColor = new Color(.30f,.40f,.23f);
        public Color GrassBeigeColor = new Color(.65f,.57f,.41f);
        public Color GrassDarkColor = new Color(.14f,.17f,.15f);
        public Color GrassPaleColor = new Color(.91f,.93f,.89f);
        public Color GrassTealColor = new Color(.24f,.48f,.42f);
        public Color GrassMossColor = new Color(.36f,.43f,.20f);
        public Color GrassPurpleColor = new Color(.47f,.30f,.48f);
        public Color GrassFlowerPink = new Color(.82f,.66f,.78f);
        public Color GrassFlowerYellow = new Color(.95f,.92f,.78f);
        [Header("효과음")]
        [Range(0, 1)] public float SoundVolume = .7f;
        public Balance ToBalance() => new Balance
        {
            FirstPreparation = FirstPreparation, Maintenance = Maintenance,
            CursorRadius = CursorRadius, HouseRadius = HouseRadius,
            WoodRate = WoodPerSecond, StoneRate = StonePerSecond,
            CursorDamage = CursorDamage, RepairRate = RepairPerSecond,
            WarriorSeconds = WarriorSeconds, WarriorMoney = WarriorMoney,
            DaySeconds = DaySeconds, DuskSeconds = DuskSeconds, NightSeconds = NightSeconds,
            PulseSeconds = ContributionSeconds, ContributionPower = ContributionPower, CursorVision = CursorVision, VillageVision = VillageVision,
            VisionTrailSeconds = VisionTrailSeconds, AutoConstruction = AutoConstruction, MapRadius = MapRadius, MapSeed=MapSeed, Days = PrototypeDays,ResourceSpawnSeconds=ResourceSpawnSeconds,TreeLimit=TreeLimit,RockLimit=RockLimit
        };
    }
}
