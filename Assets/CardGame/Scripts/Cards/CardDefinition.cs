using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardGame.Cards
{
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare
    }

    public enum CardType
    {
        Attack,
        Skill,
        Power
    }

    public enum CardTarget
    {
        None,
        Self,
        Opponent
    }

    public enum CardEffectType
    {
        Damage,
        Block,
        Heal,
        DrawCards,
        GainEnergy,
        Poison
    }

    /// <summary>
    /// 单条卡牌效果数据。框架通过“效果列表”而不是为每张牌写一段代码来组合卡牌。
    /// 后续添加有目标选择、多段攻击、状态附加等功能时，只需要扩展这里的参数。
    /// </summary>
    [Serializable]
    public sealed class CardEffectData
    {
        [SerializeField] private CardEffectType type;
        [SerializeField] private CardTarget target;
        [SerializeField] private int amount;

        public CardEffectType Type => type;
        public CardTarget Target => target;
        public int Amount => amount;

        public CardEffectData(CardEffectType type, CardTarget target, int amount)
        {
            this.type = type;
            this.target = target;
            this.amount = Mathf.Max(0, amount);
        }
    }

    /// <summary>
    /// 卡牌静态配置。正式项目中可以在 Unity 中通过 Create/Card Game/Card 创建资产。
    /// 战斗运行时不会直接修改这些定义，而是创建 RuntimeCard 实例参与战斗。
    /// </summary>
    [CreateAssetMenu(menuName = "Card Game/Card", fileName = "Card")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private string id = "new-card";
        [SerializeField] private string displayName = "新卡牌";
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField, Min(0)] private int cost = 1;
        [SerializeField] private CardRarity rarity = CardRarity.Common;
        [SerializeField] private CardType cardType = CardType.Attack;
        [SerializeField] private List<CardEffectData> effects = new List<CardEffectData>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => string.IsNullOrWhiteSpace(description) ? BuildDescription() : description;
        public int Cost => cost;
        public CardRarity Rarity => rarity;
        public CardType Type => cardType;
        public IReadOnlyList<CardEffectData> Effects => effects;

        public static CardDefinition CreateRuntime(
            string id,
            string displayName,
            string description,
            int cost,
            CardRarity rarity,
            CardType cardType,
            params CardEffectData[] effects)
        {
            CardDefinition definition = CreateInstance<CardDefinition>();
            definition.name = id;
            definition.id = id;
            definition.displayName = displayName;
            definition.description = description;
            definition.cost = Mathf.Max(0, cost);
            definition.rarity = rarity;
            definition.cardType = cardType;
            definition.effects = new List<CardEffectData>(effects ?? Array.Empty<CardEffectData>());
            return definition;
        }

        private string BuildDescription()
        {
            if (effects.Count == 0)
            {
                return "无效果";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < effects.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                CardEffectData effect = effects[i];
                switch (effect.Type)
                {
                    case CardEffectType.Damage:
                        builder.Append("造成 ").Append(effect.Amount).Append(" 点伤害。");
                        break;
                    case CardEffectType.Block:
                        builder.Append("获得 ").Append(effect.Amount).Append(" 点格挡。");
                        break;
                    case CardEffectType.Heal:
                        builder.Append("恢复 ").Append(effect.Amount).Append(" 点生命。");
                        break;
                    case CardEffectType.DrawCards:
                        builder.Append("抽 ").Append(effect.Amount).Append(" 张牌。");
                        break;
                    case CardEffectType.GainEnergy:
                        builder.Append("获得 ").Append(effect.Amount).Append(" 点能量。");
                        break;
                    case CardEffectType.Poison:
                        builder.Append("施加 ").Append(effect.Amount).Append(" 层中毒。");
                        break;
                    default:
                        builder.Append(effect.Type);
                        break;
                }
            }

            return builder.ToString();
        }

        private void OnValidate()
        {
            id = string.IsNullOrWhiteSpace(id) ? name : id.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
            cost = Mathf.Max(0, cost);
        }
    }
}
