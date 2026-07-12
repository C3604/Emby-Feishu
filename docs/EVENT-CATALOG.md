# 事件目录（EVENT-CATALOG）

本插件（v1.4.0.0）支持的全部事件。所有事件均已对本地 Emby Server **4.9.5.0** DLL 反射核验存在。

- **内部 EventType**：插件统一事件模型 `NotificationEvent.EventType`
- **Emby 原始事件**：订阅的 Emby 接口事件
- **衍生**：是否由其他事件派生（非直接一对一）
- **默认**：对应配置项的默认开关
- **去重/聚合**：抑制策略

> 严重程度（Severity）决定飞书卡片配色：Information=蓝、Success=绿、Warning=橙、Error/Security=红。

## 一、播放（Playback）— 来源 `ISessionManager`

| 中文名 | 内部 EventType | Emby 原始事件 | 衍生 | 默认 | 去重/聚合 | 备注 |
| --- | --- | --- | --- | --- | --- | --- |
| 开始播放 | PlaybackStarted | PlaybackStart | 否 | 开 | 每会话一次 | 保持旧版行为 |
| 暂停播放 | PlaybackPaused | PlaybackProgress | 是 | 关 | 状态跟踪去重 | 高频进度只推一次 |
| 恢复播放 | PlaybackResumed | PlaybackProgress | 是 | 关 | 状态跟踪去重 | |
| 停止播放 | PlaybackStopped | PlaybackStopped | 否 | 开 | 与完成/放弃互斥 | 未完成且非早退 |
| 播放完成 | PlaybackCompleted | PlaybackStopped | 是 | 开 | 与停止互斥 | PlayedToCompletion 或进度≥完成阈值 |
| 放弃播放 | PlaybackAbandoned | PlaybackStopped | 是 | 关 | 与停止/完成互斥 | 进度<25% 的早退 |
| 播放方式变化 | PlaybackMethodChanged | PlaybackProgress | 是 | 关 | 首次建立基线不推，仅真正改变时推 | 直放/直接串流/转码 |
| 播放进度里程碑 | PlaybackMilestone | PlaybackProgress | 是 | 关 | 每会话每阈值一次，快进只推最高 | 阈值可配 25,50,75 |

**播放停止分类规则**：完成→PlaybackCompleted；未完成早退(<25%)→PlaybackAbandoned；其余→PlaybackStopped。同一次停止仅产生一条外部通知，已发送完成不再发送普通停止。不足“最短播放秒数”直接忽略。

**可用字段**：用户、媒体（含季集号）、类型、播放方式、客户端、设备、播放位置/完成进度、媒体时长、年份（Detailed）、视频/音频编码、声道、分辨率、客户端版本、脱敏 IP（Detailed+敏感技术细节）。

**已知限制**：播放方式依赖 `SessionInfo.PlayState.PlayMethod`，个别客户端可能不上报，此时不猜测、不通知。

## 二、登录与会话（Authentication / Session）— 来源 `ISessionManager`

| 中文名 | 内部 EventType | Emby 原始事件 | 默认 | 严重程度 | 去重 |
| --- | --- | --- | --- | --- | --- |
| 登录成功 | AuthenticationSucceeded | AuthenticationSucceeded | 关 | Info | — |
| 登录失败 | AuthenticationFailed | AuthenticationFailed | 开 | Security | 用户+设备+IP 30 秒去重 |
| 会话开始 | SessionStarted | SessionStarted | 关 | Info | — |
| 会话结束 | SessionEnded | SessionEnded | 关 | Info | — |
| 远程控制断开 | RemoteControlDisconnected | RemoteControlDisconnected | 关 | Info | — |
| 加入同步播放 | PartyJoined | AddedToParty | 关 | Info | — |
| 离开同步播放 | PartyLeft | RemovedFromParty | 关 | Info | — |

**安全约束**：登录失败绝不读取/记录/推送 `AuthenticationRequest.Password`；登录成功绝不读取/记录/推送 `AuthenticationResult.AccessToken`。IP 与设备 ID 默认脱敏。

**登录失败可用字段**：用户、客户端、客户端版本、设备、设备 ID（脱敏）、协议、远程地址（脱敏）、时间。

**已知限制**：登录失败采用 30 秒去重防风暴 + 安全事件豁免普通限流；“最近 N 分钟失败 N 次”式细粒度计数聚合为简化实现（去重已足以防止无限通知）。

## 三、用户管理（UserManagement）— 来源 `IUserManager`

| 中文名 | 内部 EventType | Emby 原始事件 | 默认 | 严重程度 | 去重 |
| --- | --- | --- | --- | --- | --- |
| 用户被锁定 | UserLockedOut | UserLockedOut | 开 | Security | 10 秒 |
| 修改密码 | UserPasswordChanged | UserPasswordChanged | 开 | Warning | — |
| 创建用户 | UserCreated | UserCreated | 关 | Info | — |
| 删除用户 | UserDeleted | UserDeleted | 关 | Warning | — |
| 更新用户 | UserUpdated | UserUpdated | 关 | Info | 同用户 2 秒 |
| 更新用户策略 | UserPolicyUpdated | UserPolicyUpdated | 关 | Info | 同用户 2 秒 |
| 更新用户配置 | UserConfigurationUpdated | UserConfigurationUpdated | 关 | Info | 同用户 2 秒 |

**说明**：修改密码只提示发生变化，不含任何密码值。更新/策略/配置共享同用户 2 秒去重键，短时间内多个相关事件只推一次。

## 四、媒体库（Library）— 来源 `ILibraryManager`

