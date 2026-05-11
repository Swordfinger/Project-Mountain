# 第一阶段场景搭建详细指南

跟着这一步一步做，你们能在 30 分钟内跑出一个可玩的"刺客 vs 守门人"原型。

## 步骤 1：在 Unity 中创建 CardData 资产

打开 Unity 后，依次按 `Assets/Documentation/AssassinCards.csv` 里的内容，
为前 5 张卡（先做 5 张验证）创建 ScriptableObject：

1. 在 Project 窗口右键 `Assets/ScriptableObjects/Cards`
2. `Create → JailerGame → Card Data`
3. 命名 `Card_Assassinate`（对应 ASS_001）
4. 在 Inspector 填入：
   - cardId: `ASS_001`
   - cardName: `刺杀`
   - rarity: `Common`
   - category: `Attack`
   - timing: `Normal`
   - energyCost: `1`
   - targetType: `SingleEnemy`
   - range: `1`
   - **Effects → Add → Damage Effect → damage: 5, critMultiplier: 2.0**

重复给 ASS_002 (防御) / ASS_003 (暗中前行) / ASS_005 (难以治愈) / ASS_007 (投毒) 各创建一份。

## 步骤 2：创建 CharacterData

1. 右键 `Assets/ScriptableObjects/Characters`
2. `Create → JailerGame → Character Data`
3. 命名 `Char_Assassin`
4. 在 Inspector 填入：
   - characterId: `CHAR_ASSASSIN`
   - displayName: `影衣`（或随便起）
   - characterClass: `Assassin`
   - maxHp: 80
   - baseSpeed: 8
   - moveRange: 3
   - maxEnergyPerTurn: 3
   - initialHandSize: 5
   - **startingDeck**：拖入刚才创建的 5 张卡 × 各 2 份 = 10 张

## 步骤 3：创建 EnemyData（守门人）

1. 右键 `Assets/ScriptableObjects/Enemies`
2. `Create → JailerGame → Enemy Data`
3. 命名 `Enemy_Gatekeeper_Lv1`
4. 填入：
   - enemyId: `BOSS_GATE_1`
   - displayName: `第一层守门人`
   - type: `Gatekeeper`
   - baseMaxHp: 100
   - baseDamage: 8
   - baseSpeed: 5
   - moveRange: 1
   - **actionPatterns**:
     - 名字：重击 / Attack / value=10 / weight=2
     - 名字：防御 / Defend / value=8 / weight=1
     - 名字：横扫 / AOEAttack / value=5 / weight=1

## 步骤 4：搭建场景

1. `File → New Scene → Basic (URP)` 保存为 `Assets/Scenes/CombatTest.unity`
2. Hierarchy 创建空 GameObject `GameRoot`
3. 在 GameRoot 下创建子物体：
   ```
   GameRoot
   ├── HexGridManager   (空, 挂 HexGridManager.cs, 设 8x8)
   ├── TurnManager      (空, 挂 TurnManager.cs)
   ├── CombatManager    (空, 挂 CombatManager.cs)
   ├── UIPlaceholder    (空, 挂 UIPlaceholder.cs)
   ├── Player_Assassin  (空, 挂 PlayerController.cs, 拖 Char_Assassin SO)
   └── Enemy_Boss1      (空, 用于显示)
   ```
4. 选中 `CombatManager`，在 Inspector：
   - playerControllers: 拖 Player_Assassin
   - enemyHolders: Add 一个，填：
     - gameObject: 拖 Enemy_Boss1
     - enemyData: 拖 Enemy_Gatekeeper_Lv1
     - startCol: 7, startRow: 4
   - assignEmissary: **关闭**（单人测试用）

## 步骤 5：跑起来

按播放键，Console 应该输出：

```
[HexGridManager] 蜂窝格地图生成完成：8x8，共 64 格
[Player] 影衣 加入战斗：HP 80/80, 速度 8
========== 回合 1 开始 ==========
[Phase] → TurnStart
[Phase] → PlanPhase
... (30秒倒计时)
[Phase] → ResolvePhase
[Phase] → EnemyPhase
[Enemy] 第一层守门人 攻击 影衣，造成 X 伤害
...
```

## 步骤 6：手动出牌（测试用）

在 PlanPhase 阶段，写一个测试脚本调 `TurnManager.Instance.SubmitCard(...)`。
我会在第二阶段提供一个 `DebugCardPlayer.cs` 让你们用键盘 1/2/3/4/5 直接出手牌。

## 常见错误

**Q: Console 报 "BreakPointSystem could not be found"**
A: 检查 Scripts/Combat/BreakPoint/ 下 4 个文件是否都复制完整。

**Q: 玩家位置总是 (0,0,0)**
A: 这是世界坐标，蜂窝格 0,0,0 在原点是正确的。第二阶段加视觉时会改。

**Q: 想可视化蜂窝格**
A: 选中 HexGridManager，在 Scene 视图能看到蓝色蜂窝线（Gizmos 已开）。
