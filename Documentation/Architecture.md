# 架构文档

## 整体设计哲学

整个项目采用 **数据驱动 + 事件驱动 + ScriptableObject 资产** 的组合：

- **数据驱动**：角色数值、卡牌效果、敌人参数全部存在 ScriptableObject 里。策划改数值不需要改代码，直接在 Unity Inspector 里调。
- **事件驱动**：战斗中的"出牌""受伤""Boss 转向""破绽刷新""回合结束"全部通过 C# event 通知监听者。UI 只订阅事件，不主动查询，便于解耦和未来加联机同步。
- **ScriptableObject**：每张卡、每个角色、每个敌人都是独立的 .asset 文件，便于版本控制、AB 测试和 DLC 扩展。

## 核心系统分层

```
┌─────────────────────────────────────────┐
│          UI 层（占位，后续美术做）         │
└──────────────────▲──────────────────────┘
                   │ 事件订阅
┌──────────────────┴──────────────────────┐
│          表现层 (Visualizers)            │
│  HexCellView / CharacterView / CardView  │
└──────────────────▲──────────────────────┘
                   │
┌──────────────────┴──────────────────────┐
│          逻辑层 (Pure C#)                │
│  HexGrid · BreakPoint · TurnManager     │
│  Card · CombatEntity · DamageCalc        │
└──────────────────▲──────────────────────┘
                   │
┌──────────────────┴──────────────────────┐
│       数据层 (ScriptableObject)          │
│  CardData · CharacterData · EnemyData   │
└─────────────────────────────────────────┘
```

**关键点**：逻辑层是纯 C#，**不依赖 MonoBehaviour**，这是为了未来联机时把"权威逻辑"放服务端跑得动，反作弊就靠这一层。

## 蜂窝格选型说明

我们用 **Cube Coordinates**（立方坐标）`(x, y, z)` 满足 `x+y+z=0`：

- 优点：邻居计算 = 6 个固定向量，距离 = `(|dx|+|dy|+|dz|)/2`，旋转/反射极易实现
- 缺点：3 个分量但 1 个冗余，需要校验
- 转换：渲染时通过 `HexCoordinates.ToWorld()` 转世界坐标

参考资料：https://www.redblobgames.com/grids/hexagons/

## 破绽系统的 Class 化

每个职业看到的破绽是不同的：

| 职业 | 破绽 Class | 视觉表现 |
|---|---|---|
| 刺客 | `Wound` 伤口 | 红色裂痕 |
| 神偷 | `PickPoint` 可偷点 | 闪光的钱袋图标 |
| 战士 | `Crack` 护甲缝隙 | 银色刻痕 |
| 法师 | `MagicWeak` 魔法弱点 | 紫色符文 |
| 商人 | `Greed` 贪欲点 | 金色硬币 |
| 牧师 | `Sin` 罪孽点 | 黑色十字 |

**重要规则**：
- 每个玩家**独立刷新自己的破绽**，互不干扰（你回答的 Q48 选 B）
- Boss 转向会**同时**让所有玩家的破绽方向重新计算
- 每个破绽位置（6 个边）按概率独立刷新（类剑姬被动）

## 同时出牌 + 速度结算

每回合流程（30 秒倒计时）：

```
1. 准备阶段（5s）
   - 显示破绽位置
   - 玩家从手牌选 1~N 张卡放进"出牌区"
   - 玩家可锁定/取消锁定，但不能预览结算

2. 提交阶段（25s）
   - 倒计时归零或全员锁定后进入结算

3. 结算阶段
   - 按 [基础速度 → 离Boss距离] 排序
   - 逐个执行，期间允许"反客为主"等响应卡牌打断
   - 结算完毕进入 Boss 回合

4. Boss 回合
   - Boss AI 决策（攻击 / 移动 / 释放技能）
   - 可能触发转向，刷新所有玩家破绽

5. 进入下回合
```

## 联机预留接口（第五阶段实现）

所有"会改变状态"的操作都通过 `ICommand` 接口走：

```csharp
public interface ICommand
{
    bool Validate(GameState state);  // 服务端验证
    void Execute(GameState state);   // 实际执行
    byte[] Serialize();              // 网络序列化
}
```

第五阶段把 `Execute` 放服务端跑，客户端只发 Command，反作弊就成立了。

## 文件命名约定

- **逻辑类**：`PascalCase.cs`，纯 C#，无 `using UnityEngine`（除非真的需要 Vector3）
- **MonoBehaviour**：以 `Manager` / `Controller` / `View` 结尾
- **ScriptableObject**：以 `Data` 结尾（如 `CardData`、`CharacterData`）
- **接口**：`I` 前缀（如 `ICommand`、`ICardEffect`）
- **枚举**：`PascalCase`（如 `CharacterClass`、`BreakPointType`）
