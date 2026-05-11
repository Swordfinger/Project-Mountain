# BossArena 测试场景搭建指南

## 目标
在 Unity 中跑通"Boss 螺旋编号 + 1v1 决斗 + Boss 撤退"完整流程，
Console 会输出每回合谁打谁、伤害多少、Boss 何时撤退。

## 编号规则（已实现 SpiralHexNumbering）
- 0 = Boss 自己
- 第 1 圈：1=右上(NE)、2=右(E)、3=右下(SE)、4=左下(SW)、5=左(W)、6=左上(NW)
- 第 2 圈：7~18 顺时针外扩
- 第 N 圈：6N 格

Boss 每次受到血量阈值伤害（80%/60%/40%/20%）→ 向"门方向"后退 1 格 → 重建编号。

## 场景搭建（5 分钟）

1. 新建场景 `Assets/Scenes/BossArenaTest.unity`

2. 创建空 GameObject，命名后挂组件：
   | GameObject 名 | 组件 | 关键参数 |
   |---|---|---|
   | HexGridManager | HexGridManager | gridWidth=8, gridHeight=8, cellSize=1 |
   | Boss | BossController | startCol=4, startRow=4, maxHp=200, baseSpeed=5 |
   | Player1 | PlayerController | characterData=Char_Assassin |
   | Player2 | PlayerController | characterData=Char_Merchant |
   | BossArenaSceneStarter | BossArenaSceneStarter | autoCreateManager=true |

3. 在 BossArenaSceneStarter 的 Inspector 里：
   - Boss → 拖入 Boss 物体
   - Player Controllers → 拖入 Player1、Player2
   - Player Start Offsets → 添加两项：(5,4)、(5,5)（Boss 右边和右下方各放一个玩家）

4. 创建 ScriptableObject：
   - `Char_Assassin.asset`（Class=Assassin，starting deck 至少 5 张刺客卡）
   - `Char_Merchant.asset`（Class=Merchant，starting deck 至少 5 张商人卡）
   - 各张卡 SO（按 AssassinCards.csv / MerchantCards.csv 配）

5. Play → Console 应看到：
```
[HexGridManager] 蜂窝格地图生成完成：8x8，共 64 格
[Boss] 测试守门人 部署到 (4,4), HP 200/200
[Player] 影衣 加入战斗：HP 80/80, 速度 8
[BossArena] 编号映射： #0=(4,4) #1=(5,5) #2=(5,4) ...
[BossArena] >>> 新一轮交战开始，出场顺序：#1影衣 → #2商人
[BossArena] —— 1v1 开始：Boss vs 影衣（编号 #1）——
  → 影衣 打出 [刺杀]
  → Boss 攻击 影衣，造成 10 伤害（剩 70/80）
...
[BossArena] Boss 血量降至 80%，触发第 1 次撤退
[BossArena] Boss 撤退至 (3,4)，重建编号
```

## 已实现 vs 未实现

✅ 已实现：
- 螺旋编号（圈 1-6）
- 编号最小者优先 1v1
- 速度高者先动；同速时玩家先
- Boss 4 段血量阈值撤退
- 刺客 12 张卡完整效果
- 商人 11 张卡完整效果（含局内金币系统）
- 流血 / 投毒 / 暴利时刻 / 反击姿态 等状态

❌ 未实现（下一阶段）：
- 同时出牌 + 30s 限时（目前是 AI 自动按顺序出牌）
- 玩家手动选目标格 / 朝向（目前 attackDirection=0 写死）
- "购物时间"真实商店 UI
- "时间就是金钱"伤害拦截路径（目前只标记状态，未拦截 TakeDamage）
- 联机同步（Mirror + Steam P2P）

## 调试技巧
- 在 BossArenaManager 的 maxRoundsPerDuel 改小（如 3）可快速跑完 1 个 1v1
- 在 BossController 的 attackDamage 改大可快速触发撤退
- HexGridManager 勾选 drawGizmos 可在 Scene 视图看到地图
