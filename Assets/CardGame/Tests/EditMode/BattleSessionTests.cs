using CardGame.Battle;
using CardGame.Cards;
using CardGame.Core;
using NUnit.Framework;

namespace CardGame.Tests
{
    public sealed class BattleSessionTests
    {
        [Test]
        public void StateMachine_BroadcastsValidTransition()
        {
            StateMachine<BattlePhase> stateMachine = new StateMachine<BattlePhase>(BattlePhase.NotStarted);
            BattlePhase observed = BattlePhase.NotStarted;
            stateMachine.StateChanged += (_, next) => observed = next;

            bool changed = stateMachine.TryTransition(BattlePhase.PlayerTurn);

            Assert.IsTrue(changed);
            Assert.AreEqual(BattlePhase.PlayerTurn, stateMachine.Current);
            Assert.AreEqual(BattlePhase.PlayerTurn, observed);
        }

        [Test]
        public void PlayingDamageCard_ConsumesEnergyAndDamagesOpponent()
        {
            CardDefinition strike = CreateDamageCard("test-strike", 1, 7);
            BattleSession session = new BattleSession(
                new[] { strike },
                new[] { strike },
                randomSeed: 1);
            session.Start();

            RuntimeCard card = session.GetHand(BattleSide.Player)[0];
            int healthBefore = session.Enemy.Health;
            int energyBefore = session.Player.Energy;

            Assert.IsTrue(session.TryPlayCard(card));

            Assert.AreEqual(energyBefore - 1, session.Player.Energy);
            Assert.AreEqual(healthBefore - 7, session.Enemy.Health);
            Assert.AreEqual(1, session.Player.DiscardPile.Count);
            Assert.AreEqual(0, session.Player.Hand.Count);
        }

        [Test]
        public void Block_AbsorbsDamageAndResetsOnOwnersNextTurn()
        {
            CardDefinition strike = CreateDamageCard("test-strike", 0, 4);
            CardDefinition guard = CreateBlockCard("test-guard", 0, 6);
            BattleSession session = new BattleSession(
                new[] { strike },
                new[] { guard },
                randomSeed: 2);
            session.Start();

            session.EndTurn();
            RuntimeCard enemyGuard = session.GetHand(BattleSide.Enemy)[0];
            Assert.IsTrue(session.TryPlayCard(enemyGuard));
            Assert.AreEqual(6, session.Enemy.Block);

            session.EndTurn();
            RuntimeCard playerStrike = session.GetHand(BattleSide.Player)[0];
            int enemyHealthBefore = session.Enemy.Health;
            Assert.IsTrue(session.TryPlayCard(playerStrike));

            Assert.AreEqual(enemyHealthBefore, session.Enemy.Health);
            Assert.AreEqual(2, session.Enemy.Block);

            session.EndTurn();
            Assert.AreEqual(0, session.Enemy.Block);
        }

        [Test]
        public void LethalDamage_EndsBattleWithVictory()
        {
            CardDefinition lethal = CreateDamageCard("test-lethal", 0, 999);
            BattleSession session = new BattleSession(
                new[] { lethal },
                new[] { lethal },
                randomSeed: 3);
            session.Start();

            bool played = session.TryPlayCard(session.GetHand(BattleSide.Player)[0]);

            Assert.IsTrue(played);
            Assert.AreEqual(0, session.Enemy.Health);
            Assert.AreEqual(BattleOutcome.Victory, session.Outcome);
            Assert.AreEqual(BattlePhase.Victory, session.Phase);
        }

        private static CardDefinition CreateDamageCard(string id, int cost, int damage)
        {
            return CardDefinition.CreateRuntime(
                id,
                id,
                string.Empty,
                cost,
                CardRarity.Common,
                CardType.Attack,
                new CardEffectData(CardEffectType.Damage, CardTarget.Opponent, damage));
        }

        private static CardDefinition CreateBlockCard(string id, int cost, int block)
        {
            return CardDefinition.CreateRuntime(
                id,
                id,
                string.Empty,
                cost,
                CardRarity.Common,
                CardType.Skill,
                new CardEffectData(CardEffectType.Block, CardTarget.Self, block));
        }
    }
}
