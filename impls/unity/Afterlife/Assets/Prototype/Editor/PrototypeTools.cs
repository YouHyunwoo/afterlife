using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Afterlife.Prototype.Editor
{
    public static class PrototypeTools
    {
        private const string Root = "Assets/Prototype";
        // CLI 진입점: 기존 Build Settings 씬 목록을 사용하거나 변경하지 않는다.
        public static void BuildWindows()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Root+"/Scenes/"+PrototypeScene.SceneName+".unity")==null)
                throw new BuildFailedException("저장된 프로토타입 씬이 없습니다.");
            if(AssetDatabase.LoadAssetAtPath<PrototypeSettings>(Root+"/Resources/AfterlifePrototype/Settings.asset")==null)
                throw new BuildFailedException("프로토타입 Settings.asset이 없습니다.");
            foreach(string animal in new[]{"Deer","Rabbit","Boar","Fox"})
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Resources/AfterlifePrototype/Animals/"+animal+".fbx");
                if(asset==null||asset.GetComponentsInChildren<MeshFilter>().Length==0)throw new BuildFailedException("Blender animal import failed: "+animal);
                Debug.Log("Blender animal imported: "+animal+" · meshes "+asset.GetComponentsInChildren<MeshFilter>().Length);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "/Resources/AfterlifePrototype/Pipeline.asset");
            if(pipeline==null)throw new BuildFailedException("프로토타입 Pipeline.asset이 없습니다.");
            var previousGraphics = GraphicsSettings.defaultRenderPipeline;
            int previousLevel = QualitySettings.GetQualityLevel();
            var previousPipelines = new RenderPipelineAsset[QualitySettings.names.Length];
            for (int i = 0; i < previousPipelines.Length; i++) previousPipelines[i] = QualitySettings.GetRenderPipelineAssetAt(i);
            var target = UnityEditor.Build.NamedBuildTarget.Standalone;
            var previousBackend = PlayerSettings.GetScriptingBackend(target);
            string output = Path.GetFullPath("Builds/PixelPrototypeWindows/AfterlifePrototype.exe");
            PrepareEmptyOutputDirectory(Path.GetDirectoryName(output));
            try
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                for (int i = 0; i < previousPipelines.Length; i++)
                { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
                QualitySettings.SetQualityLevel(previousLevel, false);
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Root + "/Scenes/" + PrototypeScene.SceneName + ".unity" },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                string summary = $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\nOutput: {output}\n";
                File.WriteAllText("Builds/PixelPrototypeWindows/build-summary.txt", summary);
                Debug.Log(summary);
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors > 0) throw new BuildFailedException(summary);
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = previousGraphics;
                for (int i = 0; i < previousPipelines.Length; i++)
                { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = previousPipelines[i]; }
                QualitySettings.SetQualityLevel(previousLevel, false);
                PlayerSettings.SetScriptingBackend(target, previousBackend);
                AssetDatabase.SaveAssets();
            }
        }

        private static void PrepareEmptyOutputDirectory(string outputDirectory)
        {
            string buildsRoot = Path.GetFullPath("Builds").TrimEnd(Path.DirectorySeparatorChar);
            string expected = Path.Combine(buildsRoot, "PixelPrototypeWindows");
            if (!string.Equals(outputDirectory, expected, System.StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Unexpected build output directory: " + outputDirectory);
            if (Directory.Exists(buildsRoot) && (File.GetAttributes(buildsRoot) & FileAttributes.ReparsePoint) != 0)
                throw new BuildFailedException("Builds root is a link: " + buildsRoot);
            if (Directory.Exists(outputDirectory) && (File.GetAttributes(outputDirectory) & FileAttributes.ReparsePoint) != 0)
                throw new BuildFailedException("Build output directory is a link: " + outputDirectory);
            Directory.CreateDirectory(outputDirectory);
            var pending = new System.Collections.Generic.Stack<string>();
            pending.Push(outputDirectory);
            while (pending.Count > 0)
            {
                foreach (string entry in Directory.GetFileSystemEntries(pending.Pop()))
                {
                    var attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                        throw new BuildFailedException("Build output contains a link: " + entry);
                    if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
                }
            }
            foreach (string file in Directory.GetFiles(outputDirectory)) File.Delete(file);
            foreach (string directory in Directory.GetDirectories(outputDirectory))
                Directory.Delete(directory, true);
        }
    }
}
