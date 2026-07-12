# 架构说明（ARCHITECTURE）

本文件描述 EmbyFeishu v1.4.0.0 的真实代码结构与消息处理流程。

## 顶层组件

```
Plugin.cs            插件主类（BasePluginSimpleUI<PluginOptions>，IHasThumbImage）
                     负责：侧边栏入口、配置保存回调（OnOptionsSaving/OnOptionsSaved）、测试推送
PluginOptions.cs     配置模型（EditableOptionsBase）：8 个分组对象 + 旧扁平字段 + 双向同步 + 迁移入口
EntryPoint.cs        IServerEntryPoint：组装事件源、启动后台调度器、生命周期与定时清理
```

## 目录结构

```
src/EmbyFeishu/
├── Plugin.cs / PluginOptions.cs / EntryPoint.cs
├── Configuration/
│   ├── ConfigValidator.cs        配置校验（Webhook、关键词、用户名、里程碑）
│   ├── ConfigMigrator.cs         架构迁移（v0/v1 → v2）与数值夹紧
│   └── Groups/                   8 个 EditorGroup 配置分组
├── Events/                       事件源与调度策略
│   ├── IEventSource.cs           事件源统一接口（Start/Stop/Dispose）
│   ├── PlaybackEventSource.cs    播放（含状态机去重、里程碑、播放方式）
│   ├── SessionEventSource.cs     登录/会话/同步播放
│   ├── UserEventSource.cs        用户管理
│   ├── LibraryEventSource.cs     媒体库新增/更新/删除（配合聚合器）
│   ├── UserDataEventSource.cs    收藏/已看/评分（缓存旧状态，仅变化才推）
│   ├── TaskEventSource.cs        计划任务
│   ├── LiveTvEventSource.cs      Live TV（可选）
│   ├── ServerEventSource.cs      服务器状态
│   ├── NotificationContext.cs    发布入口：启用判断 → 策略评估 → 入队
│   ├── NotificationPolicy.cs     去重 + 滑动窗口限流（安全事件可豁免）
│   ├── NotificationDispatcher.cs 后台有界队列（200）+ 单消费循环 + 一次重试 + 卡片回退
│   ├── LibraryAggregator.cs      媒体库窗口聚合，防扫描消息风暴
│   ├── PlaybackStateTracker.cs   播放会话状态（暂停/恢复去重，24h 过期）
│   ├── DeduplicationCache.cs     有界去重缓存
│   └── SlidingWindowRateLimiter.cs 每分钟滑动窗口限流器
├── Feishu/
│   ├── IFeishuWebhookClient.cs / FeishuWebhookClient.cs  Webhook 发送（IHttpClient，关闭日志）
│   ├── FeishuMessageSecurityDecorator.cs  统一注入关键词 + 签名
│   ├── FeishuSignatureProvider.cs         HMAC-SHA256 签名
│   ├── IUnixTimeProvider.cs               可测试时间源
│   ├── FeishuWebhookRequest/Response.cs   请求/响应模型
│   └── WebhookSendResult.cs               发送结果（是否成功/是否可重试）
├── Messaging/
│   ├── INotificationFormatter.cs          格式化器接口
│   ├── FeishuTextNotificationFormatter.cs 文本消息
│   ├── FeishuCardNotificationFormatter.cs 交互卡片
│   └── MessageComposer.cs                 字段组合（按详细程度过滤）
├── Infrastructure/               脱敏、过滤、时间/标题格式化、媒体类型分类
├── Models/                       NotificationEvent、枚举、播放状态
└── logo/EmbyFeishu.png           嵌入资源图标
```

## 消息处理流程

1. **事件回调**：Emby 触发事件（如 `PlaybackStart`），对应 `IEventSource` 的处理器被调用。
2. **提取快照**：处理器只从事件参数提取所需字段，构造不可变 `NotificationEvent`；绝不在回调线程做网络请求，也绝不把 Emby 原始对象放入队列。
3. **发布**：`NotificationContext.Publish(evt)` 依次检查插件启用、用户过滤，再交给 `NotificationPolicy` 做去重与限流评估。
4. **入队**：通过评估的事件进入 `NotificationDispatcher` 的有界队列（上限 200，满则丢弃最旧并告警）。媒体库事件先经 `LibraryAggregator` 聚合。
5. **后台消费**：`NotificationDispatcher` 单个后台循环取出事件，按配置选择文本或卡片格式化器构造消息体。
6. **安全装饰**：`FeishuMessageSecurityDecorator` 注入自定义关键词与签名（`timestamp` + `sign`），每次发送/重试/回退都重新生成签名。
7. **发送**：`FeishuWebhookClient` 经 `IHttpClient` 发送。瞬时错误（429/5xx/网络）最多重试一次；卡片失败且允许时回退文本一次。
8. **清理**：`EntryPoint` 每 5 分钟清理过期播放状态、用户数据缓存、策略缓存，并汇总被限流抑制的数量。

## 生命周期

- **启动**（`EntryPoint.Run`）：先启动后台调度器，再逐个启动事件源（单个可选源失败不影响其他），启动定时清理，发送「服务器已启动」通知。
- **停止**（`EntryPoint.Dispose`）：尽力发送「服务器停止」通知（2 秒短超时），随后对称解除所有事件源订阅并释放调度器、聚合器与定时器。

## 关键设计决策

1. **netstandard2.0 + System.Memory（PrivateAssets=all）**：4.9.5.0 的 `IJsonSerializer` 使用 `ReadOnlyMemory<char>`，编译期需要 `System.Memory`；运行时由 Emby 提供，不随插件部署。
2. **事件回调零阻塞**：所有网络发送在后台队列完成，事件回调只提取数据并入队。
3. **配置双字段架构**：分组对象供 UI 使用，旧扁平字段供序列化兼容与降级；`SyncFromGroups`/`SyncToGroups` 双向同步。
4. **测试结果持久化**：`Plugin.PersistTestResult` 调用官方 `protected SaveOptions(TOptions)`，并以 `_isPersistingTestResult` 防止保存回调重入。

## 使用的 Emby 4.9.5.0 接口（要点）

- `BasePluginSimpleUI<T>`、`EditableOptionsBase`、`IServerEntryPoint`
- `ISessionManager`、`IUserManager`、`ILibraryManager`、`IUserDataManager`、`ITaskManager`、`IServerApplicationHost`、`ILiveTvManager`（可选）
- `IHttpClient`、`IJsonSerializer`、`ILogManager`/`ILogger`

> 完整事件与载荷清单见 [EVENT-CATALOG.md](EVENT-CATALOG.md)。
