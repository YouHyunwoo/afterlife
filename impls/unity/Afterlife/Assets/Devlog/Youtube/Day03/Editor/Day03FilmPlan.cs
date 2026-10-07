using System.Linq;
using Afterlife.Devlog.Cinematics;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day03.Editor
{
    // Day03-owned authoring specification. Runtime economy and common film playback stay independent.
    public static class Day03FilmPlan
    {
        private static readonly string[] Subtitles={"","채집에서 자동 수입으로","채집한 골드로 농장을 지어봅니다.",
            "농장은 집의 마을 영역 안에 지을 수 있습니다.","같은 8초 동안 자동 공사의 진행을 봅니다.",
            "같은 8초 동안 버튼을 눌러 공사를 돕습니다.","농장이 보리를 생산해 골드를 벌기 시작합니다.",
            "손을 놓아도 수입은 계속됩니다.","다음 집을 고르는 동안에도 수입이 들어옵니다.",
            "농장 수입으로 집을 짓고 마을을 넓힙니다.","집 한 채가 작은 마을이 됐습니다.",
            "다음엔 밤의 마을을 지켜봅니다.",""};
        public static void Apply(ShowcaseFilm film)
        {
            float[] durations={1,6,7,12,8,8,8,9,11,16,8,8,16};
            string[] titles={"Opening","VillageTeaser","HarvestInvestment","FarmPlacement","AutoConstruction",
                "HoldConstruction","FirstIncome","HandsOffIncome","HousePreview","HouseReinvestment",
                "VillageBeforeAfter","NightPreview","Credits"};
            var whole=Pose(new Vector3(0,.6f,.2f),4.2f);
            var harvest=Pose(new Vector3(-.35f,.7f,1.1f),3.5f);
            var farm=Pose(new Vector3(.35f,.5f,.05f),3.1f);
            var income=Pose(new Vector3(.65f,.45f,0),2.8f);
            var expansion=Pose(new Vector3(0,.55f,-.3f),3.8f);
            FilmPose[] poses={whole,whole,harvest,farm,farm,farm,income,income,expansion,expansion,whole,whole,whole};
            film.Shots=durations.Select((duration,i)=>new FilmShot{
                Title="Day03 "+(i+1).ToString("00")+" "+titles[i],Heading="",Duration=duration,Stage=0,
                From=poses[i],To=poses[i],Subtitle=Subtitles[i],SubtitleDelay=i==0||i==12?0:.4f,
                SubtitleDuration=Mathf.Max(.1f,duration-2.4f),AllowInteraction=i==2||i==3||i==5||i==8||i==9,
                FadeIn=i==0,FadeInDuration=.8f,FadeOut=i==12,FadeOutDuration=2,
                TransitionToNight=i==11,NightTransitionStart=0,NightTransitionDuration=8,Night=i==12,ShowCredits=i==12
            }).ToArray();
            // Every shot holds its own pose; framing changes only at hard cuts.
            ApplyOnScreenText(film);
            ApplyCreditsBlur(film);
            film.NarrationEnabled=false;film.BackgroundMusicEnabled=false;
            film.CreditsText="AFTERLIFE\nDay03 · 마을 건설과 첫 자동 수입\n\n기획 · 개발 · 영상\n1인 개발 기록\n\n나레이션 · 음악 적용 전 리허설";
        }
        public static void ApplyCreditsBlur(ShowcaseFilm film)
        {
            if(film==null)throw new System.ArgumentNullException(nameof(film));
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Devlog/Youtube/Modules/Cinematics/Runtime/FilmCreditsBlur.shader");
            if(shader==null)throw new System.InvalidOperationException("Day03 credits blur shader is missing.");
            film.CreditsBlurShader=shader;
            film.CreditsFadeSeconds=2;
            film.CreditsBlurRadius=5;
            film.CreditsDarkness=.38f;
        }
        public static void ApplyHeadings(ShowcaseFilm film)
        {
            string[] headings={"Day03 · 농장과 마을","Day03 · 농장과 마을",
                "채집 골드로 첫 투자","농장 짓기","공사 비교 · 자동 진행","공사 비교 · 직접 돕기",
                "첫 농장 수입","자동 수입","다음 집 고르기",
                "마을 넓히기","마을의 변화","첫 밤",""};
            if(film==null||film.Shots==null||film.Shots.Length!=headings.Length)
                throw new System.InvalidOperationException("Day03 headings require the current 13-shot film.");
            for(int i=0;i<headings.Length;i++)film.Shots[i].Heading=film.Shots[i].ShowCredits?"":headings[i];
        }
        public static void ApplyOnScreenText(ShowcaseFilm film)
        {
            ApplyHeadings(film);
            for(int i=0;i<Subtitles.Length;i++)film.Shots[i].Subtitle=Subtitles[i];
        }
        private static FilmPose Pose(Vector3 focus,float size)=>new FilmPose(focus,size){Offset=new Vector3(7,10,-12)};
    }
}
