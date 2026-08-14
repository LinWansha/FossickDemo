using System;
using System.Collections.Generic;
using Fossick.Core.Definition.Config;
using Fossick.Core.Visual;
using UnityEngine;

namespace Fossick.Runtime.Views
{
    public sealed partial class FossickBoardView
    {
        private sealed class TerrainAttachmentPool
        {
            public readonly List<RectTransform> views = new List<RectTransform>();
            public int used;
        }

        private readonly Dictionary<string, TerrainAttachmentPool> terrainAttachmentPrefabPools =
            new Dictionary<string, TerrainAttachmentPool>();

        private Func<string, Transform, RectTransform> terrainAttachmentFactory;

        public void SetTerrainAttachmentFactory(Func<string, Transform, RectTransform> factory)
        {
            terrainAttachmentFactory = factory;
        }

        private void RenderTerrainAttachmentPrefab(string rewardId, float left, float top, float cellSize)
        {
            if (!terrainAttachmentPrefabPools.TryGetValue(rewardId, out var pool))
            {
                pool = new TerrainAttachmentPool();
                terrainAttachmentPrefabPools.Add(rewardId, pool);
            }

            var index = pool.used;
            while (pool.views.Count <= index)
            {
                pool.views.Add(terrainAttachmentFactory(rewardId, attachmentRoot));
            }

            pool.used++;
            var view = pool.views[index];
            view.gameObject.SetActive(true);
            view.anchorMin = new Vector2(0f, 1f);
            view.anchorMax = new Vector2(0f, 1f);
            view.anchoredPosition = new Vector2(
                Mathf.Round(left + cellSize * 0.5f),
                -Mathf.Round(top + cellSize * 0.5f));
        }

        private void DisableUnusedTerrainAttachmentPrefabs()
        {
            foreach (var pool in terrainAttachmentPrefabPools.Values)
            {
                for (var i = pool.used; i < pool.views.Count; i++)
                {
                    pool.views[i].gameObject.SetActive(false);
                }
            }
        }

        private static bool IsOreTerrainAttachment(FossickCellRenderData cell)
        {
            return cell != null && cell.HasTerrainAttachedReward &&
                   cell.embeddedPayload.ElementType == FossickElementType.Ore;
        }
    }
}
