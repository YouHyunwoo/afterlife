using System;
using System.Collections.Generic;
namespace Afterlife.Prototype
{
    public sealed class ConstructionRule
    {
        public Kind Id,RequiredBuilding;
        public int GoldCost;
        public bool RequiresBuilding;
    }
    public sealed partial class VillageModel
    {
        private readonly Dictionary<Kind,ConstructionRule> _constructionRules=new Dictionary<Kind,ConstructionRule>();
        public void ConfigureConstruction(IEnumerable<ConstructionRule> rules)
        {
            _constructionRules.Clear();
            foreach(var rule in rules)
            {
                if(rule.GoldCost<0||_constructionRules.ContainsKey(rule.Id))throw new ArgumentException("건설 데이터 가격/중복 ID 오류: "+rule.Id);
                _constructionRules.Add(rule.Id,rule);
            }
        }
        public int ConstructionPrice(Kind kind)=>_constructionRules.TryGetValue(kind,out var rule)?rule.GoldCost:BuildPrice(kind);
        public string ConstructionRequirement(Kind kind)
        {
            if(_constructionRules.TryGetValue(kind,out var rule))return rule.RequiresBuilding&&!Has(rule.RequiredBuilding)?Name(rule.RequiredBuilding)+" 필요":null;
            return kind==Kind.MagicTower&&!Has(Kind.Altar)?"제단 필요":null;
        }
    }
}
