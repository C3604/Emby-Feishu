# 更新日志（CHANGELOG）

本项目遵循语义化版本。插件 GUID 始终保持不变：`d3a7f1b2-8c4e-4f5a-9b6d-2e1c0a3f5d7b`。

## 1.5.0.0 — 运行诊断、配置预设、指数退避与优先级队列

### 新增
- **通知发送统计**：`NotificationStatistics` 实时跟踪发送成功/失败/重试/丢弃次数、队列深度峰值、连续失败数、最后成功/失败时间。
- **运行诊断面板**：配置页「高级与诊断」组新增只读字段「运行诊断信息」和「Webhook 连接状态」，每 5 分钟自动刷新。
- **快速配置预设**：支持 Conservative（谨慎）、Standard（标准）、Full（全量）、PlaybackOnly（仅播放）四种预设模板，选择后保存即自动设置所有事件开关，随后自动重置为 None。
- **可配置重试次数**：新增「最大重试次数」配置项（0～3），替代原来固定 1 次重试。

### 优化
- **指数退避重试**：发送失败后重试延迟从固定 1 秒改为指数递增（1s → 2s → 4s），减少对飞书 API 的压力。
- **优先级队列**：安全事件（Security/Error 级别）进入高优先级队列优先处理，确保安全告警不被普通通知阻塞。
- **配置描述增强**：所有 8 个配置分组新增 `EditorDescription` 引导文案，关键字段补充详细使用说明，降低首次配置门槛。

### 配置兼容
- 新增 4 个扁平字段（`MaxRetryCount`、`ApplyPreset`、`DiagnosticInfo`、`WebhookHealthStatus`）与分组对象双向同步。
- `ConfigMigrator` 新增 `MaxRetryCount` 的 ClampRanges 校验。
- 旧配置文件无需手动修改即可升级。

---

## 1.4.1.0 — 架构优化、重复代码消除与工程化

在 1.4.0.0 基础上进行全面代码审计后的优化重构。不新增业务功能，不改变既有行为，向后兼容。

### 优化
- **消除配置同步重复代码**：新增 `ConfigSynchronizer`，`SyncToGroups`/`SyncFromGroups`/`ConfigMigrator.MigrateToGroups` 三处约 300 行重复代码合并为单一来源。新增配置字段时只需修改 `ConfigSynchronizer` 一处。
- **正则表达式缓存**：`SensitiveDataSanitizer` 中的路径脱敏正则改为 `static readonly` 编译缓存，避免每次调用重新创建。
- **许可证统一**：README 许可声明从「个人学习使用」修正为与 LICENSE 文件一致的 MIT 许可。

### 工程化
- **GitHub Actions CI**：新增 `.github/workflows/build.yml`，自动化 Restore → Build → SelfTest 流程。
- **配置同步完整性测试**：新增反射验证测试，自动检测 92 个扁平字段的双向同步完整性，防止新增字段遗漏。

### 测试
- 自测 **237 项全部通过**（新增 2 项），Debug/Release 均 0 警告 0 错误。

---

## 1.4.0.0 — 配置分组、侧边栏入口、飞书安全校验、发布前优化

本版本在 1.3.0.0 事件体系之上加入配置分组与飞书安全校验，并完成一次发布前的代码优化、目录清理与文档重构。未新增业务功能，未改变既有行为，向后兼容。

### 新增
- **控制台侧边栏入口**：在 Emby Server 管理控制台侧边栏显示「飞书通知」菜单项，点击直接打开配置页面（`EnableInMainMenu=true`、`EnableInUserMenu=false`、`IsMainConfigPage=true`）。
- **配置分组**：约 60 个配置项组织为 8 个功能分组（飞书连接、机器人安全校验、消息显示、播放通知、登录与用户、媒体库与用户行为、任务/Live TV/服务器、高级与诊断），每组继承 `EditableOptionsBase` 提供独立中文标题与说明。旧扁平字段保留并标记 `Browsable(false)` 以保持兼容。
- **自定义关键词**：支持飞书自定义机器人「自定义关键词」安全设置。开启后所有消息正文（文本/卡片/回退文本）自动追加关键词，已含时不重复。
- **签名校验**：支持飞书自定义机器人「签名校验」。按官方算法实现 HMAC-SHA256，每次发送/重试/回退都重新生成时间戳与签名。密钥使用 `[IsPassword]` 保护，保存时不回显，日志不输出完整密钥与 sign。
- **消息安全装饰器**：`IFeishuMessageSecurityDecorator` / `FeishuMessageSecurityDecorator` 统一处理关键词注入与签名添加，与事件格式化器解耦。
- **可测试时间源**：`IUnixTimeProvider` / `SystemUnixTimeProvider` 支持测试固定时间戳。
- 文档：新增 `docs/FEISHU-SECURITY.md`、`docs/ARCHITECTURE.md`，并在配置页展示飞书机器人使用指南。

