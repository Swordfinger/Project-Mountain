# 上手指南

## 1. 创建 Unity 项目

1. 安装 Unity Hub，下载 **Unity 2022.3 LTS**
2. 新建项目：模板选 **3D (URP)**，名字 `JailerGame`
3. 关闭 Unity Editor

## 2. 复制本框架代码

把本仓库的 `Assets/` 目录下所有内容复制到你新建的 `JailerGame/Assets/` 下，覆盖即可。

打开 Unity Editor，等待编译。**预期 0 个编译错误**。

## 3. 安装依赖（按需）

打开 `Window → Package Manager`，添加：

| 包名 | 用途 | 阶段 |
|---|---|---|
| Mirror（GitHub Releases） | 联机框架 | 第五阶段才用 |
| Facepunch.Steamworks（GitHub） | Steam 集成 | 第五阶段才用 |
| Hex Game Studio（Asset Store） | 蜂窝格视觉资源 | 第一阶段可选 |

**第一阶段只需要 URP 自带的内容**，不用装额外的包。

## 4. 创建你的第一个战斗场景

### 步骤 4.1 创建场景

`File → New Scene → Basic (URP)`，保存为 `Assets/Scenes/CombatTest.unity`。

### 步骤 4.2 创建 GameObject

在 Hierarchy 创建以下空物体：

```
- GameRoot (空)
  ├── HexGridManager   (挂 HexGridManager.cs)
  ├── TurnManager      (挂 TurnManager.cs)
  ├── CombatManager    (挂 CombatManager.cs)
  └── DebugOverlay     (空，可选)
```

### 步骤 4.3 配置 HexGridManager

选中 `HexGridManager`，在 Inspector 里设置：

- `Grid Width`: 8
- `Grid Height`: 8
- `Cell Size`: 1.0

### 步骤 4.4 创建第一个角色（刺客）

1. 右键 `Assets/ScriptableObjects/Characters` → Create → JailerGame → Character Data
2. 命名 `Assassin_Data.asset`
3. 在 Inspector 里填入：
   - Class: `Assassin`
   - Max HP: 80
   - Base Speed: 8
   - Move Range: 3
4. 在场景里创建 `Player_Assassin` GameObject，挂上 `PlayerController.cs`，把上面的 SO 拖进 `Character Data` 字段

### 步骤 4.5 创建一张卡

1. 右键 `Assets/ScriptableObjects/Cards` → Create → JailerGame → Card Data
2. 命名 `Card_Assassinate.asset`
3. 填入：
   - Card Name: 刺杀
   - Energy Cost: 1
   - Effects: 添加一个 `DamageEffect`，伤害 5

### 步骤 4.6 运行

按播放键。控制台应输出回合开始日志。第一阶段还没有 UI，所以画面看起来是空的，但日志会显示一切都在跑。

## 5. 常见问题

**Q: 编译报错 "The type or namespace name 'Mirror' could not be found"？**
A: 第一阶段不需要 Mirror，相关代码我已用 `#if MIRROR` 包起来。如果还报错，去 `Networking/` 目录把那几个文件先删掉，第五阶段再加回来。

**Q: 蜂窝格在场景里看不到？**
A: 第一阶段 `HexGridManager` 只生成数据，不渲染。如果你想看到，可以临时给每个 HexCell 实例化一个 Cylinder 当占位（代码里有注释指出哪一行加）。或者等买了 Hex Game Studio 资源包再做视觉。

**Q: 如何添加新卡？**
A: 右键 ScriptableObjects 文件夹 → Create → JailerGame → Card Data，配置完字段，在角色数据的卡组列表里加进去就行，不需要改代码。

**Q: 如何添加新职业？**
A: 在 `Assets/Scripts/Characters/` 下新建一个继承 `CharacterBase` 的脚本，参考 `Assassin.cs` 的写法。然后创建对应的 `CharacterData` SO 和 `BreakPointType` 枚举值。

## 6. 下一阶段你需要给我什么反馈

第一阶段跑通后，请告诉我：

1. **核心战斗手感**：同时出牌 + 速度结算这套节奏，玩起来是否有"博弈感"？
2. **破绽系统**：6 边独立刷新，玩家是否能记住和规划？
3. **数值平衡**：刺杀 5 伤害、HP 80，是 3 回合击杀 Boss 还是 10 回合？
4. **代码可读性**：你们能看懂代码吗？哪里需要更详细的注释？

根据反馈我会调整第二阶段的方向。
