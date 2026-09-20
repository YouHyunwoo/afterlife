using System;

namespace Afterlife.Prototype
{
    // Unity 참조 없이 발생 사실만 표현 계층으로 전달한다.
    public enum SoundCue
    {
        Wood, ResourceEmpty, Gold, Place, Work, Complete, Hit, Magic,
        Kill, Critical, BuildingHit, Collapse, Chest, Relic,
        Select, Cancel, Open, Error, Warning, Day, Result, Rebirth
    }

    public sealed partial class VillageModel
    {
        public event Action<SoundCue, Point?> SoundRequested;
        public event Action SoundReset;
        private void Sound(SoundCue cue, Point? position = null) => SoundRequested?.Invoke(cue, position);
    }
}
