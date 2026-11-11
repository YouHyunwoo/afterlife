using System;
using System.Collections.Generic;
using Afterlife.Modules.EconomicSites;

namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        private EconomicSiteLayout _economicSites;

        public IReadOnlyList<EconomicSite> EconomicSites => _economicSites?.Sites ?? Array.Empty<EconomicSite>();

        // Terrain.Regenerate 직후 판마다 한 번 호출한다. 타이머·자금·건물 데이터는 지대에 저장하지 않는다.
        public void InitializeEconomicSites()
        {
            var configuration = new EconomicSiteSettings(Settings.FertileSiteCount, Settings.LeylineSiteCount,
                Settings.FertileSiteSize, Settings.LeylineSiteSize, Settings.EconomicSiteMinimumDistance);
            _economicSites = EconomicSiteLayout.Generate(Terrain.Radius, Terrain.Seed,
                (x, z) => Terrain.At(x, z) != TerrainKind.Water, 0, 0, configuration,
                (int)Math.Ceiling(Math.Max(0,Building(Kind.House).TerritoryRadius)));
        }

        public EconomicSiteKind EconomicSiteAt(int x, int z) =>
            _economicSites?.At(x, z) ?? EconomicSiteKind.None;

        public bool EconomicSiteCovers(EconomicSiteKind kind, int x, int z, int width, int depth) =>
            _economicSites != null && _economicSites.Covers(kind, x, z, width, depth);

        public int ProductionYieldMultiplier(Body body)
        {
            if (body == null) return 1;
            var kind = body.Kind == Kind.Farm ? EconomicSiteKind.Fertile :
                body.Kind == Kind.Altar || body.Kind == Kind.Sanctuary ? EconomicSiteKind.Leyline : EconomicSiteKind.None;
            if (kind == EconomicSiteKind.None) return 1;
            int width = Math.Max(1, body.Width), depth = Math.Max(1, body.Depth);
            int x = (int)Math.Floor(body.Position.X - (width - 1) * .5f + .5f);
            int z = (int)Math.Floor(body.Position.Z - (depth - 1) * .5f + .5f);
            return EconomicSiteCovers(kind, x, z, width, depth) ? 2 : 1;
        }

        public string EconomicSiteTooltip(Kind building, int x, int z, int width, int depth)
        {
            var kind = building == Kind.Farm ? EconomicSiteKind.Fertile :
                building == Kind.Altar || building == Kind.Sanctuary ? EconomicSiteKind.Leyline : EconomicSiteKind.None;
            if (!EconomicSiteCovers(kind, x, z, width, depth)) return string.Empty;
            return kind == EconomicSiteKind.Fertile ? "비옥한 땅 · 보리 생산량 2배" : "영맥 · 에테론 생산량 2배";
        }
    }
}
