using System;
using System.Collections.Generic;
namespace Afterlife.Prototype
{
    public sealed class ConstructionRule
    {
        public Kind Id,RequiredBuilding;
        public int GoldCost;
        public bool RequiresBuilding;
        public string DisplayName;
        public BuildingStats Stats;
    }
    public sealed partial class VillageModel
    {
        private readonly Dictionary<Kind,ConstructionRule> _constructionRules=new Dictionary<Kind,ConstructionRule>();
        public void ConfigureConstruction(IEnumerable<ConstructionRule> rules)
        {
            _constructionRules.Clear();
            foreach(var rule in rules)
            {
                if(rule.GoldCost<0||_constructionRules.ContainsKey(rule.Id)||rule.Stats!=null&&(rule.Stats.Health<=0||rule.Stats.ConstructionSeconds<=0||rule.Stats.Width<1||rule.Stats.Depth<1||rule.Stats.Production!=ProductionKind.None&&(rule.Stats.ProductionSeconds<=0||rule.Stats.ProductionAmount<1)))throw new ArgumentException("건설 데이터 수치/중복 ID 오류: "+rule.Id);
                _constructionRules.Add(rule.Id,rule);
            }
        }
        public int ConstructionPrice(Kind kind)=>_constructionRules.TryGetValue(kind,out var rule)?rule.GoldCost:BuildPrice(kind);
        public BuildingStats Building(Kind kind)=>_constructionRules.TryGetValue(kind,out var rule)&&rule.Stats!=null?rule.Stats:BuildingStats.Defaults(kind);
        public string DisplayName(Kind kind)=>_constructionRules.TryGetValue(kind,out var rule)&&!string.IsNullOrEmpty(rule.DisplayName)?rule.DisplayName:BodiesKindIsUnit(kind)?Rules.Units.Find(kind).DisplayName:Name(kind);
        private static bool BodiesKindIsUnit(Kind kind)=>kind>=Kind.Warrior&&kind<=Kind.Animal||IsHostile(kind);
        public string ConstructionRequirement(Kind kind)
        {
            if(!Building(kind).DirectBuild)return "합체 전용 또는 사용하지 않는 건물입니다.";
            if((kind==Kind.KnightHall||kind==Kind.MageTower||kind==Kind.MagicTower)&&!HasAltar)return "제단 필요";
            if(_constructionRules.TryGetValue(kind,out var rule))return rule.RequiresBuilding&&!(rule.RequiredBuilding==Kind.Altar?HasAltar:Has(rule.RequiredBuilding))?Name(rule.RequiredBuilding)+" 필요":null;
            return null;
        }
    }
}