| 中文名 | 内部 EventType | Emby 原始事件 | 默认 | 聚合 |
| --- | --- | --- | --- | --- |
| 新增电影/剧集/音乐/其他 | ItemAdded | ItemAdded | 关 | 窗口聚合 |
| 项目更新 | ItemUpdated | ItemUpdated | 关 | 窗口聚合 |
| 项目删除 | ItemRemoved | ItemRemoved | 关 | 窗口聚合 |
| 媒体库汇总 | LibraryAggregated | （聚合派生） | — | — |

**类型过滤**：基于 Emby 真实类型 `BaseItem.GetClientTypeName()` 分类，过滤 Folder/CollectionFolder/Person/Genre/Studio/Year/Chapter 以及虚拟项、占位项、主题视频。允许 Movie/Episode/Series/Audio/MusicAlbum；MusicArtist、Trailer、BoxSet 默认不推送。**不使用 ItemAdding。**

**聚合规则**：窗口（默认 60 秒，10~600）内统计新增/更新/删除并按电影/剧集/音乐/其他分类；新增数 ≤「逐条推送上限」（默认 5）且无更新/删除时逐条推送，否则合并为一条汇总（列出前 5 个名称+剩余数量）。同一 ItemId+操作在窗口内去重。插件停止时安全刷新未发送数据。

## 五、用户行为（UserActivity）— 来源 `IUserDataManager.UserDataSaved`

| 中文名 | 内部 EventType | 触发条件 | 默认 |
| --- | --- | --- | --- |
| 添加收藏 | FavoriteAdded | IsFavorite false→true | 关 |
| 取消收藏 | FavoriteRemoved | IsFavorite true→false | 关 |
| 标记已看 | MarkedPlayed | Played false→true（非播放完成触发） | 关 |
| 标记未看 | MarkedUnplayed | Played true→false | 关 |
| 评分变化 | UserRatingChanged | Rating 改变 | 关 |

**去噪**：缓存每个(用户,项目)的旧 IsFavorite/Played/Rating，仅真正变化才通知；普通播放进度保存（PlaybackProgress/Start/Import/HideFromResume）只更新缓存不通知；`PlaybackFinished` 触发的已看不重复通知（由播放完成事件覆盖）。缓存有界（5000）并按 24 小时过期清理。

## 六、计划任务（ScheduledTask）— 来源 `ITaskManager`

| 中文名 | 内部 EventType | 条件 | 默认 |
| --- | --- | --- | --- |
| 任务失败 | TaskFailed | 状态 Failed/Aborted | 开 |
| 任务完成 | TaskCompleted | 状态 Completed（非扫描/元数据/备份） | 关 |
| 任务取消 | TaskCancelled | 状态 Cancelled | 关 |
| 媒体库扫描开始 | LibraryScanStarted | TaskExecuting 且识别为扫描 | 关 |
| 媒体库扫描完成 | LibraryScanCompleted | Completed 且识别为扫描 | 开 |
| 元数据刷新完成 | MetadataRefreshCompleted | Completed 且识别为元数据 | 关 |
| 备份完成 | BackupCompleted | Completed 且识别为备份 | 关 |

**分类**：优先按 `TaskResult.Key` / `IScheduledTaskWorker.Category` 稳定标识（如 `RefreshLibrary`），名称仅作降级判断。**失败摘要**仅使用简短 `ErrorMessage`（非堆栈 LongErrorMessage），并经脱敏去除绝对路径，绝不发送完整堆栈、Token 或敏感配置。不发送实时进度。

## 七、Live TV（可选）— 来源 `ILiveTvManager`

| 中文名 | 内部 EventType | 默认 |
| --- | --- | --- |
| 开始/结束录制 | RecordingStarted / RecordingEnded | 关 |
| 创建/更新/取消录制定时 | TimerCreated / TimerUpdated / TimerCancelled | 关 |
| 创建/更新/取消连续录制 | SeriesTimerCreated / SeriesTimerUpdated / SeriesTimerCancelled | 关 |

**总开关**：`启用 Live TV 通知`（默认关）。若服务器未启用 Live TV 或无法注入 `ILiveTvManager`，该事件源不加载，不影响插件其他功能。

## 八、服务器（Server）— 来源 `IServerApplicationHost`

| 中文名 | 内部 EventType | Emby 原始事件 | 默认 |
| --- | --- | --- | --- |
| 服务器已启动 | ServerStarted | （初始化完成后主动发送） | 开 |
| 服务器正在停止 | ServerStopping | （Dispose 尽力而为） | 关 |
| 有可用更新 | UpdateAvailable | HasUpdateAvailableChanged（变为 true 时） | 开 |
| 已应用更新 | ApplicationUpdated | ApplicationUpdated | 开 |
| 需要重启 | RestartRequired | HasPendingRestartChanged（变为 true 时） | 开 |
| 进入/退出维护模式 | MaintenanceModeEntered / MaintenanceModeExited | EnterMaintenanceMode / ExitMaintenanceMode | 关 |

**说明**：`HasPendingRestartChanged` / `HasUpdateAvailableChanged` 只在状态变为 true 时发送。服务器启动通知在事件订阅与后台队列就绪后发送。服务器停止通知为尽力而为（短超时 2 秒，不阻塞关闭），强杀/断电/容器被杀无法保证送达。

## API 差异说明

- `IApplicationHost.HasUpdateAvailableChanged` 在 4.9.5.0 **不存在**；该事件改用其派生接口 `IServerApplicationHost.HasUpdateAvailableChanged`（存在）。
- 其余 Prompt 中列出的会话、用户、媒体库、用户数据、任务、Live TV、服务器事件在本地 4.9.5.0 DLL **全部存在**。
