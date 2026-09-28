using System;
using UnityEngine;

namespace CardGame.Persistence
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int victories;
        public int defeats;
        public int currentStreak;
        public int bestStreak;
    }
}
