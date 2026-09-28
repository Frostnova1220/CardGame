using System;
using System.Collections.Generic;
using CardGame.Cards;
using CardGame.Core;

namespace CardGame.Battle
{
    /// <summary>
    /// 纯 C# 战斗核心。它不引用 UI 或 MonoBehaviour，因此可以在 EditMode 测试、
    /// 服务器模拟器或不同表现层中复用。
    /// </summary>
    public sealed class BattleSession
    {
        private const int MaxHandSize = 8;
        private const int StartingHandSize = 4;

        private readonly Random random;
        private readonly StateMachine<BattlePhase> stateMachine;
        private readonly BattleActorState player;
        private readonly BattleActorState enemy;
        private readonly List<BattleLogEntry> logs = new List<BattleLogEntry>();
        private readonly List<RuntimeCard> playerMasterDeck = new List<RuntimeCard>();
        private readonly List<RuntimeCard> enemyMasterDeck = new List<RuntimeCard>();
        private int turnNumber;
        private BattleOutcome outcome;

        public event Action StateChanged;
        public event Action<BattleLogEntry> LogAdded;
        public event Action<BattleOutcome> BattleEnded;

        public BattlePhase Phase => stateMachine.Current;
        public BattleOutcome Outcome => outcome;
        public int TurnNumber => turnNumber;
        public BattleActorState Player => player;
        public BattleActorState Enemy => enemy;
        public IReadOnlyList<BattleLogEntry> Logs => logs;
        public BattleSide CurrentSide => Phase == BattlePhase.EnemyTurn ? BattleSide.Enemy : BattleSide.Player;

        public BattleSession(
            IReadOnlyList<CardDefinition> playerDeck,
            IReadOnlyList<CardDefinition> enemyDeck,
            int randomSeed = 20260928)
        {
            if (playerDeck == null || playerDeck.Count == 0)
            {
                throw new ArgumentException("Player deck cannot be empty.", nameof(playerDeck));
            }

            if (enemyDeck == null || enemyDeck.Count == 0)
            {
                throw new ArgumentException("Enemy deck cannot be empty.", nameof(enemyDeck));
            }

            random = new Random(randomSeed);
            stateMachine = new StateMachine<BattlePhase>(BattlePhase.NotStarted);
            player = new BattleActorState("player", "冒险者", 50, 3);
            enemy = new BattleActorState("enemy", "遗迹守卫", 50, 3);

            BuildDeck(playerMasterDeck, player.Id, playerDeck);
            BuildDeck(enemyMasterDeck, enemy.Id, enemyDeck);
            Shuffle(playerMasterDeck);
            Shuffle(enemyMasterDeck);
        }

        public static BattleSession Create(
            DeckDefinition playerDeck,
            DeckDefinition enemyDeck,
            int randomSeed = 20260928)
        {
            if (playerDeck == null)
            {
                throw new ArgumentNullException(nameof(playerDeck));
            }

            if (enemyDeck == null)
            {
                throw new ArgumentNullException(nameof(enemyDeck));
            }

            return new BattleSession(playerDeck.Cards, enemyDeck.Cards, randomSeed);
        }

        public void Start()
        {
            if (Phase != BattlePhase.NotStarted)
            {
                return;
            }

            player.MutableDrawPile.AddRange(playerMasterDeck);
            enemy.MutableDrawPile.AddRange(enemyMasterDeck);

            AddLog("战斗开始。击败遗迹守卫即可获胜。", BattleLogTone.Good);
            DrawCards(player, StartingHandSize, false);
            BeginTurn(BattleSide.Player, false);
        }

        /// <summary>
        /// 尝试为当前行动方打出手牌。返回 false 时不会消耗能量或修改手牌。
        /// </summary>
        public bool TryPlayCard(RuntimeCard card)
        {
            if (!CanPlayCard(card, out string reason))
            {
                AddLog(reason, BattleLogTone.Warning);
                return false;
            }

            BattleActorState actor = GetActor(CurrentSide);
            BattleActorState opponent = GetOpponent(CurrentSide);
            actor.Energy -= card.Definition.Cost;
            actor.MutableHand.Remove(card);

            AddLog(
                $"{actor.DisplayName}打出「{card.Definition.DisplayName}」。",
                CurrentSide == BattleSide.Player ? BattleLogTone.Player : BattleLogTone.Enemy);

            IReadOnlyList<CardEffectData> effects = card.Definition.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                ResolveEffect(actor, opponent, effects[i]);
                if (outcome != BattleOutcome.None)
                {
                    break;
                }
            }

