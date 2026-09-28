using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Cards
{
    /// <summary>
    /// 一套卡组的静态配置。战斗开始时会把定义展开成带唯一实例 ID 的 RuntimeCard。
    /// </summary>
    [CreateAssetMenu(menuName = "Card Game/Deck", fileName = "Deck")]
    public sealed class DeckDefinition : ScriptableObject
    {
        [SerializeField] private string id = "new-deck";
        [SerializeField] private string displayName = "新卡组";
        [SerializeField] private List<CardDefinition> cards = new List<CardDefinition>();

        public string Id => id;
        public string DisplayName => displayName;
        public IReadOnlyList<CardDefinition> Cards => cards;

        public static DeckDefinition CreateRuntime(
            string id,
            string displayName,
            IReadOnlyList<CardDefinition> cards)
        {
            DeckDefinition deck = CreateInstance<DeckDefinition>();
            deck.name = id;
            deck.id = id;
            deck.displayName = displayName;
            deck.cards = cards == null ? new List<CardDefinition>() : new List<CardDefinition>(cards);
            return deck;
        }

        public static DeckDefinition CreateRuntime(
            string id,
            string displayName,
            params CardDefinition[] cards)
        {
            return CreateRuntime(id, displayName, cards ?? Array.Empty<CardDefinition>());
        }
    }
}
