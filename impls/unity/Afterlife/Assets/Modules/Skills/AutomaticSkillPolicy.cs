using System;

namespace Afterlife.Modules.Skills
{
    /// <summary>Game-independent automatic activation conditions supplied by the composition layer.</summary>
    public sealed class AutomaticSkillPolicy : ISkillActivationPolicy
    {
        private readonly Func<bool> _available;
        private readonly Func<string, bool> _hasTarget;
        public AutomaticSkillPolicy(Func<bool> available, Func<string, bool> hasTarget)
        { _available = available ?? throw new ArgumentNullException(nameof(available)); _hasTarget = hasTarget ?? throw new ArgumentNullException(nameof(hasTarget)); }
        public bool CanActivate(string id) => _available() && _hasTarget(id);
    }
}
