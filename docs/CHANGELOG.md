# 更新日志（CHANGELOG）

## 1.3.0.0 — 事件类型、消息格式与详细程度大幅扩展

在不破坏既有播放通知、旧配置与插件 GUID 的前提下，扩展了 Emby 事件覆盖面、消息表现力与安全性。

### 新增
- **统一事件模型** `NotificationEvent`（分类/严重程度/字段/去重键），所有事件回调即时转换为不可变快照后入队，绝不把 Emby 原始对象放入队列。
- **事件源架构**：按职责拆分为 8 个 `IEventSource`（播放/会话/用户/媒体库/用户数据/任务/Live TV/服务器），`EntryPoint` 改为组装容器；可选源失败不影响整体。
- **播放增强**：播放完成、中途放弃、播放方式变化、进度里程碑；停止分类互斥（完成/放弃/停止只推一条）。
- **登录与会话**：登录成功/失败、会话开始/结束、远程控制断开、加入/离开同步播放。
- **用户管理**：锁定、改密、创建/删除/更新、策略/配置更新。
- **媒体库**：新增/更新/删除 + 窗口聚合（防扫描消息风暴），基于真实类型过滤非媒体项。
- **用户行为**：收藏/取消收藏、标记已看/未看、评分变化（缓存旧状态，仅真正变化才推）。
- **计划任务**：失败/完成/取消、库扫描开始/完成、元数据刷新、备份完成（按稳定 Key 分类，错误摘要脱敏）。
- **Live TV（可选）**：录制与定时事件；不可用时不加载、不影响其他功能。
- **服务器**：启动/停止、有更新/已更新、需要重启、维护模式进出。
- **消息格式**：新增飞书交互卡片（按严重程度配色、双列字段、页脚），卡片失败可回退文本一次。
- **详细程度**：Simple / Standard / Detailed / Custom（默认 Custom，播放沿用旧字段开关，外观不变）。
- **去重/限流/聚合**：`NotificationPolicy`（有界去重缓存 + 每分钟滑动窗口限流，安全事件可豁免）、媒体库聚合器。
- **敏感信息脱敏器** `SensitiveDataSanitizer`：Webhook / IP / 设备 ID / 异常 / 绝对路径。IP 与设备 ID 默认脱敏。
- **插件图标**：`logo/EmbyFeishu.png` 作为嵌入资源，通过 `IHasThumbImage` 显示。
- **配置迁移** `ConfigMigrator`：旧配置平滑加载、数值夹紧、架构版本标记。
- 文档：EVENT-CATALOG、MESSAGE-FORMATS、CONFIGURATION、CHANGELOG，更新 TESTING。

### 安全
- 绝不读取/记录/推送 `AuthenticationRequest.Password` 与 `AuthenticationResult.AccessToken`。
- 任务失败仅用简短 `ErrorMessage` 并脱敏路径，不含堆栈/Token/绝对路径。
- 日志与消息中的 Webhook 全程脱敏。

### 兼容
- 插件 GUID、配置文件名、Webhook 不变；旧配置可继续加载；新字段安全默认。
- 默认 `Text` + `Custom`，播放通知外观与 1.2.0.0 一致。

### 测试与构建
- 自测扩充至 **150 项全部通过**。
- Debug / Release 均 **0 警告 0 错误**；Release 产物仅 `EmbyFeishu.dll`。

### API 差异
- `IApplicationHost.HasUpdateAvailableChanged` 在 4.9.5.0 不存在，改用 `IServerApplicationHost.HasUpdateAvailableChanged`。其余目标事件均存在。

### 已知限制
- 登录失败采用 30 秒去重 + 安全豁免限流；“最近 N 分钟失败 N 次”式聚合为简化实现。
- 服务器停止通知为尽力而为（2 秒短超时），强杀/断电无法保证送达。
- 播放方式依赖客户端上报 `PlayMethod`，缺失时不猜测、不通知。
- 同步播放（Party）事件受 4.9.5.0 载荷限制，仅推送标题级信息。

---

## 1.2.0.0 — 安全加固
- 关闭 Emby HttpClient 默认日志避免 Webhook 泄露；异常消息脱敏；重试判定改用 `HttpException`；统一会话键；非飞书域名改为非阻断。

## 1.1.0.0 / 1.0.0.0 — 初始版本
- 播放开始/停止/暂停/恢复推送、用户过滤、暂停恢复去重、Simple UI 配置、Webhook 脱敏。
