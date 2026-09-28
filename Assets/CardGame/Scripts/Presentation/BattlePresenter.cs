using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CardGame.Battle;
using CardGame.Cards;
using CardGame.Persistence;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CardGame.Presentation
{
    /// <summary>
    /// 表现层控制器：把按钮输入转成战斗意图，再把战斗状态投影到 UI。
    /// 它不决定伤害、抽牌或胜负，这些规则全部位于 BattleSession。
    /// </summary>
    public sealed class BattlePresenter : MonoBehaviour
    {
        private BattleSession session;
        private IPlayerProfileStore profileStore;
        private PlayerProfile profile;
        private Coroutine enemyTurnRoutine;
        private bool resultRecorded;

        private RectTransform handRoot;
        private Text enemyStatsText;
        private Text playerStatsText;
        private Text turnText;
        private Text logText;
        private Text deckText;
        private Text profileText;
        private Button endTurnButton;
        private GameObject resultOverlay;
        private Text resultTitleText;
        private Text resultDetailText;

        private void Awake()
        {
            profileStore = new JsonPlayerProfileStore();
            profile = profileStore.Load();
            BuildUi();
            EnsureEventSystem();
        }

        private void Start()
        {
            StartNewBattle();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSession();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "BattleCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image background = UiFactory.CreatePanel(
                "Background",
                canvasObject.transform,
                new Color(0.045f, 0.055f, 0.08f, 1f),
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.Stretch(background.rectTransform);

            Image enemyPanel = UiFactory.CreatePanel(
                "EnemyPanel",
                canvasObject.transform,
                new Color(0.13f, 0.08f, 0.10f, 0.98f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(1400f, 132f));

            UiFactory.CreateText(
                "EnemyTitle",
                enemyPanel.transform,
                "遗迹守卫",
                30,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.76f, 0.76f, 1f),
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(28f, 0f),
                new Vector2(250f, 0f));

            enemyStatsText = UiFactory.CreateText(
                "EnemyStats",
                enemyPanel.transform,
                string.Empty,
                24,
                TextAnchor.MiddleLeft,
                Color.white,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(280f, 0f),
                new Vector2(-620f, -20f));

            profileText = UiFactory.CreateText(
                "Profile",
                enemyPanel.transform,
                string.Empty,
                19,
                TextAnchor.MiddleRight,
                new Color(0.82f, 0.86f, 0.95f, 1f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0.5f),
                new Vector2(-24f, 0f),
                new Vector2(310f, -20f));

            Image logPanel = UiFactory.CreatePanel(
                "BattleLogPanel",
                canvasObject.transform,
                new Color(0.075f, 0.09f, 0.13f, 0.98f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f),
                new Vector2(1240f, 440f));

            Text logTitle = UiFactory.CreateText(
                "LogTitle",
                logPanel.transform,
                "战斗记录",
                25,
                TextAnchor.MiddleLeft,
                new Color(0.74f, 0.82f, 1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -16f),
                new Vector2(-48f, 44f));
            logTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            logTitle.rectTransform.anchorMax = new Vector2(1f, 1f);

            turnText = UiFactory.CreateText(
                "TurnText",
                logPanel.transform,
                string.Empty,
                23,
                TextAnchor.MiddleRight,
                new Color(1f, 0.84f, 0.42f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -16f),
                new Vector2(420f, 44f));

            logText = UiFactory.CreateText(
                "LogText",
                logPanel.transform,
                string.Empty,
                20,
                TextAnchor.LowerLeft,
                new Color(0.9f, 0.92f, 0.96f, 1f),
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f),
                new Vector2(-56f, -82f));

            Image playerPanel = UiFactory.CreatePanel(
                "PlayerPanel",
                canvasObject.transform,
                new Color(0.075f, 0.15f, 0.19f, 0.98f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 332f),
                new Vector2(1240f, 116f));

            UiFactory.CreateText(
                "PlayerTitle",
                playerPanel.transform,
                "冒险者",
                29,
                TextAnchor.MiddleLeft,
                new Color(0.68f, 0.95f, 1f, 1f),
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(28f, 0f),
                new Vector2(220f, 0f));

            playerStatsText = UiFactory.CreateText(
                "PlayerStats",
                playerPanel.transform,
                string.Empty,
                23,
                TextAnchor.MiddleLeft,
                Color.white,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(250f, 0f),
                new Vector2(-500f, -16f));

            deckText = UiFactory.CreateText(
                "DeckText",
                playerPanel.transform,
                string.Empty,
                19,
                TextAnchor.MiddleRight,
                new Color(0.82f, 0.86f, 0.95f, 1f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0.5f),
                new Vector2(-24f, 0f),
                new Vector2(320f, -16f));

            handRoot = UiFactory.CreateRect(
                "Hand",
                canvasObject.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(1640f, 290f));
            HorizontalLayoutGroup layout = handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            endTurnButton = UiFactory.CreateButton(
                "EndTurnButton",
                canvasObject.transform,
                "结束回合",
                new Color(0.86f, 0.48f, 0.18f, 1f),
                Color.white,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-44f, 338f),
                new Vector2(230f, 76f),
                out _);
            endTurnButton.onClick.AddListener(HandleEndTurnClicked);

            Image overlayImage = UiFactory.CreatePanel(
                "ResultOverlay",
                canvasObject.transform,
                new Color(0.015f, 0.02f, 0.03f, 0.94f),
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.Stretch(overlayImage.rectTransform);
            resultOverlay = overlayImage.gameObject;

            resultTitleText = UiFactory.CreateText(
                "ResultTitle",
                resultOverlay.transform,
                string.Empty,
                64,
                TextAnchor.MiddleCenter,
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 130f),
                new Vector2(900f, 100f));

            resultDetailText = UiFactory.CreateText(
                "ResultDetail",
                resultOverlay.transform,
                string.Empty,
                25,
                TextAnchor.MiddleCenter,
                new Color(0.85f, 0.89f, 0.95f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f),
                new Vector2(900f, 100f));

            Button restartButton = UiFactory.CreateButton(
                "RestartButton",
                resultOverlay.transform,
                "再来一局",
                new Color(0.18f, 0.55f, 0.78f, 1f),
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -100f),
                new Vector2(280f, 76f),
                out _);
            restartButton.onClick.AddListener(StartNewBattle);
            resultOverlay.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);
            InputSystemUIInputModule inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private void StartNewBattle()
        {
            if (enemyTurnRoutine != null)
            {
                StopCoroutine(enemyTurnRoutine);
                enemyTurnRoutine = null;
            }

            UnsubscribeFromSession();
            resultRecorded = false;
            resultOverlay.SetActive(false);

            session = BattleSession.Create(
                DemoContentFactory.CreatePlayerDeck(),
                DemoContentFactory.CreateEnemyDeck());

            session.StateChanged += OnStateChanged;
            session.LogAdded += OnLogAdded;
            session.BattleEnded += OnBattleEnded;
            session.Start();
        }

        private void HandleEndTurnClicked()
        {
            if (session != null && session.Phase == BattlePhase.PlayerTurn)
            {
                session.EndTurn();
            }
        }

        private void HandleCardClicked(RuntimeCard card)
        {
            if (session != null)
            {
                session.TryPlayCard(card);
            }
        }

        private void OnStateChanged()
        {
            Refresh();

            if (session != null &&
                session.Phase == BattlePhase.EnemyTurn &&
                enemyTurnRoutine == null)
            {
                enemyTurnRoutine = StartCoroutine(EnemyTurnRoutine());
            }
        }

        private void OnLogAdded(BattleLogEntry entry)
        {
            RefreshLog();
        }

        private IEnumerator EnemyTurnRoutine()
        {
            yield return new WaitForSeconds(0.55f);

            int safetyCounter = 0;
            while (session != null &&
                   session.Phase == BattlePhase.EnemyTurn &&
                   safetyCounter < 30)
            {
                safetyCounter++;
                RuntimeCard card = SimpleBattleAi.ChooseCard(session, BattleSide.Enemy);
                if (card == null)
                {
                    break;
                }

                session.TryPlayCard(card);
                yield return new WaitForSeconds(0.62f);
            }

            if (session != null && session.Phase == BattlePhase.EnemyTurn)
            {
                session.EndTurn();
            }

            enemyTurnRoutine = null;
        }

        private void OnBattleEnded(BattleOutcome outcome)
        {
            if (!resultRecorded)
            {
                RecordResult(outcome);
                resultRecorded = true;
            }

            resultOverlay.SetActive(true);
            bool victory = outcome == BattleOutcome.Victory;
            resultTitleText.text = victory ? "胜利" : "挑战失败";
            resultTitleText.color = victory
                ? new Color(1f, 0.84f, 0.36f, 1f)
                : new Color(1f, 0.52f, 0.52f, 1f);
            resultDetailText.text = victory
                ? "你击败了遗迹守卫。\n继续扩展卡牌、敌人和战斗规则吧。"
                : "冒险者倒下了。\n调整出牌顺序后再次挑战。";
        }

        private void UnsubscribeFromSession()
        {
            if (session == null)
            {
                return;
            }

            session.StateChanged -= OnStateChanged;
            session.LogAdded -= OnLogAdded;
            session.BattleEnded -= OnBattleEnded;
        }

        private void Refresh()
        {
            if (session == null)
            {
                return;
            }

            enemyStatsText.text = FormatActorStats(session.Enemy);
            playerStatsText.text = FormatActorStats(session.Player);
            turnText.text = GetTurnText(session);
            deckText.text =
                $"抽牌堆 {session.Player.DrawPile.Count}  |  手牌 {session.Player.Hand.Count}  |  弃牌堆 {session.Player.DiscardPile.Count}";
            profileText.text =
                $"战绩: {profile.victories} 胜 {profile.defeats} 负\n当前连胜: {profile.currentStreak}    最佳: {profile.bestStreak}";
            endTurnButton.interactable = session.Phase == BattlePhase.PlayerTurn;
            RebuildHand();
            RefreshLog();
        }

        private void RefreshLog()
        {
            if (session == null || logText == null)
            {
                return;
            }

            const int visibleLineCount = 10;
            int startIndex = Mathf.Max(0, session.Logs.Count - visibleLineCount);
            StringBuilder builder = new StringBuilder();
            for (int i = startIndex; i < session.Logs.Count; i++)
            {
                BattleLogEntry entry = session.Logs[i];
                builder.Append("<color=")
                    .Append(GetToneColor(entry.Tone))
                    .Append(">• ")
                    .Append(entry.Message)
                    .Append("</color>\n");
            }

            logText.text = builder.ToString();
        }

        private void RebuildHand()
        {
            for (int i = handRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(handRoot.GetChild(i).gameObject);
            }

            IReadOnlyList<RuntimeCard> hand = session.GetHand(BattleSide.Player);
            if (hand.Count == 0)
            {
                Text emptyText = UiFactory.CreateText(
                    "EmptyHand",
                    handRoot,
                    "当前没有手牌",
                    24,
                    TextAnchor.MiddleCenter,
                    new Color(0.72f, 0.76f, 0.84f, 0.8f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(500f, 80f));
                UiFactory.AddLayoutElement(emptyText.gameObject, 500f, 80f);
                return;
            }

            for (int i = 0; i < hand.Count; i++)
            {
                RuntimeCard card = hand[i];
                bool canPlay = session.Phase == BattlePhase.PlayerTurn &&
                               session.CanPlayCard(card, out _);
                CardView.Create(handRoot, card, canPlay, HandleCardClicked);
            }
        }

        private void RecordResult(BattleOutcome battleOutcome)
        {
            if (battleOutcome == BattleOutcome.Victory)
            {
                profile.victories++;
                profile.currentStreak++;
                profile.bestStreak = Mathf.Max(profile.bestStreak, profile.currentStreak);
            }
            else if (battleOutcome == BattleOutcome.Defeat)
            {
                profile.defeats++;
                profile.currentStreak = 0;
            }

            profileStore.Save(profile);
        }

        private static string FormatActorStats(BattleActorState actor)
        {
            string poison = actor.Poison > 0 ? $"    中毒 {actor.Poison}" : string.Empty;
            return $"生命 {actor.Health}/{actor.MaxHealth}    格挡 {actor.Block}    能量 {actor.Energy}/{actor.MaxEnergy}{poison}";
        }

        private static string GetTurnText(BattleSession battleSession)
        {
            switch (battleSession.Phase)
            {
                case BattlePhase.NotStarted:
                    return "准备中";
                case BattlePhase.PlayerTurn:
                    return $"第 {battleSession.TurnNumber} 回合 · 你的回合";
                case BattlePhase.EnemyTurn:
                    return "敌方行动中...";
                case BattlePhase.Victory:
                    return "战斗胜利";
                case BattlePhase.Defeat:
                    return "战斗失败";
                default:
                    return string.Empty;
            }
        }

        private static string GetToneColor(BattleLogTone tone)
        {
            switch (tone)
            {
                case BattleLogTone.Player:
                    return "#71D7FF";
                case BattleLogTone.Enemy:
                    return "#FF9A9A";
                case BattleLogTone.Warning:
                    return "#FFC561";
                case BattleLogTone.Good:
                    return "#8DF0A6";
                default:
                    return "#C9D1E3";
            }
        }
    }
}