            actor.MutableDiscardPile.Add(card);
            RaiseStateChanged();
            return true;
        }

        public bool CanPlayCard(RuntimeCard card, out string reason)
        {
            if (card == null)
            {
                reason = "没有选择卡牌。";
                return false;
            }

            if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat)
            {
                reason = "战斗已经结束。";
                return false;
            }

            if (Phase == BattlePhase.NotStarted)
            {
                reason = "战斗尚未开始。";
                return false;
            }

            BattleSide side = CurrentSide;
            BattleActorState actor = GetActor(side);
            if (card.OwnerId != actor.Id || !actor.MutableHand.Contains(card))
            {
                reason = "这张牌不在当前行动方的手牌中。";
                return false;
            }

            if (actor.Energy < card.Definition.Cost)
            {
                reason = "能量不足。";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool EndTurn()
        {
            if (Phase == BattlePhase.PlayerTurn)
            {
                BeginTurn(BattleSide.Enemy, true);
                return true;
            }

            if (Phase == BattlePhase.EnemyTurn)
            {
                BeginTurn(BattleSide.Player, true);
                return true;
            }

            return false;
        }

        public IReadOnlyList<RuntimeCard> GetHand(BattleSide side)
        {
            return GetActor(side).Hand;
        }

        public List<RuntimeCard> GetPlayableCards(BattleSide side)
        {
            List<RuntimeCard> result = new List<RuntimeCard>();
            if (side != CurrentSide || Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat)
            {
                return result;
            }

            BattleActorState actor = GetActor(side);
            for (int i = 0; i < actor.Hand.Count; i++)
            {
                RuntimeCard card = actor.Hand[i];
                if (actor.Energy >= card.Definition.Cost)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        public BattleActorState GetActor(BattleSide side)
        {
            return side == BattleSide.Player ? player : enemy;
        }

        public BattleActorState GetOpponent(BattleSide side)
        {
            return side == BattleSide.Player ? enemy : player;
        }

        private void BeginTurn(BattleSide side, bool drawAtStart)
        {
            BattleActorState actor = GetActor(side);
            BattlePhase phase = side == BattleSide.Player ? BattlePhase.PlayerTurn : BattlePhase.EnemyTurn;
            actor.PrepareForTurn();
            stateMachine.TryTransition(phase);

            if (side == BattleSide.Player)
            {
                turnNumber++;
                AddLog($"第 {turnNumber} 回合：你的回合。", BattleLogTone.Player);
            }
            else
            {
                AddLog("敌方回合。", BattleLogTone.Enemy);
            }

            if (actor.Poison > 0)
            {
                int poisonDamage = actor.Poison;
                actor.Poison--;
                AddLog($"{actor.DisplayName}受到 {poisonDamage} 点中毒伤害。", BattleLogTone.Warning);
                ApplyDamage(actor, poisonDamage, true);
            }

            if (outcome == BattleOutcome.None && drawAtStart)
            {
                DrawCards(actor, 1, true);
            }

            RaiseStateChanged();
        }

        private void ResolveEffect(
            BattleActorState actor,
            BattleActorState opponent,
            CardEffectData effect)
        {
            BattleActorState target = effect.Target == CardTarget.Opponent ? opponent : actor;

            switch (effect.Type)
            {
                case CardEffectType.Damage:
                    ApplyDamage(target, effect.Amount, false);
                    break;

                case CardEffectType.Block:
                    target.AddBlock(effect.Amount);
                    AddLog($"{target.DisplayName}获得 {effect.Amount} 点格挡。", ToneFor(target));
                    break;

                case CardEffectType.Heal:
                    int beforeHeal = target.Health;
                    target.ChangeHealth(effect.Amount);
                    int healed = target.Health - beforeHeal;
                    AddLog($"{target.DisplayName}恢复 {healed} 点生命。", BattleLogTone.Good);
                    break;

                case CardEffectType.DrawCards:
                    DrawCards(target, effect.Amount, true);
                    break;

                case CardEffectType.GainEnergy:
                    target.Energy += effect.Amount;
                    AddLog($"{target.DisplayName}获得 {effect.Amount} 点能量。", ToneFor(target));
                    break;

                case CardEffectType.Poison:
                    target.AddPoison(effect.Amount);
                    AddLog($"{target.DisplayName}获得 {effect.Amount} 层中毒。", ToneFor(target));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ApplyDamage(BattleActorState target, int amount, bool ignoreBlock)
        {
            if (amount <= 0 || outcome != BattleOutcome.None)
            {
                return;
            }

            int blocked = ignoreBlock ? 0 : Math.Min(target.Block, amount);
            if (blocked > 0)
            {
                target.Block -= blocked;
            }

            int healthDamage = amount - blocked;
            if (healthDamage > 0)
            {
                target.ChangeHealth(-healthDamage);
            }

            if (blocked > 0 && healthDamage > 0)
            {
                AddLog(
                    $"{target.DisplayName}的格挡抵消 {blocked} 点，并受到 {healthDamage} 点伤害。",
                    ToneFor(target));
            }
            else if (blocked > 0)
            {
                AddLog($"{target.DisplayName}的格挡抵消了全部 {blocked} 点伤害。", ToneFor(target));
            }
            else
            {
                AddLog($"{target.DisplayName}受到 {healthDamage} 点伤害。", ToneFor(target));
            }

            TryFinishBattle();
        }

        private void DrawCards(BattleActorState actor, int count, bool writeLog)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (actor.MutableHand.Count >= MaxHandSize)
                {
                    AddLog("手牌已满，无法继续抽牌。", BattleLogTone.Warning);
                    break;
                }

                if (!DrawOne(actor))
                {
                    break;
                }

                drawn++;
            }

            if (writeLog && drawn > 0)
            {
                AddLog($"{actor.DisplayName}抽了 {drawn} 张牌。", ToneFor(actor));
            }
        }

        private bool DrawOne(BattleActorState actor)
        {
            if (actor.MutableDrawPile.Count == 0)
            {
                if (actor.MutableDiscardPile.Count == 0)
                {
                    return false;
                }

                actor.MutableDrawPile.AddRange(actor.MutableDiscardPile);
                actor.MutableDiscardPile.Clear();
                Shuffle(actor.MutableDrawPile);
                AddLog($"{actor.DisplayName}洗回弃牌堆。", ToneFor(actor));
            }

            int lastIndex = actor.MutableDrawPile.Count - 1;
            RuntimeCard card = actor.MutableDrawPile[lastIndex];
            actor.MutableDrawPile.RemoveAt(lastIndex);
            actor.MutableHand.Add(card);
            return true;
        }

        private void TryFinishBattle()
        {
            if (outcome != BattleOutcome.None)
            {
                return;
            }

            if (player.IsDead && enemy.IsDead)
            {
                FinishBattle(BattleOutcome.Defeat, "双方同归于尽，挑战失败。");
                return;
            }

            if (enemy.IsDead)
            {
                FinishBattle(BattleOutcome.Victory, "遗迹守卫已被击败，胜利！");
                return;
            }

            if (player.IsDead)
            {
                FinishBattle(BattleOutcome.Defeat, "冒险者倒下了，挑战失败。");
            }
        }

        private void FinishBattle(BattleOutcome result, string message)
        {
            outcome = result;
            stateMachine.TryTransition(
                result == BattleOutcome.Victory ? BattlePhase.Victory : BattlePhase.Defeat);
            AddLog(message, result == BattleOutcome.Victory ? BattleLogTone.Good : BattleLogTone.Warning);
            BattleEnded?.Invoke(result);
        }

        private BattleLogTone ToneFor(BattleActorState actor)
        {
            return actor == player ? BattleLogTone.Player : BattleLogTone.Enemy;
        }

        private void BuildDeck(
            ICollection<RuntimeCard> target,
            string ownerId,
            IReadOnlyList<CardDefinition> definitions)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                CardDefinition definition = definitions[i];
                if (definition != null)
                {
                    target.Add(new RuntimeCard(ownerId, definition));
                }
            }
        }

        private void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                T temp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = temp;
            }
        }

        private void AddLog(string message, BattleLogTone tone)
        {
            BattleLogEntry entry = new BattleLogEntry(message, tone);
            logs.Add(entry);
            LogAdded?.Invoke(entry);
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
