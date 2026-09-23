using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // Surface가 살아 있는 동안 원본 메시별 데이터를 공유한다. 비활성 풀 객체도 재사용한다.
    internal static class ShowcasePickMeshCache
    {
        internal sealed class Lease
        {
            internal Mesh Source;
            internal ShowcasePickMesh Data;
            internal readonly HashSet<ShowcasePickSurface> Users = new HashSet<ShowcasePickSurface>();
        }

        private static readonly Dictionary<Mesh, Lease> Entries = new Dictionary<Mesh, Lease>();

        static ShowcasePickMeshCache()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += _ => SweepDestroyedOwners();
        }

        internal static Lease Acquire(Mesh source, ShowcasePickSurface owner)
        {
            if (!Entries.TryGetValue(source, out var entry))
            {
                entry = new Lease { Source = source, Data = ShowcasePickMesh.Create(source) };
                Entries.Add(source, entry);
            }
            entry.Users.Add(owner);
            return entry;
        }

        internal static void Release(Lease entry, ShowcasePickSurface owner)
        {
            if (entry == null) return;
            entry.Users.Remove(owner);
            entry.Users.RemoveWhere(user => user == null);
            if (entry.Users.Count > 0) return;
            if (!ReferenceEquals(entry.Source, null) && Entries.TryGetValue(entry.Source, out var current)
                && ReferenceEquals(entry, current)) Entries.Remove(entry.Source);
            Dispose(entry);
        }

        private static void SweepDestroyedOwners()
        {
            // 한 번도 활성화되지 않은 객체는 OnDestroy가 없을 수 있으므로 씬 해제 때도 정리한다.
            foreach (var entry in new List<Lease>(Entries.Values)) Release(entry, null);
        }

        private static void Dispose(Lease entry)
        {
            if (entry.Data != null)
            {
                if (Application.isPlaying) Object.Destroy(entry.Data);
                else Object.DestroyImmediate(entry.Data);
            }
            entry.Data = null;
        }

        // 도메인 리로드를 끈 플레이 진입에서도 이전 실행의 데이터를 유지하지 않는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach (var entry in Entries.Values) Dispose(entry);
            Entries.Clear();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorCleanup()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Reset;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Reset;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode) Reset();
        }
#endif
    }
}
