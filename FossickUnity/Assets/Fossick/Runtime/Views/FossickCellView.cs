using System;
using Fossick.Core.Board;
using Fossick.Core.Config;
using Fossick.Core.Visual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fossick.Runtime.Views
{
    public sealed class FossickCellView : MonoBehaviour
    {
        [SerializeField] private Text label;

        private int x;
        private int y;
        private Action<int, int> clicked;
        private Action<int, int> pointerEnter;
        private Action pointerExit;
        private Action<int, int> pointerDown;
        private Action pointerUp;

        public RectTransform RectTransform => (RectTransform)transform;

        public void Bind(
            FossickCellState cell,
            int viewX,
            int viewY,
            Font font,
            bool showDebugLabel,
            bool previewed,
            Action<int, int> onClick,
            Action<int, int> onPointerEnter,
            Action onPointerExit,
            Action<int, int> onPointerDown,
            Action onPointerUp)
        {
            EnsureLayers(font);
            x = viewX;
            y = viewY;
            clicked = onClick;
            pointerEnter = onPointerEnter;
            pointerExit = onPointerExit;
            pointerDown = onPointerDown;
            pointerUp = onPointerUp;

            SetPreviewed(previewed);
            BindLabel(cell, font, showDebugLabel);
        }

        public void SetPreviewed(bool value)
        {
            EnsureLayers(null);
        }

        private void EnsureLayers(Font font)
        {
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
                AddEvent(trigger, EventTriggerType.PointerEnter, _ => pointerEnter?.Invoke(x, y));
                AddEvent(trigger, EventTriggerType.PointerExit, _ => pointerExit?.Invoke());
                AddEvent(trigger, EventTriggerType.PointerDown, _ => pointerDown?.Invoke(x, y));
                AddEvent(trigger, EventTriggerType.PointerClick, _ => clicked?.Invoke(x, y));
                AddEvent(trigger, EventTriggerType.PointerUp, _ => pointerUp?.Invoke());
            }
        }

        private void BindLabel(FossickCellState cell, Font font, bool showDebugLabel)
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

        private static string GetCellLabel(FossickCellState cell)
        {
            if (cell == null || !cell.IsContentVisible)
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
                default:
                    return cell.HasCollectableReward ? GetRewardLabel(cell.reward.type) : string.Empty;
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
