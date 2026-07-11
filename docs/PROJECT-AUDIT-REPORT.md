# EmbyFeishu 项目审查报告

审查日期：2026-07-12
审查对象：EmbyFeishu 插件（当前工作目录完整代码）
审查基准：Emby Server 4.9.5.0 / .NET SDK 8.0.422 / netstandard2.0

---

## 1. 审查范围

对 EmbyFeishu 插件进行了完整的工程审查、兼容性审查和安全审查，覆盖：

- 项目结构与依赖
- 实际构建（Debug / Release）与编译警告
- Emby 4.9.5.0 插件兼容性（主类、配置页、生命周期）
- 播放事件监听（开始 / 进度 / 停止）与暂停恢复去重
- 后台队列与异步任务
- 飞书 Webhook 客户端（请求、响应、超时、重试、资源释放）
- 安全与隐私（Webhook 泄露）
- 业务配置项是否真实生效
- 消息内容格式化
- 测试覆盖
- 部署产物
- 文档一致性

所有结论均基于实际代码、实际 DLL（版本已核验）和实际构建/测试结果。

## 2. 项目当前架构

```
EmbyFeishu.sln
├─ src/EmbyFeishu/ (netstandard2.0, 插件本体, 产物 EmbyFeishu.dll)
│  ├─ Plugin.cs                  插件主类 BasePluginSimpleUI<PluginOptions>
│  ├─ PluginOptions.cs           配置类 EditableOptionsBase + Validate
│  ├─ EntryPoint.cs              IServerEntryPoint 事件订阅与生命周期
│  ├─ Configuration/ConfigValidator.cs
│  ├─ Events/                    通知队列 + 播放状态去重
│  ├─ Feishu/                    Webhook 客户端与请求/响应模型
│  ├─ Infrastructure/            脱敏 / 过滤 / 时间 / 标题格式化
│  ├─ Messaging/                 飞书文本消息格式化
│  └─ Models/                    不可变事件数据模型
├─ tools/EmbyFeishu.SelfTest/ (net8.0, 独立控制台自测, 不部署)
├─ lib/emby/4.9.5.0/          本地 Emby 引用 DLL（不提交、不部署）
└─ docs/                       文档
```

数据流：Emby 事件回调 → 立即转换为不可变事件模型 → 入队 → 后台单线程循环 → 格式化 → 飞书 Webhook。事件回调线程不做网络请求。

## 3. 环境与依赖

| 项目 | 结论 |
| --- | --- |
| 插件目标框架 | `netstandard2.0` ✓ 符合要求 |
| 自测工具框架 | `net8.0`（独立控制台，不部署）✓ |
| Emby DLL 版本 | 4 个 DLL 均为 AssemblyVersion/FileVersion `4.9.5.0` ✓ 与服务器一致 |
| Emby DLL 引用 | 全部 `<Private>false</Private>`，不复制到输出 ✓ |
| NuGet 依赖 | 仅 `System.Memory 4.6.0`（`PrivateAssets=all`），编译期必需，运行期由 Emby 提供 ✓ |
| Jellyfin 引用 | 无 ✓ |
| Newtonsoft / System.Text.Json | 无（使用 Emby IJsonSerializer）✓ |
| ASP.NET Core / 数据库 / 第三方队列 | 无 ✓ |

**System.Memory 说明**：Emby 4.9.5.0 的 `IJsonSerializer.SerializeToString` 返回 `ReadOnlyMemory<char>`，`HttpRequestOptions.RequestContent` 也是 `ReadOnlyMemory<char>`，因此 netstandard2.0 下必须引用 System.Memory 才能编译。该程序集在 Emby Server 运行环境中已存在，无需随插件部署（已设 `PrivateAssets=all`，Release 输出目录中确认不含该 DLL）。

## 4. 构建结果

| 配置 | 结果 |
| --- | --- |
| `dotnet build -c Debug` | 成功，0 警告 0 错误 |
| `dotnet build -c Release` | 成功，0 警告 0 错误 |

Release 输出目录 `src/EmbyFeishu/bin/Release/` 仅包含：

- `EmbyFeishu.dll`（部署）
- `EmbyFeishu.deps.json`（不部署）
- `EmbyFeishu.pdb`（不部署，调试符号）

未包含任何 Emby 自带 DLL、System.Memory.dll 或测试工具。✓

## 5. 测试结果

自测工具 `EmbyFeishu.SelfTest`：**通过 74，失败 0，跳过 0**。

新增覆盖（本次审查补充）：异常消息脱敏、裸 Token 脱敏、飞书/Lark 域名识别、非飞书域名不阻断、多会话状态隔离、会话键构建规则。

## 6. Emby 兼容性结论

