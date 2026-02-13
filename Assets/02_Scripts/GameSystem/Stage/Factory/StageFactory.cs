namespace Afterlife.GameSystem.Stage
{
    public static class StageFactory
    {
        public static Model.Stage Create(Data.Stage stageData)
        {
            var mapData = stageData.MapData;
            var map = MapFactory.Create(mapData);

            return new Model.Stage
            {
                Data = stageData,
                Map = map,
            };
        }
    }
}