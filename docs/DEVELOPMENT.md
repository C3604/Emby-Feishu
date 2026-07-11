# 开发文档

## 架构概览

```
Plugin.cs                    插件主类（BasePluginSimpleUI<PluginOptions>）
PluginOptions.cs             配置选项（EditableOptionsBase）
EntryPoint.cs                入口点（IServerEntryPoint）

Configuration/
  ConfigValidator.cs         配置校验逻辑

Events/
  PlaybackStateTracker.cs    播放状态跟踪（暂停/恢复去重）
  NotificationDispatcher.cs  后台通知队列调度器
  INotificationDispatcher.cs 调度器接口

Feishu/
  IFeishuWebhookClient.cs    飞书客户端接口
  FeishuWebhookClient.cs     飞书 Webhook 客户端实现
  FeishuWebhookRequest.cs    飞书请求体模型
  FeishuWebhookResponse.cs   飞书响应体模型
  WebhookSendResult.cs       发送结果模型

Infrastructure/
  WebhookMasker.cs           Webhook 地址脱敏
  UserFilter.cs              用户过滤
  TimeFormatter.cs           时间格式化
  MediaTitleFormatter.cs     媒体标题格式化

Messaging/
  INotificationFormatter.cs  消息格式化接口
  FeishuTextNotificationFormatter.cs  飞书文本消息格式化器

Models/
  PlaybackEventType.cs       播放事件类型枚举
  PlaybackNotificationEvent.cs  播放通知事件数据模型
  PlaybackSessionState.cs    播放会话状态
  UserFilterMode.cs          用户过滤模式枚举
```

## 数据流

1. Emby 触发播放事件 → EntryPoint 事件处理器
2. 事件处理器提取数据 → 创建 PlaybackNotificationEvent
3. 检查插件启用、用户过滤、视频过滤
4. 事件入队 → NotificationDispatcher 后台队列
5. 后台循环取出事件 → FeishuTextNotificationFormatter 格式化
6. FeishuWebhookClient 发送到飞书 → 处理结果

## 使用的 Emby 4.9.5.0 接口

- `MediaBrowser.Controller.Plugins.BasePluginSimpleUI<T>` — 插件基类
- `Emby.Web.GenericEdit.EditableOptionsBase` — 配置选项基类
- `MediaBrowser.Controller.Plugins.IServerEntryPoint` — 入口点接口
- `MediaBrowser.Controller.Session.ISessionManager` — 会话管理（事件订阅）
- `MediaBrowser.Common.Net.IHttpClient` — HTTP 请求
- `MediaBrowser.Model.Serialization.IJsonSerializer` — JSON 序列化
- `MediaBrowser.Model.Logging.ILogManager` / `ILogger` — 日志
- `MediaBrowser.Controller.Library.PlaybackProgressEventArgs` — 播放进度事件
- `MediaBrowser.Controller.Library.PlaybackStopEventArgs` — 播放停止事件
- `MediaBrowser.Model.Dto.BaseItemDto` — 媒体信息

## 关键设计决策

1. **netstandard2.0 + System.Memory NuGet**：Emby 4.9.5.0 的 IJsonSerializer 使用了 ReadOnlyMemory<char>，需要 System.Memory 编译时引用。运行时由 Emby Server 提供，不需要额外部署。

2. **配置校验通过 EditableObjectBase.Validate(ValidationContext)**：Emby Simple UI 框架在保存配置前自动调用此方法。

3. **GetOptions() 是 protected**：通过 Plugin.GetPluginOptions() 公开包装方法提供外部访问。

4. **异步通知队列**：事件回调只做数据提取和入队，不做网络请求。后台单线程顺序处理队列。
