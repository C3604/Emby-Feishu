# Emby 飞书通知插件 (EmbyFeishu)

将 Emby Server 的播放事件（开始、停止、暂停、恢复）实时推送到飞书群机器人。

## 功能特性

- **播放**：开始 / 停止 / 暂停 / 恢复 / 完成 / 中途放弃 / 播放方式变化 / 进度里程碑
- **登录与会话**：登录成功 / 失败、会话开始 / 结束、远程控制断开、同步播放加入 / 离开
- **用户管理**：锁定、改密、创建 / 删除 / 更新、策略 / 配置更新
- **媒体库**：新增 / 更新 / 删除，扫描时窗口聚合防消息风暴，按真实类型过滤非媒体项
- **用户行为**：收藏、标记已看 / 未看、评分变化（仅真正变化才推）
- **计划任务**：失败 / 完成 / 取消、库扫描、元数据刷新、备份（错误摘要脱敏）
- **Live TV（可选）** 与 **服务器**：启动 / 更新 / 需要重启 / 维护模式
- **两种消息格式**：纯文本与飞书交互卡片（按严重程度配色，失败可回退文本）
- **四档详细程度**：Simple / Standard / Detailed / Custom（默认 Custom，保持旧版外观）
- 电影、剧集标题智能格式化（自动识别 S01E02）；用户过滤（全部 / 仅包含 / 排除）
- 去重 + 每分钟限流 + 聚合，安全事件可豁免限流
- 异步后台发送，不影响 Emby 播放；网络异常不影响服务器
- 全面脱敏：Webhook / IP / 设备 ID / 异常路径；绝不记录密码或 AccessToken
- 所有配置通过 Emby 后台 Simple UI 自动生成，无需手写 HTML

## 环境要求

- Emby Server 4.9.5.0
- .NET SDK 8.0（仅编译时需要）
- 飞书群机器人 Webhook 地址

## 快速开始

> 不想自己编译的话，可直接前往 [GitHub Releases](../../releases) 下载已经编译好的插件文件。

1. 编译插件：

```bash
dotnet build src/EmbyFeishu/EmbyFeishu.csproj -c Release
```

2. 将 `src/EmbyFeishu/bin/Release/EmbyFeishu.dll` 复制到 Emby 的插件目录
3. 重启 Emby Server
4. 在 Emby 后台 → 插件 → Emby 飞书通知 中配置 Webhook 地址
5. 启用插件并保存

详细步骤请参考 [部署文档](docs/DEPLOYMENT.md)。

## 项目结构

```
EmbyFeishu/
├── src/EmbyFeishu/          # 插件源码
│   ├── Plugin.cs            # 插件主类
│   ├── PluginOptions.cs     # 配置选项
│   ├── EntryPoint.cs        # 入口点（事件订阅）
│   ├── Configuration/       # 配置校验
│   ├── Events/              # 事件处理、状态跟踪、通知队列
│   ├── Feishu/              # 飞书 Webhook 客户端
│   ├── Infrastructure/      # 工具类（脱敏、过滤、格式化）
│   ├── Messaging/           # 消息格式化
│   └── Models/              # 数据模型
├── tools/EmbyFeishu.SelfTest/ # 自测工具
├── lib/emby/4.9.5.0/        # Emby 引用 DLL（不提交到 Git）
└── docs/                    # 文档
```

## 文档

- [事件目录](docs/EVENT-CATALOG.md)
- [消息格式](docs/MESSAGE-FORMATS.md)
- [配置说明](docs/CONFIGURATION.md)
- [更新日志](docs/CHANGELOG.md)
- [开发文档](docs/DEVELOPMENT.md)
- [编译文档](docs/BUILD.md)
- [部署文档](docs/DEPLOYMENT.md)
- [测试文档](docs/TESTING.md)
- [故障排查](docs/TROUBLESHOOTING.md)
- [项目审查报告](docs/PROJECT-AUDIT-REPORT.md)
- [真实环境验证清单](docs/REAL-ENVIRONMENT-VERIFICATION.md)

## 许可

本项目仅供个人学习和使用。
