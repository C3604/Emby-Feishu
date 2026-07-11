# Emby 飞书通知插件 (EmbyFeishu)

将 Emby Server 的播放事件（开始、停止、暂停、恢复）实时推送到飞书群机器人。

## 功能特性

- 播放开始 / 停止 / 暂停 / 恢复通知
- 电影、剧集标题智能格式化（自动识别 S01E02 格式）
- 用户过滤（全部 / 仅包含 / 排除）
- 暂停和恢复状态去重，不会因高频进度事件重复推送
- 最短播放时长过滤（播放不足 N 秒不发停止通知）
- 仅视频模式（可选忽略音频播放）
- 异步后台发送，不影响 Emby 播放
- Webhook 地址脱敏，日志中不会泄露完整 Token
- 所有配置通过 Emby 后台 Simple UI 自动生成，无需手写 HTML

## 环境要求

- Emby Server 4.9.5.0
- .NET SDK 8.0（仅编译时需要）
- 飞书群机器人 Webhook 地址

## 快速开始

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

- [开发文档](docs/DEVELOPMENT.md)
- [编译文档](docs/BUILD.md)
- [部署文档](docs/DEPLOYMENT.md)
- [测试文档](docs/TESTING.md)
- [故障排查](docs/TROUBLESHOOTING.md)

## 许可

本项目仅供个人学习和使用。
