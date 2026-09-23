using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Cinematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    // 데이터 파일이 준비되면 한 번만 교체한다. 이후 사용자가 편집한 컷은 Refresh로 덮지 않는다.
    [InitializeOnLoad]
    internal static class Day01NarrationSetup
    {
        private const string Root="Assets/Devlog/Youtube/Day01";
        private const string DataPath=Root+"/Data/Day01Narration.json";
        private static bool _running;
        // JsonUtility이 역직렬화하는 DTO 필드다.
#pragma warning disable CS0649
        [Serializable] private sealed class Manifest { public int version; public Shot[] shots; }
        [Serializable] private sealed class Shot
        {
            public string id,title; public int sourceShot,stage; public float duration;
            public bool night,interaction,credits,vision,transitionToNight; public Cue[] cues;
        }
        [Serializable] private sealed class Cue { public float start,duration; public string text,file; }
#pragma warning restore CS0649

        static Day01NarrationSetup()
        {
            EditorApplication.delayCall+=Apply;
            EditorSceneManager.sceneSaved+=_=>EditorApplication.delayCall+=Apply;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall+=Apply;};
        }

        internal static void Apply()
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) {EditorApplication.delayCall+=Apply;return;}
            var data=AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            var film=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(Day01FilmSetup.FilmPath);
            if (data==null || film==null || EditorUtility.IsDirty(film)) return;
            var music=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Music/Childhood-Day01.wav");
            if(music!=null && (film.BackgroundMusic==null || film.BackgroundMusic.name=="Childhood-Scott-Buckley"))
            {
                film.BackgroundMusic=music;film.MusicVolume=.08f;
                if(!(film.CreditsText ?? "").Contains("Childhood"))
                    film.CreditsText+="\n\n\n\n<size=36>배경음악</size>\n<size=54><b>Childhood</b></size>\n<size=36>Scott Buckley</size>\n<size=24>CC BY 4.0 · www.scottbuckley.com.au</size>";
                EditorUtility.SetDirty(film);AssetDatabase.SaveAssets();
            }
            var manifest=JsonUtility.FromJson<Manifest>(data.text);
            if (manifest==null || manifest.version<=film.NarrationVersion) return;
            _running=true;
            try
            {
                if (manifest.shots==null || manifest.shots.Length==0) throw new InvalidDataException("Narration has no shots.");
                var creditsShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Devlog/Youtube/Modules/Cinematics/Runtime/FilmCreditsBlur.shader");
                if(creditsShader==null) throw new InvalidDataException("Credits blur shader not imported.");
                // 사용자 구도와 원래 타이밍이 담긴 기존 에셋을 Unity API로 백업한다.
                string backup=Root+"/Data/Day01Film_BeforeNarration_v"+manifest.version+".asset";
                var source=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(backup) ?? film;
                var shots=new FilmShot[manifest.shots.Length];
                for (int i=0;i<shots.Length;i++)
                {
                    var plan=manifest.shots[i];
                    if (plan.sourceShot<0 || plan.sourceShot>=source.Shots.Length || plan.duration<=0 || plan.stage<0 || plan.stage>5)
                        throw new InvalidDataException("Invalid narration shot: "+plan.id);
                    var pose=source.Shots[plan.sourceShot];
                    var cues=(plan.cues ?? Array.Empty<Cue>()).Select(c=>new FilmCue {Start=c.start,Duration=c.duration,Text=c.text,Narration=LoadClip(c.file)}).ToArray();
                    foreach (var cue in cues)
                        if (cue.Start<0 || cue.Duration<cue.Narration.length || cue.Start+cue.Duration>plan.duration+.01f)
                            throw new InvalidDataException("Voice exceeds shot duration: "+plan.id);
                    for (int c=1;c<cues.Length;c++)
                        if (cues[c].Start<cues[c-1].Start+cues[c-1].Duration)
                            throw new InvalidDataException("Overlapping captions: "+plan.id);
                    shots[i]=new FilmShot {
                        Title=plan.title,Heading=plan.title,Stage=plan.stage,Duration=plan.duration,
                        From=pose.From,To=pose.To,Cues=cues,Subtitle="",Night=plan.night,AllowInteraction=plan.interaction,
                        FadeIn=i==0 || i==3 || i==13,FadeInDuration=.6f,
                        ShowCredits=plan.credits,ShowVision=plan.vision,TransitionToNight=plan.transitionToNight,
                        FadeOut=i==2 || i==12 || i==shots.Length-1,FadeOutDuration=i==shots.Length-1 ? 1f : .6f
                    };
                    if(plan.credits || plan.vision) {shots[i].From=pose.To;shots[i].To=pose.To;}
                }
                if (AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(backup)==null && !AssetDatabase.CopyAsset(Day01FilmSetup.FilmPath,backup))
                    throw new IOException("Could not back up existing film.");
                Undo.RecordObject(film,"첫 개발일지 나레이션 시퀀스 적용");
                film.Shots=shots; film.NarrationVolume=.85f;film.MusicVolume=.10f;
                film.NarrationDisclosure="";
                film.CreditsBlurShader=creditsShader;
                if(string.IsNullOrEmpty(film.CreditsText)) film.CreditsText="<size=36>나레이션</size>\n<size=54><b>Supertonic 3</b></size>\n<size=24>AI 생성 음성</size>";
                film.CreditsFadeSeconds=2;film.CreditsDarkness=.38f;film.CreditsBlurRadius=5;
                film.NarrationVersion=manifest.version;
                EditorUtility.SetDirty(film); AssetDatabase.SaveAssets();
                string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../devlog/youtube/records/film-narration-unity.json"));
                File.WriteAllText(report,"{\"version\":"+film.NarrationVersion+",\"shots\":"+shots.Length+",\"clips\":"+shots.Sum(s=>s.Cues.Length)+",\"duration\":"+film.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"savedAtUtc\":\""+DateTime.UtcNow.ToString("O")+"\",\"backup\":\""+backup+"\"}");
                Debug.Log("Day01 narrated film saved. F8 plays captions and narration; Space pauses both. Original film backed up at "+backup);
            }
            catch (Exception error) {Debug.LogException(error);}
            finally {_running=false;}
        }

        private static AudioClip LoadClip(string filename)
        {
            if (string.IsNullOrEmpty(filename) || Path.GetFileName(filename)!=filename) throw new InvalidDataException("Invalid voice filename.");
            string path=Root+"/Audio/Narration/"+filename;
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer==null) throw new FileNotFoundException("Narration not imported: "+path);
            var settings=importer.defaultSampleSettings;
            if (settings.loadType!=AudioClipLoadType.DecompressOnLoad || settings.compressionFormat!=AudioCompressionFormat.PCM || !settings.preloadAudioData)
            {
                settings.loadType=AudioClipLoadType.DecompressOnLoad; settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.preloadAudioData=true; importer.defaultSampleSettings=settings; importer.loadInBackground=false;
                importer.SaveAndReimport();
            }
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip==null || clip.length<=0) throw new InvalidDataException("Invalid voice clip: "+path);
            return clip;
        }
    }

    internal sealed class Day01NarrationAssets : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            if (imported.Any(p=>p.EndsWith("Day01Narration.json",StringComparison.Ordinal) || p.StartsWith("Assets/Devlog/Youtube/Day01/Audio/",StringComparison.Ordinal)))
                EditorApplication.delayCall+=Day01NarrationSetup.Apply;
        }
    }
}
