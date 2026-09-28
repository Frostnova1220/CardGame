using System;
using System.Collections.Generic;
using CardGame.Cards;

namespace CardGame.Battle
{
    /// <summary>
    /// 一个参战者在对局中的全部可变状态。当前版本以英雄/角色为单位，
    /// 后续可以把随从、装备、光环等对象集合挂在这里，或抽出统一的 BattleEntity。
    /// </summary>
    public sealed class BattleActorState
    {
        private readonly List<RuntimeCard> drawPile = new List<RuntimeCard>();
        private readonly List<RuntimeCard> hand = new List<RuntimeCard>();
        private readonly List<RuntimeCard> discardPile = new List<RuntimeCard>();

        public string Id { get; }
        public string DisplayName { get; }
        public int MaxHealth { get; }
        public int Health { get; internal set; }
        public int Block { get; internal set; }
        public int Energy { get; internal set; }
        public int MaxEnergy { get; }
        public int Poison { get; internal set; }
        public IReadOnlyList<RuntimeCard> DrawPile => drawPile;
        public IReadOnlyList<RuntimeCard> Hand => hand;
        public IReadOnlyList<RuntimeCard> DiscardPile => discardPile;
        public bool IsDead => Health <= 0;

        internal List<RuntimeCard> MutableDrawPile => drawPile;
        internal List<RuntimeCard> MutableHand => hand;
        internal List<RuntimeCard> MutableDiscardPile => discardPile;

        public BattleActorState(
            string id,
            string displayName,
            int maxHealth,
            int maxEnergy)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Actor id is required.", nameof(id)) : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            MaxHealth = Math.Max(1, maxHealth);
            Health = MaxHealth;
            MaxEnergy = Math.Max(0, maxEnergy);
            Energy = MaxEnergy;
        }

        internal void PrepareForTurn()
        {
            Block = 0;
            Energy = MaxEnergy;
        }

        internal void ChangeHealth(int delta)
        {
            Health = Math.Max(0, Math.Min(MaxHealth, Health + delta));
        }

        internal void AddBlock(int amount)
        {
            Block = Math.Max(0, Block + amount);
        }

        internal void AddPoison(int amount)
        {
            Poison = Math.Max(0, Poison + amount);
        }
    }
}
