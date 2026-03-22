namespace Afterlife.Dev.Field
{
    public class BuildingVisible : ObjectVisible
    {
        protected float townAreaInfluenceRadius;

        public float TownAreaInfluenceRadius => townAreaInfluenceRadius;

        public override void SetData<TObjectData>(TObjectData data)
        {
            base.SetData(data);

            if (data is BuildingData buildingData)
            {
                townAreaInfluenceRadius = buildingData.TownAreaInfluenceRadius;
            }
        }
    }
}