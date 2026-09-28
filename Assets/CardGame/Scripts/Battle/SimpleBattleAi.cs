using System.Collections.Generic;
using CardGame.Cards;

namespace CardGame.Battle
{
    /// <summary>
    /// 一个可替换的简单 AI：给每张可打出的牌计算启发式分数，选择最高分的一张。
    /// 后续可以替换为行为树、效用 AI 或搜索 AI，而不改变战斗核心。
    /// </summary>
    public static class SimpleBattleAi
    {
        public static RuntimeCard ChooseCard(BattleSession session, BattleSide side)
        {
            if (session == null || session.CurrentSide != side)
            {
                return null;
            }

            List<RuntimeCard> playableCards = session.GetPlayableCards(side);
            if (playableCards.Count == 0)
            {
                return null;
            }

            BattleActorState actor = session.GetActor(side);
            BattleActorState opponent = session.GetOpponent(side);
            RuntimeCard bestCard = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < playableCards.Count; i++)
            {
                RuntimeCard card = playableCards[i];
                float score = ScoreCard(card.Definition, actor, opponent);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCard = card;
                }
            }

            return bestCard;
        }

        private static float ScoreCard(
            CardDefinition card,
            BattleActorState actor,
            BattleActorState opponent)
        {
            float score = -card.Cost * 0.25f;
            IReadOnlyList<CardEffectData> effects = card.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                CardEffectData effect = effects[i];
                switch (effect.Type)
                {
                    case CardEffectType.Damage:
                        score += effect.Amount * (opponent.Block > 0 ? 6f : 10f);
                        break;
                    case CardEffectType.Block:
                        score += effect.Amount * 6f;
                        break;
                    case CardEffectType.Heal:
                        score += System.Math.Min(effect.Amount, actor.MaxHealth - actor.Health) * 8f;
                        break;
                    case CardEffectType.DrawCards:
                        score += effect.Amount * 7f;
                        break;
                    case CardEffectType.GainEnergy:
                        score += effect.Amount * 10f;
                        break;
                    case CardEffectType.Poison:
                        score += effect.Amount * 5f;
                        break;
                }
            }

            return score;
        }
    }
}
