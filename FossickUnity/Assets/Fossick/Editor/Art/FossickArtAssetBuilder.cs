using System.Collections.Generic;
using System.IO;
using Fossick.Core.Definition.Config;
using Fossick.Core.Visual;
using Fossick.Runtime.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Fossick.Editor.Art
{
    public static class FossickArtAssetBuilder
    {
        private const float PixelsPerUnit = 140f;
        private const string ArtRoot = "Assets/Fossick/Resources/FossickArt";
        private const string CatalogPath = "Assets/Fossick/Resources/FossickArt/FossickArtCatalog.asset";
        private const string PrefabRoot = "Assets/Fossick/Runtime/Prefabs";
        private const string CellViewPrefabPath = PrefabRoot + "/FossickCellView.prefab";
        private const string BoardViewPrefabPath = PrefabRoot + "/FossickBoardView.prefab";

        [InitializeOnLoadMethod]
        private static void EnsureGeneratedAssetsOnLoad()
        {
            if (HasGeneratedAssets())
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!HasGeneratedAssets() && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    RebuildCatalogAndPrefabs();
                }
            };
        }

        [MenuItem("Fossick/Art/Rebuild Catalog And Prefabs")]
        public static void RebuildCatalogAndPrefabs()
        {
            EnsureFolder("Assets/Fossick/Runtime");
            EnsureFolder(PrefabRoot);
            NormalizeArtImportSettings();
            var catalog = BuildCatalog();
            var cellPrefab = BuildCellViewPrefab();
            BuildBoardViewPrefab(catalog, cellPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fossick art catalog and view prefabs rebuilt.");
        }

        private static bool HasGeneratedAssets()
        {
            return AssetDatabase.LoadAssetAtPath<FossickArtCatalog>(CatalogPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(CellViewPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(BoardViewPrefabPath) != null;
        }

        private static void NormalizeArtImportSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static FossickArtCatalog BuildCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FossickArtCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FossickArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            ClearCatalog(catalog);

            AddAutoTileSet(catalog, FossickTerrainType.Dirt, "Dirt", TerrainAutoTileFiles);
            AddAutoTileSet(catalog, FossickTerrainType.Stone, "Stone", StoneAutoTileFiles);
            AddAutoTileSet(catalog, FossickTerrainType.Unbreakable, "Bedrock", TerrainAutoTileFiles);
            AddTerrainSprite(catalog, FossickTerrainType.Explosives, "explosive_crate", "Layer4_Reward/Signs/explosive_crate_sign.png");
            AddFogAutoTileSet(catalog, FogAutoTileFiles);

            var mineBackground = LoadSprite("Layer0_Background/mine_default.png")
                ?? LoadSprite("Layer0_Background/mine_variant.png")
                ?? LoadSprite("Layer0_Background/mine_map.png");
            var treasureRoomBackground = LoadSprite("Layer1_RewardBackground/treasure_room_7x2.png")
                ?? LoadSprite("Layer1_RewardBackground/treasure_room_5x2.png")
                ?? LoadSprite("Layer1_RewardBackground/treasure_room_3x2.png")
                ?? mineBackground;

            AddReward(catalog, FossickElementType.Coin, "coin_pile", "Layer4_Reward/Coins/coin_stack.png", "Layer4_Reward/Coins/coin_pile_large.png", "Layer4_Reward/Coins/coin_pile_small.png");
            AddReward(catalog, FossickElementType.Ore, "ore_copper", "Layer4_Reward/Ores/ore_copper.png");
            AddReward(catalog, FossickElementType.Ore, "ore_gem", "Layer4_Reward/Ores/ore_gem.png");
            AddReward(catalog, FossickElementType.Ore, "ore_gold", "Layer4_Reward/Ores/ore_gold.png");
            AddReward(catalog, FossickElementType.Ore, "ore_silver", "Layer4_Reward/Ores/ore_silver.png");
            AddReward(catalog, FossickElementType.Item, "pickaxe", "Layer4_Reward/Tools/tool_pickaxe.png");
            AddReward(catalog, FossickElementType.Item, "dynamite", "Layer4_Reward/Tools/tool_dynamite.png");
            AddReward(catalog, FossickElementType.Item, "tnt", "Layer4_Reward/Tools/tool_tnt.png");
            AddReward(catalog, FossickElementType.Item, "radar", "Layer4_Reward/Tools/tool_radar.png");
            AddReward(catalog, FossickElementType.Chest, "treasure_chest", "Layer4_Reward/Chests/chest_closed.png", "Layer4_Reward/Chests/chest_locked.png");
            AddReward(catalog, FossickElementType.Collection, "collection_piece", "Layer4_Reward/Collections/collection_bottle.png", "Layer4_Reward/Collections/collection_bottle_shadow.png");

            AddTerrainAttachment(catalog, FossickElementType.Ore, "ore_copper", FossickTerrainType.Empty, "Layer3_TerrainAttachment/ore_copper.png");
            AddTerrainAttachment(catalog, FossickElementType.Ore, "ore_gem", FossickTerrainType.Empty, "Layer3_TerrainAttachment/ore_gem.png");
            AddTerrainAttachment(catalog, FossickElementType.Ore, "ore_gold", FossickTerrainType.Empty, "Layer3_TerrainAttachment/ore_gold.png");
            AddTerrainAttachment(catalog, FossickElementType.Ore, "ore_silver", FossickTerrainType.Empty, "Layer3_TerrainAttachment/ore_silver.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "pickaxe", FossickTerrainType.Dirt, "Layer3_TerrainAttachment/tool_pickaxe_dirt.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "pickaxe", FossickTerrainType.Stone, "Layer3_TerrainAttachment/tool_pickaxe_stone.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "dynamite", FossickTerrainType.Dirt, "Layer3_TerrainAttachment/tool_dynamite_dirt.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "dynamite", FossickTerrainType.Stone, "Layer3_TerrainAttachment/tool_dynamite_stone.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "tnt", FossickTerrainType.Dirt, "Layer3_TerrainAttachment/tool_tnt_dirt.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "tnt", FossickTerrainType.Stone, "Layer3_TerrainAttachment/tool_tnt_stone.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "radar", FossickTerrainType.Dirt, "Layer3_TerrainAttachment/tool_radar_dirt.png");
            AddTerrainAttachment(catalog, FossickElementType.Item, "radar", FossickTerrainType.Stone, "Layer3_TerrainAttachment/tool_radar_stone.png");
            AddTerrainAttachment(catalog, FossickElementType.Chest, "treasure_chest", FossickTerrainType.Dirt, "Layer3_TerrainAttachment/chest_mystery_dirt.png");

            AddNamed(catalog.layer5Decoration.decorations, "grass_large", LoadSprite("Layer5_Decoration/grass_large.png"));
            AddNamed(catalog.layer5Decoration.decorations, "grass_small", LoadSprite("Layer5_Decoration/grass_small.png"));
            AddNamed(catalog.layer5Decoration.decorations, "mushroom", LoadSprite("Layer5_Decoration/mushroom.png"));

            AddNamed(catalog.layer0Background.backgrounds, "mine_default", LoadSprite("Layer0_Background/mine_default.png"));
            AddNamed(catalog.layer0Background.backgrounds, "mine_map", LoadSprite("Layer0_Background/mine_map.png"));
            AddNamed(catalog.layer0Background.backgrounds, "mine_variant", LoadSprite("Layer0_Background/mine_variant.png"));
            AddNamed(catalog.layer1RewardBackground.backgrounds, "treasure_room_3x2", LoadSprite("Layer1_RewardBackground/treasure_room_3x2.png"));
            AddNamed(catalog.layer1RewardBackground.backgrounds, "treasure_room_5x2", LoadSprite("Layer1_RewardBackground/treasure_room_5x2.png"));
            AddNamed(catalog.layer1RewardBackground.backgrounds, "treasure_room_7x2", treasureRoomBackground);

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static FossickCellView BuildCellViewPrefab()
        {
            var root = new GameObject("FossickCellView", typeof(RectTransform), typeof(Image), typeof(FossickCellView));
            root.GetComponent<Image>().color = Color.clear;
            root.GetComponent<Image>().raycastTarget = true;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CellViewPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<FossickCellView>();
        }

        private static void ClearCatalog(FossickArtCatalog catalog)
        {
            catalog.layer0Background ??= new FossickVisualLayer0BackgroundCatalog();
            catalog.layer1RewardBackground ??= new FossickVisualLayer1RewardBackgroundCatalog();
            catalog.layer2Terrain ??= new FossickVisualLayer2TerrainCatalog();
            catalog.layer3TerrainAttachment ??= new FossickVisualLayer3TerrainAttachmentCatalog();
            catalog.layer4Reward ??= new FossickVisualLayer4RewardCatalog();
            catalog.layer5Decoration ??= new FossickVisualLayer5DecorationCatalog();
            catalog.layer6Fog ??= new FossickVisualLayer6FogCatalog();
            catalog.layer2Terrain.terrainSprites ??= new List<FossickTerrainSpriteEntry>();

            catalog.layer0Background.backgrounds.Clear();
            catalog.layer1RewardBackground.backgrounds.Clear();
            catalog.layer2Terrain.autoTileSets.Clear();
            catalog.layer2Terrain.terrainSprites.Clear();
            catalog.layer3TerrainAttachment.attachments.Clear();
            catalog.layer4Reward.rewards.Clear();
            catalog.layer5Decoration.decorations.Clear();
            catalog.layer6Fog.autoTileSet = new FossickAutoTileSet { kind = FossickAutoTileSetKind.Fog };
        }

        private static void BuildBoardViewPrefab(FossickArtCatalog catalog, FossickCellView cellPrefab)
        {
            var root = new GameObject("FossickBoardView", typeof(RectTransform), typeof(FossickBoardView));
            var boardView = root.GetComponent<FossickBoardView>();
            var serialized = new SerializedObject(boardView);
            serialized.FindProperty("artCatalog").objectReferenceValue = catalog;
            serialized.FindProperty("cellViewPrefab").objectReferenceValue = cellPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, BoardViewPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static readonly string[] TerrainAutoTileFiles =
        {
            "tile_01_tl.png",
            "tile_02_tr.png",
            "tile_03_tl_tr.png",
            "tile_04_bl.png",
            "tile_05_tl_bl.png",
            "tile_06_tr_bl.png",
            "tile_07_tl_tr_bl.png",
            "tile_08_br.png",
            "tile_09_tl_br.png",
            "tile_10_tr_br.png",
            "tile_11_tl_tr_br.png",
            "tile_12_bl_br.png",
            "tile_13_tl_bl_br.png",
            "tile_14_tr_bl_br.png",
            "tile_15_full.png",
            "single.png",
            "outer_corner_br.png",
            "outer_corner_bl.png"
        };

        private static readonly string[] StoneAutoTileFiles =
        {
            "tile_01_tl.png",
            "tile_02_tr.png",
            "tile_03_tl_tr.png",
            "tile_04_bl.png",
            "tile_05_tl_bl.png",
            "tile_06_tr_bl.png",
            "tile_07_tl_tr_bl.png",
            "tile_08_br.png",
            "tile_09_tl_br.png",
            "tile_10_tr_br.png",
            "tile_11_tl_tr_br.png",
            "tile_12_bl_br.png",
            "tile_13_tl_bl_br.png",
            "tile_14_tr_bl_br.png",
            "tile_15_full.png",
            "single.png",
            "outer_corner_br.png",
            "outer_corner_bl.png",
            "damaged_1hp.png"
        };

        private static readonly string[] FogAutoTileFiles =
        {
            "tile_01_tl.png",
            "tile_02_tr.png",
            "tile_03_tl_tr.png",
            "tile_04_bl.png",
            "tile_05_tl_bl.png",
            "tile_06_tr_bl.png",
            "tile_07_tl_tr_bl.png",
            "tile_08_br.png",
            "tile_09_tl_br.png",
            "tile_10_tr_br.png",
            "tile_11_tl_tr_br.png",
            "tile_12_bl_br.png",
            "tile_13_tl_bl_br.png",
            "tile_14_tr_bl_br.png",
            "tile_15_full.png"
        };

        private static void AddAutoTileSet(FossickArtCatalog catalog, FossickTerrainType terrain, string folder, IReadOnlyList<string> files)
        {
            var set = new FossickAutoTileSet { kind = FossickAutoTileSetKind.Terrain, terrain = terrain };
            for (var i = 0; i < files.Count; i++)
            {
                var index = i + 1;
                var path = $"{ArtRoot}/Layer2_Terrain/{folder}/{files[i]}";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    set.sprites.Add(new FossickAutoTileSpriteEntry { index = index, sprite = sprite });
                }
            }

            catalog.layer2Terrain.autoTileSets.Add(set);
        }

        private static void AddFogAutoTileSet(FossickArtCatalog catalog, IReadOnlyList<string> files)
        {
            var set = new FossickAutoTileSet { kind = FossickAutoTileSetKind.Fog };
            for (var i = 0; i < files.Count; i++)
            {
                var index = i + 1;
                var path = $"{ArtRoot}/Layer6_Fog/{files[i]}";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    set.sprites.Add(new FossickAutoTileSpriteEntry { index = index, sprite = sprite });
                }
            }

            catalog.layer6Fog.autoTileSet = set;
        }

        private static void AddReward(FossickArtCatalog catalog, FossickElementType type, string id, params string[] candidates)
        {
            var sprite = LoadFirstSprite(candidates);
            if (sprite != null)
            {
                catalog.layer4Reward.rewards.Add(new FossickElementSpriteEntry { type = type, id = id, sprite = sprite });
            }
        }

        private static void AddTerrainAttachment(FossickArtCatalog catalog, FossickElementType type, string id, FossickTerrainType terrain, string path)
        {
            var sprite = LoadSprite(path);
            if (sprite != null)
            {
                catalog.layer3TerrainAttachment.attachments.Add(new FossickTerrainAttachmentSpriteEntry
                {
                    type = type,
                    id = id,
                    terrain = terrain,
                    sprite = sprite
                });
            }
        }

        private static void AddTerrainSprite(FossickArtCatalog catalog, FossickTerrainType terrain, string id, string path)
        {
            var sprite = LoadSprite(path);
            if (sprite != null)
            {
                catalog.layer2Terrain.terrainSprites.Add(new FossickTerrainSpriteEntry
                {
                    terrain = terrain,
                    id = id,
                    sprite = sprite
                });
            }
        }

        private static void AddNamed(List<FossickNamedSpriteEntry> entries, string id, Sprite sprite)
        {
            if (!string.IsNullOrEmpty(id) && sprite != null)
            {
                entries.Add(new FossickNamedSpriteEntry { id = id, sprite = sprite });
            }
        }

        private static Sprite LoadFirstSprite(params string[] candidates)
        {
            for (var i = 0; i < candidates.Length; i++)
            {
                var sprite = LoadSprite(candidates[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Sprite LoadSprite(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + relativePath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

    }
}
