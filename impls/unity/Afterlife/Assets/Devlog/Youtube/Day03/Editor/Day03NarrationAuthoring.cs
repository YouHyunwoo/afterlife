using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Cinematics;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day03.Editor
{
    // Episode data connects to the shared film player without rebuilding shots or the scene.
    public static class Day03NarrationAuthoring
    {
        private const string Root="Assets/Devlog/Youtube/Day03";
        private const string FilmPath=Root+"/Data/Day03Film.asset";
        private const string ManifestPath=Root+"/Data/Day03Narration.json";
        private static readonly string[] ShotIds={"opening","village-teaser","harvest-investment","farm-placement",
            "auto-construction","hold-construction","first-income","hands-off-income","house-preview",
            "house-reinvestment","village-before-after","night-preview","credits"};
#pragma warning disable CS0649
        [Serializable] private sealed class Manifest {public int version;public float duration;public Shot[] shots;}
        [Serializable] private sealed class Shot {public string id;public float start,duration;public Cue[] cues;}
        [Serializable] private sealed class Cue {public float start,duration,audioSeconds;public string text,file;}
#pragma warning restore CS0649

        public static string ApplyExisting()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var film=AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(FilmPath);
            if(film==null)throw new FileNotFoundException("Day03 film is missing.");
            string backup=Root+"/Data/Day03Film_BeforeNarration.asset";
            if(film.NarrationVersion==0&&AssetDatabase.LoadAssetAtPath<ShowcaseFilm>(backup)==null
                &&!AssetDatabase.CopyAsset(FilmPath,backup))throw new IOException("Cannot back up the silent film.");
            if(!ApplyPrepared(film))throw new FileNotFoundException("Day03 narration manifest is missing.");
            EditorUtility.SetDirty(film);
            AssetDatabase.SaveAssets();
            return "SUCCESS: Day03 narration v"+film.NarrationVersion+"; "+film.Shots.Sum(s=>s.Cues.Length)
                +" clips; "+film.Duration+" seconds; original poses and interaction preserved. Play not run.";
        }

        internal static bool ApplyPrepared(ShowcaseFilm film)
        {
            var data=AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            if(data==null)return false;
            if(film==null||film.Shots==null||film.Shots.Length!=ShotIds.Length||!film.Shots.Last().ShowCredits)
                throw new InvalidDataException("Narration requires the current Day03 13-shot film.");
            var manifest=JsonUtility.FromJson<Manifest>(data.text);
            if(manifest==null||manifest.version<1||manifest.shots==null||manifest.shots.Length!=ShotIds.Length)
                throw new InvalidDataException("Invalid Day03 narration manifest.");
            var cues=new FilmCue[ShotIds.Length][];
            float timeline=0;
            for(int i=0;i<ShotIds.Length;i++) {
                var shot=manifest.shots[i];
                if(shot==null||shot.id!=ShotIds[i]||!Finite(shot.duration)||shot.duration<=0
                    ||!Finite(shot.start)||Mathf.Abs(shot.start-timeline)>.02f)
                    throw new InvalidDataException("Invalid narration shot order/timing: "+ShotIds[i]);
                if((i==4||i==5)&&Mathf.Abs(shot.duration-8)>.001f)
                    throw new InvalidDataException("Both construction comparisons must remain eight seconds.");
                cues[i]=(shot.cues??Array.Empty<Cue>()).Select(c=>LoadCue(c,shot.duration)).ToArray();
                for(int n=1;n<cues[i].Length;n++)
                    if(cues[i][n].Start<cues[i][n-1].Start+cues[i][n-1].Duration)
                        throw new InvalidDataException("Overlapping narration cues: "+shot.id);
                if((i==0||i==1||i==12)&&cues[i].Length!=0)
                    throw new InvalidDataException("Opening, teaser and credits must have no narration.");
                timeline+=shot.duration;
            }
            if(!Finite(manifest.duration)||Mathf.Abs(timeline-manifest.duration)>.02f)
                throw new InvalidDataException("Narration total duration does not match shots.");
            // Validate every imported clip before changing the film's timeline.
            Day03FilmPlan.ApplyCreditsBlur(film);
            for(int i=0;i<ShotIds.Length;i++) {
                film.Shots[i].Duration=manifest.shots[i].duration;
                film.Shots[i].Cues=cues[i];
                if(cues[i].Length>0)film.Shots[i].Subtitle="";
            }
            film.NarrationVersion=manifest.version;
            film.NarrationEnabled=true;
            film.NarrationVolume=.85f;
            film.NarrationDisclosure="";
            Day03FilmPlan.ApplyEndingAudio(film);
            return true;
        }

        private static FilmCue LoadCue(Cue cue,float shotDuration)
        {
            if(cue==null||string.IsNullOrWhiteSpace(cue.text)||string.IsNullOrEmpty(cue.file)
                ||Path.GetFileName(cue.file)!=cue.file||Path.GetExtension(cue.file)!=".wav"
                ||!Finite(cue.start)||!Finite(cue.duration)||!Finite(cue.audioSeconds)||cue.start<0
                ||cue.audioSeconds<=0||cue.duration<cue.audioSeconds||cue.start+cue.duration>shotDuration+.01f)
                throw new InvalidDataException("Invalid Day03 narration cue.");
            string path=Root+"/Audio/Narration/"+cue.file;
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(importer==null)throw new FileNotFoundException("Narration WAV not imported: "+path);
            var settings=importer.defaultSampleSettings;
            if(settings.loadType!=AudioClipLoadType.DecompressOnLoad||settings.compressionFormat!=AudioCompressionFormat.PCM
                ||!settings.preloadAudioData||importer.loadInBackground) {
                settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;
                importer.loadInBackground=false;
                importer.SaveAndReimport();
            }
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if(clip==null||Mathf.Abs(clip.length-cue.audioSeconds)>.02f)
                throw new InvalidDataException("Narration WAV duration differs from manifest: "+path);
            return new FilmCue{Start=cue.start,Duration=cue.duration,Text=cue.text,Narration=clip};
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
