# XiYouJi 资源布局

## 统一资源入口

所有导入包资源统一放在 `Assets/GameContent/` 下，并按用途分类：

- `GameContent/01_Scenes/`：三个后续场景及其场景光照配置。
- `GameContent/02_Environment/`：森林、村庄、岩石、地形和地下世界等环境包，包内继续保留原有目录结构，避免同名资源冲突。

## 后续场景

- `GameContent/01_Scenes/Demo1.unity`
- `GameContent/01_Scenes/NatureStarterKit2_Realistic.unity`
- `GameContent/01_Scenes/village.unity`

这三个场景已加入 Build Settings，并作为当前项目的场景依赖入口。

## 环境包

- `GameContent/02_Environment/RealisticForestMaterials/`：森林粒子、地形和树木风动脚本。
- `GameContent/02_Environment/NatureStarterKit2/`：自然环境模型、材质和贴图。
- `GameContent/02_Environment/RockFREE/`：岩石模型和材质。
- `GameContent/02_Environment/Underworld/`：Demo1 场景使用的地下环境资源。
- `GameContent/02_Environment/DesertedVillage/`：village 场景使用的村庄资源。
- `GameContent/02_Environment/_TerrainAutoUpgrade/`：Unity 地形升级生成的项目资源。

`DiscoDialoguePrototype/` 和 `Editor/` 保留在 `Assets` 根目录：前者是项目对话系统原型，后者是 Unity 编辑器/MCP 工具，不属于环境包且需要固定目录才能正常工作。