- 主类继承 `BasePluginSimpleUI<PluginOptions>`，构造函数注入 `IApplicationHost` + `ILogManager`，与 4.9.5.0 实际 DLL 一致。✓
- GUID 固定 `d3a7f1b2-8c4e-4f5a-9b6d-2e1c0a3f5d7b`，升级不变。✓
- 配置类继承 `EditableOptionsBase`，`Validate(ValidationContext)` 签名与实际 DLL 一致，所有配置项均为 bool/int/enum/string 可序列化类型。✓
- 配置页由 Simple UI 自动生成，无手写 HTML。✓
- 旧配置缺字段时，属性默认值可正常反序列化。✓

## 7. 生命周期审查

- `Run()` 中订阅三个事件；`Dispose()` 中对称解除三个事件，并释放 Timer 和 Dispatcher。✓
- Dispatcher 释放 SemaphoreSlim、CancellationTokenSource、后台任务。✓
- Dispose 为有界等待（后台任务最多等 5 秒），不会无限死锁。✓
- **本次加固**：ProcessLoop 增加对 `ObjectDisposedException` 的捕获，避免关闭竞态下（在途请求超过 5 秒停止等待）访问已释放信号量产生未观察异常。

## 8. 并发与异步审查

- 事件回调仅做数据提取与入队，**不含** `.Result` / `.Wait()` / `GetResult()` / `Thread.Sleep()`。✓
- 播放状态使用 `ConcurrentDictionary`，队列使用 `ConcurrentQueue` + `SemaphoreSlim`。✓
- 后台单循环顺序发送；单条消息异常被 try/catch 包裹，不会终止消费者。✓
- 队列上限 200，满时丢弃最旧并记录 Warning。✓
- 存在两处同步等待，均已评估为**可接受**（详见问题清单 P3-2、P3-3），不在播放线程上。

## 9. 飞书客户端审查

- 请求体经 `IJsonSerializer` 序列化，非字符串拼接。✓
- `Content-Type: application/json`（已按飞书文档修正，早期的 `application/json; charset=utf-8` 会被 Emby 拒绝）。✓
- 响应处理：HTTP 200 + `code!=0` 判为业务失败；空响应容错；解析异常不崩溃后台循环。✓
- 超时来自配置；网络/超时/5xx/429 最多重试 1 次，间隔 1 秒；4xx 不重试。✓（本次改为基于 `HttpException.StatusCode` / `IsTimedOut` 判定，替代原字符串匹配）
- Response、Stream、StreamReader 均 `using` 释放。✓
- 重试响应取消信号（CancellationToken）。✓

## 10. 配置审查

逐项确认下列配置均被代码实际使用且生效：Enabled、WebhookUrl、OnlyVideo、RequestTimeoutSeconds、MinimumStopSeconds、NotifyPlaybackStarted/Stopped/Paused/Resumed、IncludeUserName/MediaTitle/MediaType/SeriesEpisode/ClientName/DeviceName/PlaybackPosition/PlayedToCompletion、UserFilterMode、UserNames、SendTestNotification、LastTestResult。✓

配置在运行中修改后即时生效：后台发送时每次都通过 `Plugin.Instance.GetPluginOptions()` 重新读取最新配置，不缓存旧值。✓

## 11. 安全审查

- Webhook 全程脱敏：日志中仅出现 `域名/****末四位`。✓
- **本次修复两处真实泄露风险**（见 P1-1、P1-2）：
  1. Emby `HttpClient` 默认会把完整请求 URL 写入 Emby 日志（`LogRequest`/`LogResponse` 默认 `true`）。已显式关闭并禁用错误日志。
  2. 异常消息可能包含完整 Webhook；测试推送时该消息会写入 `LastTestResult`（持久化到配置文件并显示在界面）。已加脱敏处理。
- 无硬编码真实 Webhook / Token（已全库搜索确认）。✓
- `.gitignore` 覆盖 bin/obj/lib、构建产物、IDE 文件。✓
- 配置校验错误信息不回显完整 Webhook。✓

## 12. 部署产物审查

见第 4 节。需上传 `EmbyFeishu.dll`；`deps.json`、`pdb` 及任何 Emby 自带 DLL 均不上传。GUID 固定，升级保留配置。

## 13. 问题清单

