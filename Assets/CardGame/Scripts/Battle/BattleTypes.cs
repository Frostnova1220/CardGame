using System;
using CardGame.Cards;

namespace CardGame.Battle
{
    public enum BattleSide
    {
        Player,
        Enemy
    }

    public enum BattlePhase
    {
        NotStarted,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }

    public enum BattleOutcome
    {
        None,
        Victory,
        Defeat
    }

    public enum BattleLogTone
    {
        Neutral,
        Player,
        Enemy,
        Warning,
        Good
    }

    public readonly struct BattleLogEntry
    {
        public string Message { get; }
        public BattleLogTone Tone { get; }

        public BattleLogEntry(string message, BattleLogTone tone)
        {
            Message = message;
            Tone = tone;
        }
    }

    /// <summary>
    /// 战斗中的卡牌实例。同一张 CardDefinition 可以生成多个互不影响的实例。
    /// </summary>
    public sealed class RuntimeCard
    {
        public string InstanceId { get; }
        public string OwnerId { get; }
        public CardDefinition Definition { get; }

        public RuntimeCard(string ownerId, CardDefinition definition)
        {
            InstanceId = Guid.NewGuid().ToString("N");
            OwnerId = ownerId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
}
