using System.Collections.Generic;
using System.IO;
using Fossick.Core.Config;
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

            catalog.autoTileSets.Clear();
            catalog.rewardSprites.Clear();
            catalog.toolSprites.Clear();
            catalog.decorations.Clear();
            catalog.backgrounds.Clear();

            AddAutoTileSet(catalog, FossickTerrainType.Dirt, "Dirt");
            AddAutoTileSet(catalog, FossickTerrainType.Stone, "Stone");
            AddAutoTileSet(catalog, FossickTerrainType.Unbreakable, "Rock");
            AddAutoTileSet(catalog, "Fog");

            var mineBackground = LoadSprite("Backgrounds/底图2.png")
                ?? LoadSprite("Backgrounds/底图3.png")
                ?? LoadSprite("Backgrounds/地图1.png");
            var treasureRoomBackground = LoadSprite("TreasureBackgrounds/33.png")
                ?? LoadSprite("TreasureBackgrounds/32.png")
                ?? LoadSprite("TreasureBackgrounds/31.png")
                ?? mineBackground;

            AddReward(catalog, FossickElementType.Coin, string.Empty, "Rewards/General/28.png", "Rewards/General/35.png", "Rewards/General/36.png");
            AddReward(catalog, FossickElementType.Score, string.Empty, "Rewards/General/29.png", "Rewards/General/30.png");
            AddReward(catalog, FossickElementType.Ore, "ore_copper", "Rewards/Digged/5.png");
            AddReward(catalog, FossickElementType.Ore, "ore_gem", "Rewards/Digged/6.png");
            AddReward(catalog, FossickElementType.Ore, "ore_gold", "Rewards/Digged/8.png");
            AddReward(catalog, FossickElementType.Ore, "ore_silver", "Rewards/Digged/9.png");
            AddReward(catalog, FossickElementType.Item, "pickaxe", "Rewards/Digged/24.png");
            AddReward(catalog, FossickElementType.Item, "dynamite", "Rewards/Digged/26.png");
            AddReward(catalog, FossickElementType.Item, "tnt", "Rewards/Digged/27.png");
            AddReward(catalog, FossickElementType.Item, "radar", "Rewards/Digged/25.png");
            AddReward(catalog, FossickElementType.Item, string.Empty, "Rewards/Digged/24.png", "Rewards/Digged/25.png", "Rewards/Digged/26.png", "Rewards/Digged/27.png");
            AddReward(catalog, FossickElementType.Chest, string.Empty, "Rewards/General/宝箱关上.png", "Rewards/General/20.png");
            AddReward(catalog, FossickElementType.Collection, string.Empty, "Rewards/General/34.png", "Rewards/General/漂流瓶阴影.png");

            AddTool(catalog, FossickToolType.Pickaxe, "Rewards/Digged/24.png");
            AddTool(catalog, FossickToolType.Dynamite, "Rewards/Digged/26.png");
            AddTool(catalog, FossickToolType.Tnt, "Rewards/Digged/27.png");
            AddTool(catalog, FossickToolType.Radar, "Rewards/Digged/25.png");

            AddNamed(catalog.decorations, "grass_large", LoadSprite("Attachments/Static/21.png"));
            AddNamed(catalog.decorations, "grass_small", LoadSprite("Attachments/Static/22.png"));
            AddNamed(catalog.decorations, "mushroom", LoadSprite("Attachments/Static/23.png"));

            AddNamed(catalog.backgrounds, "mine_default", mineBackground);
            AddNamed(catalog.backgrounds, "mine_map_1", LoadSprite("Backgrounds/地图1.png"));
            AddNamed(catalog.backgrounds, "mine_bottom_2", LoadSprite("Backgrounds/底图2.png"));
            AddNamed(catalog.backgrounds, "mine_bottom_3", LoadSprite("Backgrounds/底图3.png"));
            AddNamed(catalog.backgrounds, "treasure_room_3x2", LoadSprite("TreasureBackgrounds/31.png"));
            AddNamed(catalog.backgrounds, "treasure_room_5x2", LoadSprite("TreasureBackgrounds/32.png"));
            AddNamed(catalog.backgrounds, "treasure_room_7x2", treasureRoomBackground);

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

        private static void AddAutoTileSet(FossickArtCatalog catalog, FossickTerrainType terrain, string folder)
        {
            var set = new FossickAutoTileSet { kind = FossickAutoTileSetKind.Terrain, terrain = terrain };
            var paths = Directory.GetFiles(Path.Combine(ArtRoot, "AutoTiles", folder), "*.png");
            for (var i = 0; i < paths.Length; i++)
            {
                var path = NormalizePath(paths[i]);
                var index = ParseLeadingInt(Path.GetFileNameWithoutExtension(path));
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (index > 0 && sprite != null)
                {
                    set.sprites.Add(new FossickAutoTileSpriteEntry { index = index, sprite = sprite });
                }
            }

            set.sprites.Sort((a, b) => a.index.CompareTo(b.index));
            catalog.autoTileSets.Add(set);
        }

        private static void AddAutoTileSet(FossickArtCatalog catalog, string folder)
        {
            var set = new FossickAutoTileSet { kind = FossickAutoTileSetKind.Fog };
            var paths = Directory.GetFiles(Path.Combine(ArtRoot, "AutoTiles", folder), "*.png");
            for (var i = 0; i < paths.Length; i++)
            {
                var path = NormalizePath(paths[i]);
                var index = ParseLeadingInt(Path.GetFileNameWithoutExtension(path));
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (index > 0 && sprite != null)
                {
                    set.sprites.Add(new FossickAutoTileSpriteEntry { index = index, sprite = sprite });
                }
            }

            set.sprites.Sort((a, b) => a.index.CompareTo(b.index));
            catalog.autoTileSets.Add(set);
        }

        private static void AddReward(FossickArtCatalog catalog, FossickElementType type, string id, params string[] candidates)
        {
            var sprite = LoadFirstSprite(candidates);
            if (sprite != null)
            {
                catalog.rewardSprites.Add(new FossickElementSpriteEntry { type = type, id = id, sprite = sprite });
            }
        }

        private static void AddTool(FossickArtCatalog catalog, FossickToolType type, params string[] candidates)
        {
            var sprite = LoadFirstSprite(candidates);
            if (sprite != null)
            {
                catalog.toolSprites.Add(new FossickToolSpriteEntry { type = type, sprite = sprite });
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

        private static string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }

        private static int ParseLeadingInt(string value)
        {
            var number = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch < '0' || ch > '9')
                {
                    break;
                }

                number = number * 10 + ch - '0';
            }

            return number;
        }
    }
}