| 编号 | 级别 | 位置 | 问题 | 影响 | 状态 |
| --- | --- | --- | --- | --- | --- |
| P1-1 | P1 | FeishuWebhookClient.cs / Plugin.cs（HttpRequestOptions） | Emby HttpClient 默认 `LogRequest=true`、`LogResponse=true`，插件仅关闭了 `LogErrors`，导致完整 Webhook URL（含 Token）被写入 Emby 自身日志 | Webhook 明文泄露到 Emby 日志，脱敏形同虚设 | **已修复** |
| P1-2 | P1 | Plugin.cs:180-185；FeishuWebhookClient.cs:108-114 | 异常消息（可能含完整 URL）直接写入日志，且测试推送时写入持久化的 `LastTestResult` 并显示在配置页 | Webhook 可能通过异常文本泄露到日志/界面/配置文件 | **已修复** |
| P2-1 | P2 | FeishuWebhookClient.cs:110 | 通过 `ex.Message.IndexOf("timeout")` 字符串匹配判断是否重试，脆弱且可能误判 | 重试策略不可靠 | **已修复**（改用 `HttpException.IsTimedOut` / `StatusCode`） |
| P2-2 | P2 | EntryPoint.cs（三处会话键） | 开始事件用 `evt.DeviceName`（含 session 回退），进度/停止事件用 `e.DeviceName`（不回退），键构建来源不一致 | 在 PlaySessionId 缺失且 DeviceName 仅在 session 上时，键不匹配，可能导致暂停/恢复去重或停止清理失效 | **已修复**（统一 `BuildSessionKey`） |
| P3-1 | P3 | ConfigValidator.cs:56-64 | 非飞书域名被当作**阻断性**校验错误，而提示语却说"请忽略此警告重新保存"（实际无法忽略），与"不强制锁死飞书域名"要求矛盾 | 使用自定义中转域名的用户永远无法保存配置 | **已修复**（改为非阻断，仅记录日志警告） |
| P3-2 | P3 | Plugin.cs:147 | 测试推送使用 `GetAwaiter().GetResult()` 同步阻塞 | 阻塞的是"保存配置"的管理线程（用户主动点击并等待结果），**不在播放线程**，最多阻塞 timeout 秒 | 可接受（保留） |
| P3-3 | P3 | NotificationDispatcher.cs:70 | Stop() 中 `_processingTask.Wait(5s)` 同步等待 | 仅在插件卸载/关闭时执行，有界 5 秒，不在播放线程 | 可接受（保留） |
| P3-4 | P3 | NotificationDispatcher ProcessLoop | 关闭竞态下若在途请求超过 5 秒停止等待，随后可能访问已释放的信号量 | 产生一次被吞掉的未观察异常，不影响 Emby | **已加固**（捕获 ObjectDisposedException） |

## 14. 已修复问题

- P1-1、P1-2、P2-1、P2-2、P3-1、P3-4 共 6 项，全部通过重新编译（0 警告 0 错误）和自测（74/74 通过）验证。
- 关键改动文件：`Infrastructure/WebhookMasker.cs`（新增 `Sanitize`）、`Feishu/FeishuWebhookClient.cs`、`Plugin.cs`、`EntryPoint.cs`、`Configuration/ConfigValidator.cs`、`PluginOptions.cs`、`Events/NotificationDispatcher.cs`、自测 `Program.cs`。
- 版本号从 1.1.0.0 提升至 **1.2.0.0**。

## 15. 未修复问题及原因

- P3-2、P3-3：两处同步等待均不在播放线程，且分别由用户主动操作和插件关闭触发，属合理设计，改为纯异步会增加复杂度而无实际收益，故保留。

## 16. 真实环境验证事项

以下项目无法在当前开发环境验证，需在真实 Emby Server 上确认，详见 `docs/REAL-ENVIRONMENT-VERIFICATION.md`：

1. 插件能否被 Emby 4.9.5.0 加载并在后台显示。
2. 配置页能否正常渲染、保存、即时生效。
3. 真实播放能否触发开始/停止通知，且各只一次。
4. 暂停/恢复在高频进度事件下是否只推送一次。
5. 多设备并发播放会话是否互不干扰。
6. 填写错误 Webhook 时播放是否不受影响。
7. **Emby 日志中确认不再出现完整 Webhook URL**（本次 P1-1 修复的关键验证点）。
8. 重启 Emby 后配置是否保留。

## 17. 最终是否建议部署

**结论：有条件部署（可以部署，但需完成真实环境验证）。**

代码层面所有 P0/P1/P2 问题均已修复，构建与自测全部通过，安全泄露风险已消除。剩余事项均为需在真实 Emby 环境中观察的运行时行为（见第 16 节），无法在开发机模拟。建议按验证清单在真实服务器上完成一轮验证后正式启用。

## 18. 结论依据

- 无 P0 问题（可正常编译、依赖完整、API 与实际 4.9.5.0 DLL 一致）。
- 全部 P1 安全问题已修复并有测试覆盖。
- Release 产物纯净，仅一个 DLL。
- 未使用 Jellyfin / 4.10 Beta API。
- 事件线程无网络阻塞，飞书异常不影响播放。
