# Fossick 美术资源命名规范

本文约定 `Assets/Fossick/Resources/FossickArt` 下的资源目录和命名方式。目标是让程序、策划和美术能从目录和文件名判断资源用途，同时让资源通过 `FossickArtCatalog` 稳定索引。

## 通用规则

- 文件名使用小写英文、数字和下划线。
- 不使用中文、空格、括号、`final`、`new`、`copy` 这类临时描述。
- 目录表达地图层级，文件名表达具体内容。
- 地图配置里的 ID 不等于图片文件名。
- 运行时资源查找以 `FossickArtCatalog` 为准，不解析文件名。

## 资源层级

```text
FossickArt/
  Layer0_Background/
  Layer1_RewardBackground/
  Layer2_Terrain/
  Layer3_TerrainAttachment/
  Layer4_Reward/
  Layer5_Decoration/
  Layer6_Fog/
  FossickArtCatalog.asset
```

对应地图显示层级：

```text
0 背景层
1 奖励区域背景层
2 挖掘物材质层
3 地形附着物层
4 挖出后的奖励和道具实体层
5 装饰层
6 阴影层
```

`FossickVisualLayer`、资源目录和 `FossickArtCatalog` 分组应保持同一套层级认知。

## 四方连续资源

四方连续资源用于地形和阴影拼接。运行时不解析文件名，实际顺序由 Catalog 配置决定。

命名建议：

```text
tile_01_top_left.png
tile_02_top_right.png
tile_03_top_left_top_right.png
tile_04_bottom_left.png
tile_05_top_left_bottom_left.png
tile_06_top_right_bottom_left.png
tile_07_top_left_top_right_bottom_left.png
tile_08_bottom_right.png
tile_09_top_left_bottom_right.png
tile_10_top_right_bottom_right.png
tile_11_top_left_top_right_bottom_right.png
tile_12_bottom_left_bottom_right.png
tile_13_top_left_bottom_left_bottom_right.png
tile_14_top_right_bottom_left_bottom_right.png
tile_15_full.png
tile_single.png
tile_outer_corner_bottom_left.png
tile_outer_corner_bottom_right.png
```

语义说明：

- `top_left`：左上角参与连接。
- `top_right`：右上角参与连接。
- `bottom_left`：左下角参与连接。
- `bottom_right`：右下角参与连接。
- `full`：四角都连接。
- `single`：四周都不连接的单格。
- `outer_corner`：外转角补片，用于处理拼接转角。

## 地形层

地形放在 `Layer2_Terrain`：

```text
Layer2_Terrain/Dirt/
Layer2_Terrain/Stone/
Layer2_Terrain/Bedrock/
```

石头如果有低血量破损图，可命名为：

```text
stone_damaged_1hp.png
```

## 地形附着物层

地形附着物放在 `Layer3_TerrainAttachment`。

这类资源只在地形还存在时显示。地形被挖掉后，它们会消失，并根据配置生成对应实体。

示例：

```text
ore_gold_dirt.png
ore_gold_stone.png
tool_pickaxe_dirt.png
tool_pickaxe_stone.png
tool_tnt_dirt.png
tool_tnt_stone.png
chest_mystery_dirt.png
```

如果某张附着图不区分土和石，可以在 Catalog 中配置为通用图。

## 奖励实体层

挖出后的实体奖励和道具放在 `Layer4_Reward`：

```text
Layer4_Reward/Ores/
Layer4_Reward/Tools/
Layer4_Reward/Coins/
Layer4_Reward/Chests/
Layer4_Reward/Collections/
Layer4_Reward/Signs/
```

示例：

```text
ore_gold.png
tool_pickaxe.png
tool_tnt.png
coin_stack_small.png
coin_stack_medium.png
coin_stack_large.png
chest_closed.png
collection_bottle.png
explosive_crate_sign.png
```

## 背景和藏宝阁

矿井底图：

```text
Layer0_Background/mine_default.png
Layer0_Background/mine_map.png
Layer0_Background/mine_variant.png
```

藏宝阁背景：

```text
Layer1_RewardBackground/treasure_room_3x2.png
Layer1_RewardBackground/treasure_room_5x2.png
Layer1_RewardBackground/treasure_room_7x2.png
```

藏宝阁背景是区域资源，不是单格资源。
