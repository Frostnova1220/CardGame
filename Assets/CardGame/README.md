# 卡牌对战游戏开发框架

本目录提供了一套可直接运行、可继续扩展的 Unity 回合制卡牌对战基础框架。当前样例是“玩家角色 vs AI 角色”的 1 对 1 对战，重点不是做完整商业玩法，而是先把数据、规则、流程、AI、UI、存档和测试的边界划清。

## 1. 快速运行

1. 用 Unity 6000.5.1f1 打开项目。
2. 打开 `Assets/Scenes/SampleScene.unity`。
3. 点击 Play。

`GameBootstrap` 会在运行时自动创建 `BattleApplication`，随后生成战斗 UI。当前不需要在场景里手动挂脚本，也不需要配置 Prefab。

## 2. 当前玩法

- 玩家和敌人各有 50 点生命、3 点能量。
- 玩家初始抽 4 张牌，之后每个回合开始抽 1 张。
- 每张牌消耗能量；本回合没有用完的能量不会保留。
- 格挡优先抵消伤害，并在拥有者下一次回合开始时清空。
- 中毒在拥有者回合开始时无视格挡造成伤害，然后减少 1 层。
- 牌库抽空后会将弃牌堆洗回牌库。
- 玩家点击手牌即可自动选择合法目标并结算。
- AI 会选择启发式评分最高的可打出牌，打完后自动结束回合。
- 击败敌人进入胜利结算；玩家生命归零进入失败结算。
- 本地档案会记录胜负、当前连胜和最佳连胜。

## 3. 代码结构

```text
Assets/CardGame/
├─ Scripts/
│  ├─ Core/
│  │  └─ StateMachine.cs
│  ├─ Cards/
│  │  ├─ CardDefinition.cs
│  │  ├─ DeckDefinition.cs
│  │  └─ DemoContentFactory.cs
│  ├─ Battle/
│  │  ├─ BattleTypes.cs
│  │  ├─ BattleActorState.cs
│  │  ├─ BattleSession.cs
│  │  └─ SimpleBattleAi.cs
│  ├─ Presentation/
│  │  ├─ UiFactory.cs
│  │  ├─ CardView.cs
│  │  ├─ BattlePresenter.cs
│  │  └─ GameBootstrap.cs
│  └─ Persistence/
│     ├─ PlayerProfile.cs
│     └─ JsonPlayerProfileStore.cs
└─ Tests/
   ├─ EditMode/
   │  └─ BattleSessionTests.cs
   └─ PlayMode/
      └─ BattlePresenterSmokeTests.cs
```

## 4. 各模块职责

### Core / StateMachine

提供通用有限状态机。当前用于 `NotStarted → PlayerTurn ↔ EnemyTurn → Victory/Defeat`，后续也可以复用于主菜单、匹配、房间、结算等流程模块。

### Cards / CardDefinition

定义卡牌静态数据：ID、名称、描述、费用、稀有度、类型和效果列表。效果数据由 `CardEffectType` 与 `CardTarget` 组合而成，当前支持：

- `Damage`：伤害
- `Block`：格挡
- `Heal`：治疗
- `DrawCards`：抽牌
- `GainEnergy`：获得能量
- `Poison`：中毒

可以在 Unity 中通过 `Create/Card Game/Card` 创建卡牌资产。运行中的卡牌实例使用 `RuntimeCard`，它拥有独立实例 ID，不会修改静态定义。

### Cards / DeckDefinition

定义卡组及其中的卡牌数量。可以通过 `Create/Card Game/Deck` 创建资产，也可以在代码中组装演示卡组。战斗开始时，卡组定义会被展开为运行时卡牌实例。

### Battle / BattleSession

战斗规则核心，是纯 C# 类，不引用 UI、MonoBehaviour 或场景对象。它负责：

- 初始化牌库、手牌和弃牌堆
- 校验回合、卡牌归属、能量和战斗状态
- 执行卡牌效果
- 处理伤害、格挡、治疗、抽牌、能量、中毒
- 推进玩家/敌方回合
- 判断胜负并发送状态、日志和结算事件

UI 只能向它提交“打牌”或“结束回合”意图，不能自行修改生命、能量等状态。这能让规则测试不依赖场景，也方便未来将同一套核心放到服务器权威模拟中。

### Battle / SimpleBattleAi

