using System;
using Afterlife.Modules.Threshold.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Threshold.Editor
{
    internal static class ThresholdAudioAuthoring
    {
        private const string Root = ThresholdLabInstaller.Root;
        internal static void Apply()
        {
            var bank = ThresholdLabInstaller.Asset<ThresholdAudioBank>(Root + "/Settings/ThresholdAudioBank.asset", () => ScriptableObject.CreateInstance<ThresholdAudioBank>());
            bank.CameraWhoosh = Clip("camera_whoosh", false); bank.WaterEnter = Clip("water_enter", false);
            bank.WaterExit = Clip("water_exit", false); bank.WaterLoop = Clip("water_loop", false);
            bank.TitleMusic = Clip("music_title", true); bank.PlayingMusic = Clip("music_day", true);
            bank.NightMusic = Clip("music_night", true);
            bank.GameOverMusic = Clip("music_over", true); bank.ClearMusic = Clip("music_clear", true);
            bank.Credits = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Audio/AudioCredits.txt");
            EditorUtility.SetDirty(bank);
            string audioPath = Root + "/Prefabs/ThresholdAudio.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(audioPath) == null)
            {
                var audio = new GameObject("ThresholdAudio");
                try
                {
                    var system = audio.AddComponent<ThresholdSoundscape>(); system.Bank = bank;
                    system.Motion = Source(audio.transform, "CameraMotion");
                    system.WaterTransitions = Source(audio.transform, "WaterTransitions");
                    system.WaterBed = Source(audio.transform, "WaterLoop");
                    system.MusicA = Source(audio.transform, "MusicA"); system.MusicB = Source(audio.transform, "MusicB");
                    PrefabUtility.SaveAsPrefabAsset(audio, audioPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(audio); }
            }
            Edit("ThresholdExperience", root =>
            {
                foreach (var old in root.GetComponentsInChildren<ThresholdAmbience>(true))
                { old.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(old); }
                if (root.GetComponentInChildren<ThresholdSoundscape>(true) == null)
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(audioPath), root.transform);
            });
            Edit("ModalPage", root => AddCredits(root, bank.Credits));
            AssetDatabase.SaveAssets();
        }
        private static AudioClip Clip(string name, bool music)
        {
            string path = Root + "/Audio/" + name + ".wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new InvalidOperationException("Audio asset not imported: " + path);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = .8f; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = !music;
            importer.defaultSampleSettings = settings; importer.forceToMono = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        private static AudioSource Source(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(AudioSource)); go.transform.SetParent(parent, false);
            var source = go.GetComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0;
            return source;
        }
        private static void AddCredits(GameObject root, TextAsset credits)
        {
            if (credits == null) throw new InvalidOperationException("Audio credits are missing.");
            var surface = root.transform.Find("ModalSurface");
            surface.Find("VolumeLabel").GetComponent<TMP_Text>().text = "배경음 · 효과음";
            var existing = root.transform.Find("AudioCreditsPanel");
            if (existing != null)
            {
                existing.Find("CreditsText").GetComponent<TMP_Text>().text = credits.text;
                return;
            }
            var panel = ThresholdLabUi.Rect("AudioCreditsPanel", root.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1380, 880));
            panel.pivot = new Vector2(.5f, .5f);
            panel.gameObject.AddComponent<Image>().color = new Color(.012f, .019f, .024f, 1);
            var font = surface.Find("VolumeLabel").GetComponent<TMP_Text>().font;
            var body = ThresholdLabUi.Rect("CreditsText", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            body.offsetMin = new Vector2(50, 105); body.offsetMax = new Vector2(-50, -40);
            var label = body.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = credits.text;
            label.fontSize = 22; label.color = new Color(.85f,.9f,.92f); label.raycastTarget = false;
            label.enableAutoSizing = true; label.fontSizeMin = 17; label.fontSizeMax = 22;
            var show = Button(surface, "AudioCreditsButton", "음악 · 효과음 출처", new Vector2(70, -448), new Vector2(610, 44), font);
            var close = Button(panel, "CloseCredits", "설정으로 돌아가기", new Vector2(50, -805), new Vector2(700, 48), font);
            UnityEventTools.AddBoolPersistentListener(show.onClick, panel.gameObject.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(close.onClick, panel.gameObject.SetActive, false);
            panel.gameObject.SetActive(false);
        }
        private static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size, TMP_FontAsset font)
        {
            var rect = ThresholdLabUi.Rect(name, parent, new Vector2(0,1), new Vector2(0,1), position, size); rect.pivot = new Vector2(0,1);
            var background = rect.gameObject.AddComponent<Image>(); background.color = new Color(.06f,.1f,.13f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            var child = ThresholdLabUi.Rect("Label", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var label = child.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = 22;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return button;
        }
        private static void Edit(string name, Action<GameObject> change)
        {
            string path = Root + "/Prefabs/" + name + ".prefab"; var root = PrefabUtility.LoadPrefabContents(path);
            try { change(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
