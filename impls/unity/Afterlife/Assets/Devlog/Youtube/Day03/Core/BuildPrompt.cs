namespace Afterlife.Devlog.Day03
{
    // Pure presentation state; never places a building or changes the economy.
    public readonly struct BuildPrompt
    {
        public readonly string Title,Action;
        public readonly bool Blocked;
        private BuildPrompt(string title,string action,bool blocked=false){Title=title;Action=action;Blocked=blocked;}
        public static BuildPrompt Resolve(VillageTarget selected,bool hasTile,string error,bool purchased,bool expanded)
        {
            if(selected==VillageTarget.None)return expanded
                ?new BuildPrompt("마을 영역이 넓어졌습니다","새 집 주변도 마을 영역입니다")
                :new BuildPrompt("건설 모드","건물 버튼을 눌러 선택하세요");
            string name=selected==VillageTarget.Farm?"농장":"집";
            string objectName=selected==VillageTarget.Farm?"농장을":"집을";
            if(purchased)return new BuildPrompt(objectName+" 이미 배치했습니다","다른 건물을 선택하세요");
            if(!hasTile)return new BuildPrompt(name+" 선택됨",objectName+" 지을 타일을 선택하세요");
            if(error==null)return new BuildPrompt(name+" 선택됨","표시된 타일을 클릭해 "+objectName+" 지으세요");
            string action;
            switch(error) {
                case "물 타일에는 건설할 수 없습니다.":action="육지 타일을 선택하세요";break;
                case "모든 점유 타일이 마을 영역 안이어야 합니다.":action="마을 영역 안의 타일을 선택하세요";break;
                case "다른 오브젝트와 겹칩니다.":action="빈 타일을 선택하세요";break;
                case "골드가 부족합니다.":action="골드가 부족합니다";break;
                default:action=error;break;
            }
            return new BuildPrompt("여기에는 건설할 수 없습니다",action,true);
        }
    }
}
