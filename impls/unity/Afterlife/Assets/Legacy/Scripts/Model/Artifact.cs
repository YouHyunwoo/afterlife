using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Model
{
    public class Artifact
    {
        public bool TrySelectArtifact(string artifactId)
        {
            var game = ServiceLocator.Get<GameManager>().Game;
            var player = game.Player;

            if (string.IsNullOrEmpty(artifactId)) { return false; }
            if (player.SelectedArtifactIds.Contains(artifactId)) { return false; }

            if (!ServiceLocator.Get<DataManager>().ArtifactDataDictionary.TryGetValue(artifactId, out var artifactData))
            {
                Debug.LogWarning($"Artifact data not found for ID: {artifactId}");
                return false;
            }

            if (!player.ConsumeArtifactSelection()) { return false; }

            player.SelectedArtifactIds.Add(artifactId);
            foreach (var parameter in artifactData.Parameters)
            {
                PlayerParameterApplicator.Apply(parameter);
            }

            return true;
        }
    }
}
