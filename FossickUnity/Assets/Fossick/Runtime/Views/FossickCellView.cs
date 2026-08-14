using System;
using Fossick.Core.Definition.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fossick.Runtime.Views
{
    public sealed class FossickCellView : MonoBehaviour
    {
        [SerializeField] private Image effectHighlight;
        [SerializeField] private Text label;

        private int x;
        private int y;
        private bool effectHighlighted;
        private Action<int, int> clicked;
        private Action<int, int> pointerDown;
        private Action pointerUp;

        public RectTransform RectTransform => (RectTransform)transform;

        public void SetEffectHighlighted(bool highlighted, Color color)
        {
            effectHighlighted = highlighted;
            color.a = highlighted ? color.a : 0f;
            effectHighlight.color = color;
        }

        public void SetEffectHighlightAlpha(float alpha)
        {
            if (!effectHighlighted)
            {
                return;
            }

            var color = effectHighlight.color;
            color.a = alpha;
            effectHighlight.color = color;
        }

        public void Bind(
            FossickCellRenderData cell,
            int viewX,
            int viewY,
            Font font,
            bool showDebugLabel,
            Action<int, int> onClick,
            Action<int, int> onPointerDown,
            Action onPointerUp)
        {
            EnsureLayers(font);
            x = viewX;
            y = viewY;
            clicked = onClick;
            pointerDown = onPointerDown;
            pointerUp = onPointerUp;

            BindLabel(cell, font, showDebugLabel);
        }

        private void EnsureLayers(Font font)
        {
            if (effectHighlight == null)
            {
                var highlightObject = new GameObject("Effect Highlight", typeof(RectTransform), typeof(Image));
                highlightObject.transform.SetParent(transform, false);
                highlightObject.transform.SetAsFirstSibling();
                Stretch((RectTransform)highlightObject.transform);
                effectHighlight = highlightObject.GetComponent<Image>();
                effectHighlight.color = Color.clear;
                effectHighlight.raycastTarget = false;
            }

            if (label == null)
            {
                var labelObject = new GameObject("Debug Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(transform, false);
                Stretch((RectTransform)labelObject.transform);
                label = labelObject.GetComponent<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
            }

            if (font != null)
            {
                label.font = font;
            }

            if (GetComponent<EventTrigger>() == null)
            {
                var trigger = gameObject.AddComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerDown, _ => pointerDown?.Invoke(x, y));
                AddEvent(trigger, EventTriggerType.PointerClick, _ => clicked?.Invoke(x, y));
                AddEvent(trigger, EventTriggerType.PointerUp, _ => pointerUp?.Invoke());
            }
        }

        private void BindLabel(FossickCellRenderData cell, Font font, bool showDebugLabel)
        {
            label.gameObject.SetActive(showDebugLabel);
            if (!showDebugLabel)
            {
                return;
            }

            if (font != null)
            {
                label.font = font;
            }

            label.text = GetCellLabel(cell);
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.92f, 0.94f, 0.95f);
        }

        private static string GetCellLabel(FossickCellRenderData cell)
        {
            if (cell == null || !cell.isContentVisible)
            {
                return "?";
            }

            switch (cell.terrain)
            {
                case FossickTerrainType.Dirt:
                    return "土";
                case FossickTerrainType.Stone:
                    return "石" + cell.hp;
                case FossickTerrainType.Unbreakable:
                    return "X";
                case FossickTerrainType.Explosives:
                    return "爆";
                default:
                    return cell.HasCollectableReward ? GetRewardLabel(cell.pickupPayload.ElementType) : string.Empty;
            }
        }

        private static string GetRewardLabel(FossickElementType type)
        {
            switch (type)
            {
                case FossickElementType.Coin:
                    return "G";
                case FossickElementType.Ore:
                    return "O";
                case FossickElementType.Item:
                    return "T";
                case FossickElementType.Chest:
                    return "C";
                case FossickElementType.Collection:
                    return "P";
                default:
                    return string.Empty;
            }
        }

        private static void AddEvent(EventTrigger trigger, EventTriggerType eventType, Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
