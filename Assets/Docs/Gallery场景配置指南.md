# Gallery 场景配置指南

## 目录
1. [快速开始](#快速开始)
2. [必要组件](#必要组件)
3. [场景结构](#场景结构)
4. [照片与媒体](#照片与媒体)
5. [地面与路径](#地面与路径)
6. [环境与氛围](#环境与氛围)
7. [交互元素](#交互元素)
8. [角色与NPC](#角色与npc)
9. [滤镜与相框](#滤镜与相框)
10. [编辑器工具](#编辑器工具)

---

## 快速开始

### 方法 A：自动生成示范场景
`Tools > Gallery > 创建 Gallery 场景`
会自动在 `Assets/Scenes/gallery.unity` 生成一个包含所有功能示范的完整场景。

### 方法 B：从空场景手动搭建

1. **初始化** — 执行 `Tools > Gallery > 初始化 Gallery（Prefab + 设置）`，自动生成所有 Prefab 和全局设置文件
2. **创建空场景** — `File > New Scene`
3. **拖入必要 Prefab** — 从 `Assets/Prefabs/Gallery/` 拖入下方「必要组件」
4. **摆放照片和元素** — 按需拖入其他 Prefab
5. **设置边界墙** — 用 `GalleryWall` 围出场景范围
6. **保存场景**

---

## 必要组件

每个 Gallery 场景**必须**包含以下物体：

| Prefab | 作用 | 关键设置 |
|--------|------|----------|
| **GalleryManager** | 全局管理器，入场黑屏+文字 | `introText`: 入场显示的文字<br>`introTextPosition`: 文字位置 (0~1)<br>`introFontSize`: 字号 |
| **GalleryPlayer** | 玩家角色 | `moveSpeed`: 移动速度<br>`playerSprite`: 角色贴图<br>`walkFrames`: 行走动画帧 |
| **Camera** (挂 `GalleryCamera`) | 分块摄像机 | `blockCount`: 区块数量<br>`firstBlockCenterX`: 第一个区块中心X<br>`transitionSpeed`: 区块切换速度 |

### 全局设置文件
`Assets/Resources/GallerySettings.asset` — 一个 ScriptableObject，控制全场通用参数：
- **场景灯光**: `ambientBrightness`, `ambientColor`
- **手电筒**: `flashlightRadius`, `flashlightColor`, `flashlightKey`
- **入场黑屏**: `introDuration`, `introFadeTime`
- **场景生成图片**: `filterPreviewImage`, `galleryTestImage`, `npcSprite`

---

## 场景结构

### 区块布局（Camera 系统）
Gallery 场景使用**区块制**摄像机，玩家跨越区块边界时摄像机平滑切换。

```
┌─────────┐┌─────────┐┌─────────┐┌─────────┐
│ Block 0  ││ Block 1  ││ Block 2  ││ Block 3  │
│ (0, 0)   ││ (36, 0)  ││ (72, 0)  ││ (108, 0) │
└─────────┘└─────────┘└─────────┘└─────────┘
```

`GalleryCamera` 设置：
- `blockCount` = 4
- `firstBlockCenterX` = 0（第一个区块的中心 X 坐标）
- `blockWidthOverride` = 0（留 0 则自动计算，即 `orthographicSize * 2 * aspect`）

### 边界墙
用 `GalleryWall` Prefab 在场景四周建墙。设置 `visible = false` 让墙不可见（纯碰撞）。

---

## 照片与媒体

### GalleryFrame — 静态照片
从 `Prefabs/Gallery/GalleryFrame` 拖入场景。

| 字段 | 说明 |
|------|------|
| `image` | 拖入要显示的 Sprite |
| `sortingOrder` | 图层排序 |
| `fadeInOnApproach` | 勾选 = 玩家走近才出现 |
| `fadeDistance` | 触发淡入的距离 |
| `caption` | 图片下方的说明文字 |

### GallerySlideshow — 幻灯片
在同一位置轮播多张照片。

| 字段 | 说明 |
|------|------|
| `slides` | 拖入多张 Sprite（按顺序播放） |
| `interval` | 每张停留秒数 |
| `crossfadeDuration` | 切换淡入淡出时长 |
| `loop` / `shuffle` | 循环 / 随机 |
| `startOnApproach` | 玩家走近才开始播放 |

### GalleryVideo — 视频
| 字段 | 说明 |
|------|------|
| `videoClip` 或 `videoPath` | 视频源（二选一） |
| `autoPlay` | 自动播放 |
| `fadeInOnApproach` | 走近才显示 |
| `triggerRange` | 发出声音的距离 |

### GalleryParallaxFrame — 视差照片
带前景/背景分层的照片，随玩家移动产生视差效果。

| 字段 | 说明 |
|------|------|
| `backgroundLayer` / `foregroundLayer` | 背景和前景 Sprite |
| `bgParallaxFactor` / `fgParallaxFactor` | 视差强度 |

---

## 地面与路径

### GalleryGround — 可绘制地面纹理
覆盖场景地面的大型 quad，通过 RGBA 遮罩贴图混合 4 层纹理。

**配置步骤：**
1. 拖入场景，设置 `groundWidth` 和 `groundHeight` 覆盖整个场景
2. 给 4 个纹理层各拖入一张贴图（草地、沙地、石板等），调颜色和平铺密度
3. 打开 `Tools > Gallery > 地面笔刷工具`
4. 点击"创建新遮罩"
5. 在 Scene 视图用鼠标涂抹（1/2/3/4 切换层，E 擦除，滚轮调大小）
6. 完成后点"保存遮罩到磁盘"

| 字段 | 说明 |
|------|------|
| `baseColor` | 基底颜色（未涂抹区域） |
| `layer1~4 Texture` | 4 层地面纹理贴图 |
| `layer1~4 Tint` | 每层颜色叠加 |
| `layer1~4 Tiling` | 每层纹理平铺密度 |

### GalleryPath — 小路
用控制点画出的曲线小路（Catmull-Rom 平滑）。

**配置步骤：**
1. 拖入场景
2. 选中后在 Scene 视图拖动黄色圆点调整路线
3. Inspector 可调宽度、颜色、或拖入自定义材质

| 字段 | 说明 |
|------|------|
| `points` | 路径控制点列表 |
| `pathWidth` | 路宽（世界单位） |
| `pathColor` | 纯色模式的颜色 |
| `customMaterial` | 自定义材质（如石头路），留空则纯色 |
| `edgeSoftness` | 边缘柔化（仅纯色模式） |

> 石头路材质：`Tools > Gallery > 生成石头路材质`，材质会保存在 `Assets/Art/PathMaterials/StonePath.mat`

### GalleryGroundTransition — 地面颜色过渡带
两种颜色之间的渐变条带，适合简单的区域分界。

---

## 环境与氛围

### GalleryBackground — 背景渐变
根据玩家位置在多个背景颜色/贴图之间渐变切换。

| 字段 | 说明 |
|------|------|
| `zones` 数组 | 每项有 `center`（位置）、`fallbackColor`（颜色）、`radius`（影响范围） |
| `defaultColor` | 没有区域覆盖时的默认背景 |

### GalleryParallaxSky — 视差天空/云层
多层视差飘动的背景装饰。

| 字段 | 说明 |
|------|------|
| `layers` 数组 | 每层有 `color`、`parallaxFactor`、`driftSpeed`、`yOffset`、`scale` |

### GalleryWeather — 天气效果
| 字段 | 说明 |
|------|------|
| `weatherType` | 0=雨, 1=雪, 2=雾, 3=阳光 |
| `particleCount` | 粒子数量 |
| `particleColor` | 粒子颜色 |
| `intensity` | 强度 |

### GalleryAreaParticles — 区域粒子
进入某区域时出现的装饰粒子（萤火虫、花瓣等）。

### GalleryAreaTitle — 区域标题
玩家进入区域时在屏幕上显示标题和副标题。

| 字段 | 说明 |
|------|------|
| `title` / `subtitle` | 标题和副标题文字 |
| `displayDuration` | 显示时长 |

### GalleryBGMZone — 背景音乐区域
| 字段 | 说明 |
|------|------|
| `bgmClip` | 音乐 AudioClip |
| `volume` | 音量 |
| `fadeTime` | 淡入淡出时间 |

---

## 交互元素

### GallerySign — 路牌/告示
玩家走近按键交互，上方弹出信息气泡。

| 字段 | 说明 |
|------|------|
| `text` | 显示的文字内容 |
| `displayDuration` | 自动消失时间 |

### GalleryMemory — 记忆点/文字交互
按键触发显示一段文字（支持打字机效果）。

| 字段 | 说明 |
|------|------|
| `memoryText` | 文字内容（多行） |
| `typeSpeed` | 打字速度 |

### GalleryPushBlock — 可推动方块
被玩家撞击后沿被撞方向滑动，碰到屏幕边缘会反弹。

| 字段 | 说明 |
|------|------|
| `pushSpeed` | 推动速度 |
| `maxSlideDistance` | 最大滑行距离 |
| `bounceDecay` | 反弹衰减系数 |

### GalleryImageDoor — 图片门
方块撞到门后，场景中一批图片和背景会切换。

| 字段 | 说明 |
|------|------|
| `frameSwaps` 数组 | 每项指定要切换的 GalleryFrame + 新图片 |
| `changeBackground` | 是否同时切换背景颜色 |

### GallerySecretDoor — 暗门/隐藏房间
可通过推块碰撞、手电筒照射、收集物等方式打开。

| 字段 | 说明 |
|------|------|
| `method` | 触发方式（PushBlock / Flashlight / Collectible） |
| `hiddenRoom` | 打开后激活的隐藏房间 GameObject |

### GalleryJigsaw — 拼图小游戏
| 字段 | 说明 |
|------|------|
| `fullImage` | 原始完整图片 |
| `columns` / `rows` | 切分列数和行数 |
| `interactRange` | 交互距离 |

### GalleryCollectible — 收集品
场景中的可拾取物品。

| 字段 | 说明 |
|------|------|
| `collectibleID` | 唯一标识 |
| `icon` | 显示的 Sprite |
| `pickupMessage` | 拾取时的提示文字 |

### GalleryPortal — 传送门
传送到场景内其他位置或加载新场景。

| 字段 | 说明 |
|------|------|
| `mode` | Teleport（场景内）/ LoadScene（跳转场景） |
| `targetPoint` / `targetScene` | 目标位置或场景名 |

### GalleryTimeline — 时间轴
底部的时间轴装饰线+标记点。

| 字段 | 说明 |
|------|------|
| `points` 数组 | 每项有 `position`、`dateText`、`color` |

---

## 角色与NPC

### GalleryFollower — 跟随NPC
碰到后会跟随玩家移动的人物。

| 字段 | 说明 |
|------|------|
| `npcSprite` | NPC 贴图 |
| `followDistance` | 保持距离 |
| `followSpeed` | 跟随速度 |

### GalleryNPCDialogue — 对话NPC
靠近后自动或手动触发对话气泡。

| 字段 | 说明 |
|------|------|
| `lines` 数组 | 每项有 `text`（台词）和 `duration`（显示时长） |
| `autoTrigger` | 是否自动触发 |
| `triggerDistance` | 触发距离 |

### GalleryVehicle — 交通工具
按 Tab 切换玩家外观和速度。

| 字段 | 说明 |
|------|------|
| `vehicles` 数组 | 每项有 `name`、`sprite`、`speed` |

---

## 滤镜与相框

### GalleryFilter — 滤镜效果
挂到任意有 SpriteRenderer 的物体上，支持 21 种风格化滤镜 + 15 种颜色滤镜。

常用风格化滤镜：Pencil, Oil, Watercolor, Comic, Impressionist, Glitch, VHS, Ukiyoe, PixelArt...

### GalleryPhotoFrame — 相框
挂到 GalleryFrame 物体上，添加装饰边框。

| 字段 | 说明 |
|------|------|
| `style` | None / SimpleBorder / Shadow / Polaroid |
| `frameColor` | 边框颜色 |
| `borderThickness` | 边框粗细 |

---

## 编辑器工具

所有工具在 Unity 菜单栏 `Tools > Gallery` 下：

| 菜单项 | 功能 |
|--------|------|
| **初始化 Gallery** | 生成所有 Prefab + GallerySettings |
| **创建 Gallery 场景** | 自动生成包含全部功能的示范场景 |
| **创建滤镜预览场景** | 查看所有滤镜效果的预览场景 |
| **地面笔刷工具** | 在 Scene 视图涂抹地面纹理 |
| **生成石头路材质** | 生成可用于 GalleryPath 的石头路材质 |

---

## 搭建新场景 Checklist

- [ ] 执行过 `Tools > Gallery > 初始化 Gallery`
- [ ] 场景里有 `GalleryManager`
- [ ] 场景里有 `GalleryPlayer`（玩家出生位置）
- [ ] Camera 挂了 `GalleryCamera`，配置好区块数
- [ ] 四周有 `GalleryWall` 围边
- [ ] 有 `GalleryBackground` 控制背景颜色
- [ ] 拖入若干 `GalleryFrame` 放置照片
- [ ] (可选) `GalleryGround` + 笔刷画地面
- [ ] (可选) `GalleryPath` 画小路
- [ ] (可选) `GalleryWeather` 天气效果
- [ ] (可选) NPC、收集品、传送门等交互元素
- [ ] 保存场景
