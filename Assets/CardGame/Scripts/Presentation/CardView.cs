using System;
using CardGame.Battle;
using CardGame.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Presentation
{
    /// <summary>
    /// 一张手牌的运行时视图。它只负责显示数据和上报点击，不包含任何规则判断。
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        private RuntimeCard card;
        private Action<RuntimeCard> clicked;

        public static CardView Create(
            Transform parent,
            RuntimeCard runtimeCard,
            bool interactable,
            Action<RuntimeCard> onClick)
        {
            Color cardColor = GetCardColor(runtimeCard.Definition.Type);
            Image image = UiFactory.CreatePanel(
                $"Card-{runtimeCard.Definition.DisplayName}",
                parent,
                cardColor,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(190f, 280f));

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.interactable = interactable;
            UiFactory.AddLayoutElement(image.gameObject, 190f, 280f);

            CardView view = image.gameObject.AddComponent<CardView>();
            view.Initialize(runtimeCard, button, onClick);

            RectTransform costBadge = UiFactory.CreateRect(
                "CostBadge",
                image.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(10f, -10f),
                new Vector2(46f, 46f));
            Image badgeImage = costBadge.gameObject.AddComponent<Image>();
            badgeImage.color = new Color(0.08f, 0.1f, 0.13f, 0.92f);

            Text costText = UiFactory.CreateText(
                "Cost",
                costBadge,
                runtimeCard.Definition.Cost.ToString(),
                30,
                TextAnchor.MiddleCenter,
                Color.white,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            UiFactory.Stretch(costText.rectTransform);

            UiFactory.CreateText(
                "Name",
                image.transform,
                runtimeCard.Definition.DisplayName,
                24,
                TextAnchor.MiddleCenter,
                Color.white,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(18f, -30f),
                new Vector2(-58f, 72f));

            UiFactory.CreateText(
                "Type",
                image.transform,
                GetTypeText(runtimeCard.Definition.Type),
                16,
                TextAnchor.MiddleCenter,
                new Color(0.92f, 0.92f, 0.92f, 0.86f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -108f),
                new Vector2(-24f, 30f));

            UiFactory.CreateText(
                "Description",
                image.transform,
                runtimeCard.Definition.Description,
                17,
                TextAnchor.UpperCenter,
                new Color(0.97f, 0.97f, 0.97f, 0.96f),
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(-26f, -150f));

            UiFactory.CreateText(
                "Footer",
                image.transform,
                GetRarityText(runtimeCard.Definition.Rarity),
                14,
                TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.72f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 16f),
                new Vector2(-20f, 24f));

            return view;
        }

        private void Initialize(RuntimeCard runtimeCard, Button button, Action<RuntimeCard> onClick)
        {
            card = runtimeCard;
            clicked = onClick;
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            clicked?.Invoke(card);
        }

        private static Color GetCardColor(CardType type)
        {
            switch (type)
            {
                case CardType.Attack:
                    return new Color(0.61f, 0.20f, 0.22f, 1f);
                case CardType.Skill:
                    return new Color(0.16f, 0.39f, 0.58f, 1f);
                case CardType.Power:
                    return new Color(0.42f, 0.24f, 0.58f, 1f);
                default:
                    return new Color(0.25f, 0.25f, 0.25f, 1f);
            }
        }

        private static string GetTypeText(CardType type)
        {
            switch (type)
            {
                case CardType.Attack:
                    return "攻击";
                case CardType.Skill:
                    return "技能";
                case CardType.Power:
                    return "能力";
                default:
                    return type.ToString();
            }
        }

        private static string GetRarityText(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return "普通";
                case CardRarity.Uncommon:
                    return "罕见";
                case CardRarity.Rare:
                    return "稀有";
                default:
                    return rarity.ToString();
            }
        }
    }
}
