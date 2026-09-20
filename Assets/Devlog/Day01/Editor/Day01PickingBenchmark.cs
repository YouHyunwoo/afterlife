using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Afterlife.Devlog.Day01.Editor
{
    // 명시적인 요청 파일이 있을 때만 한 번 실행한다. 씬/기존 에셋/임포터는 변경하지 않는다.
    [InitializeOnLoad]
    internal static class Day01PickingBenchmark
    {
        private const string Request = "Library/Day01PickingBenchmark.request";
        private const string Output = "Library/Day01PickingBenchmark.json";
        private const int Repeats = 31;

        [Serializable] private sealed class Entry
        {
            public string asset, source, mesh;
            public bool readable;
            public int vertices, triangles;
            public long payloadBytes;
            public double firstMs, medianMs, p95Ms;
            [NonSerialized] public Mesh input;
            [NonSerialized] public double[] samples = new double[Repeats];
        }

        [Serializable] private sealed class Report
        {
            public string utc, unity, processor, os;
            public string context = "Unity Editor, outside Play Mode; source meshes loaded before generation timing. Not a Player benchmark.";
            public int repeats = Repeats, unreadableMeshes;
            public double sourceResolutionMs, firstGenerationMs, medianGenerationMs, p95GenerationMs, maxGenerationMs;
            public long payloadBytes, medianManagedAllocationBytes;
            public double[] generationSamplesMs;
            public Entry[] entries;
        }

        static Day01PickingBenchmark() { EditorApplication.delayCall += RunIfRequested; }

        private static void RunIfRequested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += RunIfRequested; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request); // 실패하더라도 도메인 리로드마다 반복하지 않는다.
            try { Run(); }
            catch (Exception error)
            {
                File.WriteAllText(Output + ".error.txt", error.ToString());
                UnityEngine.Debug.LogException(error);
            }
        }

        private static void Run()
        {
            var watch = Stopwatch.StartNew();
            var entries = new List<Entry>();
            var seen = new HashSet<Mesh>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Day01SceneBuilder.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var surface in prefab.GetComponentsInChildren<ShowcasePickSurface>(true))
                {
                    var filter = surface.GetComponent<MeshFilter>();
                    var mesh = filter != null ? filter.sharedMesh : null;
                    if (mesh == null || !seen.Add(mesh)) continue;
                    if (!mesh.isReadable) throw new InvalidOperationException("Picking source is not readable: " + mesh.name);
                    entries.Add(new Entry { asset = path, source = AssetDatabase.GetAssetPath(mesh),
                        mesh = mesh.name, readable = mesh.isReadable, input = mesh });
                }
            }
            watch.Stop();
            var report = new Report { utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion,
                processor = SystemInfo.processorType, os = SystemInfo.operatingSystem,
                sourceResolutionMs = watch.Elapsed.TotalMilliseconds, entries = entries.ToArray(),
                unreadableMeshes = entries.Count(e => !e.readable), generationSamplesMs = new double[Repeats] };
            var outputs = new ShowcasePickMesh[entries.Count];
            var allocated = new long[Repeats];
            try
            {
                for (int run = -1; run < Repeats; run++)
                {
                    long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
                    double total = 0;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        watch.Restart();
                        outputs[i] = Generate(entries[i].input);
                        watch.Stop();
                        double ms = watch.Elapsed.TotalMilliseconds;
                        total += ms;
                        if (run < 0) entries[i].firstMs = ms;
                        else entries[i].samples[run] = ms;
                    }
                    long bytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
                    if (run < 0) report.firstGenerationMs = total;
                    else { report.generationSamplesMs[run] = total; allocated[run] = bytes; }
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var data = outputs[i];
                        if (run < 0)
                        {
                            entries[i].vertices = data.Vertices.Length;
                            entries[i].triangles = data.Triangles.Length / 3;
                            entries[i].payloadBytes = 12L * (data.Vertices.Length + data.Normals.Length)
                                + 4L * (data.Triangles.Length + data.TriangleMaterials.Length) + 16L * data.Colors.Length;
                        }
                        Object.DestroyImmediate(data); outputs[i] = null;
                    }
                }
                foreach (var entry in entries)
                { entry.medianMs = Percentile(entry.samples, .5); entry.p95Ms = Percentile(entry.samples, .95); }
                report.medianGenerationMs = Percentile(report.generationSamplesMs, .5);
                report.p95GenerationMs = Percentile(report.generationSamplesMs, .95);
                report.maxGenerationMs = report.generationSamplesMs.Max();
                Array.Sort(allocated); report.medianManagedAllocationBytes = allocated[Repeats / 2];
                report.payloadBytes = entries.Sum(e => e.payloadBytes);

                File.WriteAllText(Output, JsonUtility.ToJson(report, true));
                UnityEngine.Debug.Log($"Picking benchmark: {entries.Count} meshes, first {report.firstGenerationMs:F3} ms, "
                    + $"warm median {report.medianGenerationMs:F3} ms, p95 {report.p95GenerationMs:F3} ms, "
                    + $"Report: {Output}");
            }
            finally { foreach (var data in outputs) if (data != null) Object.DestroyImmediate(data); }
        }

        private static ShowcasePickMesh Generate(Mesh mesh) => ShowcasePickMesh.Create(mesh);

        private static double Percentile(double[] values, double fraction)
        {
            var sorted = (double[])values.Clone(); Array.Sort(sorted);
            return sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * fraction) - 1)];
        }
    }
}
