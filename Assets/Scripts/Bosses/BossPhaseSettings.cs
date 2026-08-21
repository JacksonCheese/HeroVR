using System;
using UnityEngine;

namespace HeroVR.Bosses
{
    [Serializable]
    public struct BossPhaseSettings
    {
        public BossPhaseSettings(
            float healthThreshold,
            int minionCount,
            int minionWaveGroup)
        {
            this.healthThreshold = healthThreshold;
            this.minionCount = minionCount;
            this.minionWaveGroup = minionWaveGroup;
        }

        [Range(0f, 1f)] public float healthThreshold;
        [Min(0)] public int minionCount;
        [Min(0)] public int minionWaveGroup;
    }
}