一个可替换的启发式 AI。它根据伤害、格挡、治疗缺口、抽牌和中毒效果给卡牌打分，然后选择最高分且当前可打出的牌。后续可以替换为行为树、效用 AI、规则脚本或搜索式 AI。

### Presentation / BattlePresenter

表现层控制器：

- 监听战斗核心事件
- 将玩家点击转换成战斗意图
- 刷新英雄数据、手牌、牌堆数量、日志、结算界面
- 控制敌方回合的展示节奏

它不实现战斗规则。未来接入正式卡牌动画、音效、特效和 Prefab 时，可以保留事件接口，替换 UI 构建和动画实现。

### Presentation / CardView 与 UiFactory

`CardView` 只显示卡牌数据并上报点击；`UiFactory` 负责运行时创建 UGUI 控件。这样可以先在一个空场景中验证玩法，之后再迁移到美术 Prefab。

### Persistence / JsonPlayerProfileStore

通过接口隔离存档实现，当前使用 `JsonUtility` 写入 `Application.persistentDataPath/card-game-profile.json`。未来可以替换为云存档、SQLite、平台 SDK 或加密存储。

### Tests / BattleSessionTests

EditMode 测试覆盖状态机、打牌消耗、伤害、格挡、回合重置和致命伤害结算；PlayMode 冒烟测试验证运行时 UI、事件系统和战斗启动链路。建议以后所有规则改动都先补充或更新对应测试。

## 5. 一局的执行流程

```text
GameBootstrap
  → BattleApplication
  → BattlePresenter 创建 UI
  → BattleSession.Start()
  → 洗牌并抽取初始手牌
  → 玩家回合
      → 点击卡牌
      → BattleSession 校验并结算
      → 更新 UI 与日志
  → 点击结束回合
  → AI 选择并打出卡牌
  → AI 结束回合
  → 回到玩家回合
  → 生命归零时结算胜负
```

## 6. 新增一张卡牌

### 方式一：Inspector 资产

1. 在 Project 窗口中右键。
2. 选择 `Create/Card Game/Card`。
3. 填写 ID、名称、费用、类型和效果列表。
4. 将卡牌加入某个 `DeckDefinition`。

### 方式二：代码创建

```csharp
CardDefinition card = CardDefinition.CreateRuntime(
    "fireball",
    "火球",
    "造成 10 点伤害。",
    2,
    CardRarity.Rare,
    CardType.Attack,
    new CardEffectData(CardEffectType.Damage, CardTarget.Opponent, 10));
```

如果要加入新的效果类型，应先扩展 `CardEffectType`、编辑器显示、`BattleSession.ResolveEffect` 以及对应测试。不要把具体卡牌逻辑直接塞进 `BattlePresenter`。

## 7. 建议的后续扩展顺序

1. 加入目标选择系统，支持选择己方/敌方随从和指向性法术。
2. 将 `BattleActorState` 拆分为英雄和 `BattleEntity`，加入场面、攻击力、生命值和召唤机制。
3. 增加关键词系统，例如嘲讽、冲锋、连击、冻结、护盾。
4. 将卡牌效果升级为 ScriptableObject 或数据驱动表达式，支持复杂触发器和条件。
5. 加入主菜单、关卡选择、奖励、卡牌升级与卡组编辑。
6. 将 UI 从运行时构建迁移为 Prefab + Addressables，并接入动画、音效、特效。
7. 如果要做联机，采用“服务器权威 + 客户端预测/表现”的结构；规则核心应继续保持在纯 C# 层。
8. 增加回放、遥测、平衡数据采集和自动对战平衡测试。

## 8. 当前边界与注意事项

- 目前是单人本地对战原型，AI 在表现层协程中按节奏出牌。
- 当前只有英雄目标，没有随从、装备和复杂目标选择。
- 运行时 UI 主要用于验证框架，不应直接作为最终商业 UI 架构。
- 存档失败会记录 Warning 并继续使用默认档案，不会阻断战斗。
- EditMode 测试需要 Unity Test Framework；项目已包含该包。

## 9. 已完成的验证

- Unity 6000.5.1f1 批处理编译通过。
- 4 个 EditMode 规则测试全部通过。
- 1 个 PlayMode UI/启动冒烟测试通过。
- 编译日志中无 `error CS` 或 warning CS（修复后的测试构建）。