### 优化（发布前维护）
- **测试结果持久化改用官方 API**：`Plugin.PersistTestResult` 由反射查找 `SaveConfiguration`（该方法在 4.9.5.0 基类中并不存在，导致结果无法写回）改为直接调用官方 `protected SaveOptions(TOptions)`，并加入 `_isPersistingTestResult` 防重入保护，测试结果现在能可靠回写。
- **测试推送同步阻塞加固与注释**：`OnOptionsSaved` 是框架同步回调，无法 `await`、不改 `async void`；保留 `GetAwaiter().GetResult()`，补充说明并确认阻塞时间由已夹取到 3~60 秒的 `RequestTimeoutSeconds` 严格限定。
- **版本集中管理**：新增 `Directory.Build.props` 的 `EmbyFeishuVersion` 属性作为唯一版本来源，`csproj` 的 `Version`/`AssemblyVersion`/`FileVersion`/`InformationalVersion` 全部引用它。
- **清理**：删除死方法 `PluginOptions.HandleTestPush()`、开发期探针 `tools/EmbyApiProbe/`、误放的 `lib/emby/4.9.5.0/EmbyFeishu.png`、过时阶段报告 `docs/PROJECT-AUDIT-REPORT.md`；`docs/DEVELOPMENT.md` 内容并入 `docs/ARCHITECTURE.md`。
- **`.gitignore` 全面更新**：覆盖构建产物、IDE、测试覆盖、日志临时文件、调试符号与运行时元数据、敏感配置；`release/` 仅保留 `.gitkeep`。

### 配置兼容
- 插件 GUID、配置文件名、Webhook、事件开关、消息格式、过滤与限流参数均保持原值。
- 旧配置自动迁移（`ConfigSchemaVersion` v2），旧扁平字段保留并同步到新分组；保存时双向同步（分组→扁平）以支持降级。

### 测试
- 自测 **235 项全部通过**，Debug/Release 均 0 警告 0 错误，Release 产物仅 `EmbyFeishu.dll`。

### 已知限制
- 分组嵌套对象的标题/说明展示方式取决于 Emby 4.9.5.0 GenericEdit 渲染。
- 签名仅加入 `timestamp` 与 `sign`，不修改飞书消息的 `msg_type` 与 `content` 结构。
- 服务器停止通知为尽力而为（2 秒短超时），强杀/断电无法保证送达。

---

## 1.3.0.0 — 事件类型、消息格式与详细程度大幅扩展

在不破坏既有播放通知、旧配置与插件 GUID 的前提下，扩展了 Emby 事件覆盖面、消息表现力与安全性。

### 新增
- **统一事件模型** `NotificationEvent`（分类/严重程度/字段/去重键），事件回调即时转换为不可变快照后入队，绝不把 Emby 原始对象放入队列。
- **事件源架构**：按职责拆分为 8 个 `IEventSource`（播放/会话/用户/媒体库/用户数据/任务/Live TV/服务器），`EntryPoint` 改为组装容器；可选源失败不影响整体。
- **播放增强**：播放完成、中途放弃、播放方式变化、进度里程碑；停止分类互斥。
- **登录与会话/用户管理/媒体库/用户行为/计划任务/Live TV/服务器** 全量事件覆盖。
- **消息格式**：新增飞书交互卡片（按严重程度配色、双列字段、页脚），卡片失败可回退文本一次。
- **详细程度**：Simple / Standard / Detailed / Custom（默认 Custom，播放沿用旧字段开关，外观不变）。
- **去重/限流/聚合**：`NotificationPolicy` + 媒体库聚合器。
- **敏感信息脱敏器** `SensitiveDataSanitizer`：Webhook / IP / 设备 ID / 异常 / 绝对路径。
- **插件图标**、**配置迁移** `ConfigMigrator`。

### 安全
- 绝不读取/记录/推送 `AuthenticationRequest.Password` 与 `AuthenticationResult.AccessToken`。
- 任务失败仅用简短 `ErrorMessage` 并脱敏路径。日志与消息中的 Webhook 全程脱敏。

### 兼容
- 插件 GUID、配置文件名、Webhook 不变；旧配置可继续加载；新字段安全默认。

---

## 1.2.0.0 — 安全加固
- 关闭 Emby HttpClient 默认日志避免 Webhook 泄露；异常消息脱敏；重试判定改用 `HttpException`；统一会话键；非飞书域名改为非阻断。

## 1.1.0.0 / 1.0.0.0 — 初始版本
- 播放开始/停止/暂停/恢复推送、用户过滤、暂停恢复去重、Simple UI 配置、Webhook 脱敏。
