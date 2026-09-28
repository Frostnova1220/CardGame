using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Cards
{
    /// <summary>
    /// 开箱即用的演示内容。正式项目可以删除这个工厂，改为在 Inspector 中配置 ScriptableObject 资产。
    /// </summary>
    public static class DemoContentFactory
    {
        public static DeckDefinition CreatePlayerDeck()
        {
            CardDefinition strike = CreateDamageCard("strike", "斩击", "造成 6 点伤害。", 1, 6);
            CardDefinition guard = CreateBlockCard("guard", "格挡", "获得 6 点格挡。", 1, 6);
            CardDefinition heavyStrike = CreateDamageCard("heavy-strike", "重击", "造成 13 点伤害。", 2, 13);
            CardDefinition bandage = CreateHealCard("bandage", "绷带", "恢复 6 点生命。", 1, 6);
            CardDefinition venom = CardDefinition.CreateRuntime(
                "venom",
                "毒刃",
                "造成 2 点伤害，并施加 3 层中毒。",
                1,
                CardRarity.Uncommon,
                CardType.Attack,
                new CardEffectData(CardEffectType.Damage, CardTarget.Opponent, 2),
                new CardEffectData(CardEffectType.Poison, CardTarget.Opponent, 3));
            CardDefinition quickDraw = CardDefinition.CreateRuntime(
                "quick-draw",
                "快速思考",
                "抽 2 张牌。",
                0,
                CardRarity.Uncommon,
                CardType.Skill,
                new CardEffectData(CardEffectType.DrawCards, CardTarget.Self, 2));
            CardDefinition focus = CardDefinition.CreateRuntime(
                "focus",
                "专注",
                "获得 1 点能量，抽 1 张牌。",
                0,
                CardRarity.Rare,
                CardType.Power,
                new CardEffectData(CardEffectType.GainEnergy, CardTarget.Self, 1),
                new CardEffectData(CardEffectType.DrawCards, CardTarget.Self, 1));

            List<CardDefinition> cards = new List<CardDefinition>();
            AddCopies(cards, strike, 5);
            AddCopies(cards, guard, 3);
            AddCopies(cards, heavyStrike, 2);
            AddCopies(cards, bandage, 2);
            AddCopies(cards, venom, 1);
            AddCopies(cards, quickDraw, 1);
            AddCopies(cards, focus, 1);

            return DeckDefinition.CreateRuntime("starter-player", "冒险者初始卡组", cards);
        }

        public static DeckDefinition CreateEnemyDeck()
        {
            CardDefinition claw = CreateDamageCard("enemy-claw", "爪击", "造成 5 点伤害。", 1, 5);
            CardDefinition ironWall = CreateBlockCard("enemy-iron-wall", "硬化", "获得 7 点格挡。", 1, 7);
            CardDefinition bite = CardDefinition.CreateRuntime(
                "enemy-bite",
                "汲取",
                "造成 8 点伤害，并恢复 4 点生命。",
                2,
                CardRarity.Uncommon,
                CardType.Attack,
                new CardEffectData(CardEffectType.Damage, CardTarget.Opponent, 8),
                new CardEffectData(CardEffectType.Heal, CardTarget.Self, 4));
            CardDefinition venom = CardDefinition.CreateRuntime(
                "enemy-venom",
                "毒液",
                "施加 3 层中毒。",
                1,
                CardRarity.Uncommon,
                CardType.Skill,
                new CardEffectData(CardEffectType.Poison, CardTarget.Opponent, 3));
            CardDefinition crush = CreateDamageCard("enemy-crush", "碾压", "造成 12 点伤害。", 2, 12);

            List<CardDefinition> cards = new List<CardDefinition>();
            AddCopies(cards, claw, 6);
            AddCopies(cards, ironWall, 4);
            AddCopies(cards, bite, 3);
            AddCopies(cards, venom, 2);
            AddCopies(cards, crush, 2);

            return DeckDefinition.CreateRuntime("enemy-basic", "遗迹守卫卡组", cards);
        }

        private static CardDefinition CreateDamageCard(
            string id,
            string displayName,
            string description,
            int cost,
            int amount)
        {
            return CardDefinition.CreateRuntime(
                id,
                displayName,
                description,
                cost,
                CardRarity.Common,
                CardType.Attack,
                new CardEffectData(CardEffectType.Damage, CardTarget.Opponent, amount));
        }

        private static CardDefinition CreateBlockCard(
            string id,
            string displayName,
            string description,
            int cost,
            int amount)
        {
            return CardDefinition.CreateRuntime(
                id,
                displayName,
                description,
                cost,
                CardRarity.Common,
                CardType.Skill,
                new CardEffectData(CardEffectType.Block, CardTarget.Self, amount));
        }

        private static CardDefinition CreateHealCard(
            string id,
            string displayName,
            string description,
            int cost,
            int amount)
        {
            return CardDefinition.CreateRuntime(
                id,
                displayName,
                description,
                cost,
                CardRarity.Common,
                CardType.Skill,
                new CardEffectData(CardEffectType.Heal, CardTarget.Self, amount));
        }

        private static void AddCopies(
            ICollection<CardDefinition> cards,
            CardDefinition definition,
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                cards.Add(definition);
            }
        }
    }
}
