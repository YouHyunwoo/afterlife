using Afterlife.Modules.EconomicSites;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        // GeneratedMap의 자식이므로 맵 재생성/해제 시 표시도 함께 제거된다.
        private void BuildEconomicSiteMarkers()
        {
            foreach (var site in _model.EconomicSites)
            {
                var root = new GameObject(site.Kind == EconomicSiteKind.Fertile ? "Fertile ground" : "Leyline");
                root.transform.SetParent(_mapRoot.transform, false);
                Color field = site.Kind == EconomicSiteKind.Fertile
                    ? new Color(.37f, .66f, .25f) : new Color(.32f, .47f, .86f);
                Color accent = site.Kind == EconomicSiteKind.Fertile
                    ? new Color(.85f, .75f, .29f) : new Color(.61f, .79f, 1f);
                for (int dz = 0; dz < site.Size; dz++)
                for (int dx = 0; dx < site.Size; dx++)
                {
                    int x = site.X + dx, z = site.Z + dz;
                    float lift = GroundLift(x, z);
                    Part(root.transform, "Bonus tile", PrimitiveType.Cube,
                        new Vector3(x, lift + .014f, z), new Vector3(.92f, .018f, .92f), field, false);
                    if ((dx == 0 || dx == site.Size - 1) && (dz == 0 || dz == site.Size - 1))
                    {
                        var marker = Part(root.transform, "Site marker", PrimitiveType.Cube,
                            new Vector3(x, lift + .12f, z), new Vector3(.16f, .20f, .16f), accent, false);
                        marker.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                    }
                }
            }
        }
    }
}
