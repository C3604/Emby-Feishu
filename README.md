# EmbyFeishu — Emby 飞书通知插件

![release](https://img.shields.io/badge/release-v1.4.0.0-blue)
![License](https://img.shields.io/badge/license-Personal%20Use-lightgrey)
![.NET](https://img.shields.io/badge/.NET-netstandard2.0-512BD4)
![Emby](https://img.shields.io/badge/Emby-4.9.5.0-green)

将 **Emby Server 4.9.5.0** 的事件实时推送到飞书群机器人。支持 52 种事件（8 大类），提供文本与飞书卡片两种格式、四档详细程度，内置去重、限流、聚合与全面脱敏。

---

## ✨ 核心功能

- **事件覆盖（8 大类，52 种）**：播放、登录与会话、用户管理、媒体库、用户行为、计划任务、Live TV（可选）、服务器状态。
- **双消息格式**：纯文本 + 飞书交互卡片（按严重程度配色，卡片失败自动回退文本）。
- **四档详细程度**：Simple / Standard / Detailed / Custom（默认 Custom，保持旧版外观）。
- **稳定不阻塞**：事件回调只入队，后台异步发送；有界队列 + 一次重试；飞书异常不影响 Emby 播放。
- **去重 / 限流 / 聚合**：短时去重、每分钟滑动窗口限流（安全事件可豁免）、媒体库扫描窗口聚合。
- **飞书安全设置**：支持「自定义关键词」与「签名校验（HMAC-SHA256）」。
- **全面脱敏**：Webhook、IP、设备 ID、异常路径均脱敏；绝不读取/记录密码、AccessToken、完整 Webhook。
- **Simple UI 配置**：8 大分组在 Emby 后台配置，保存即时生效，无需重启；旧配置平滑迁移。

---

## 🚀 快速构建

环境要求：.NET SDK 8.0（仅编译时需要）、Emby Server 4.9.5.0 的 4 个引用 DLL。

```bash
# 将 Emby 的 4 个 DLL 放入 lib/emby/4.9.5.0/（见 docs/BUILD.md）
#   MediaBrowser.Common.dll / MediaBrowser.Controller.dll
#   MediaBrowser.Model.dll   / Emby.Web.GenericEdit.dll

# 编译插件（Release）
dotnet build src/EmbyFeishu/EmbyFeishu.csproj -c Release

# 运行自测（235 项）
dotnet run --project tools/EmbyFeishu.SelfTest/EmbyFeishu.SelfTest.csproj -c Release
```

产物只有一个文件：`src/EmbyFeishu/bin/Release/EmbyFeishu.dll`。

---

## 📦 快速部署

1. 停止 Emby Server。
2. 将 `EmbyFeishu.dll`（**只需这一个文件**）复制到 Emby 的 `plugins` 目录；不要复制 `.deps.json`、`.pdb` 或任何 `MediaBrowser.*.dll`。
3. 启动 Emby Server。
4. 后台 → 侧边栏「飞书通知」（或插件列表 → Emby 飞书通知）→ 填写 Webhook → 勾选「发送测试通知」保存验证。

详见 [部署文档](docs/DEPLOYMENT.md) 与 [真实环境验证清单](docs/REAL-ENVIRONMENT-VERIFICATION.md)。

---

## 📚 文档索引

完整文档见 [docs/](docs/README.md)：

| 文档 | 内容 |
| --- | --- |
| [架构说明](docs/ARCHITECTURE.md) | 代码结构与消息处理流程 |
| [配置说明](docs/CONFIGURATION.md) | 8 大分组与全部字段 |
| [事件目录](docs/EVENT-CATALOG.md) | 事件矩阵与字段可用性 |
| [消息格式](docs/MESSAGE-FORMATS.md) | 文本 / 卡片与脱敏规则 |
| [飞书安全](docs/FEISHU-SECURITY.md) | 关键词与签名校验 |
| [编译文档](docs/BUILD.md) | 环境与命令 |
| [部署文档](docs/DEPLOYMENT.md) | 安装、升级、卸载 |
| [测试文档](docs/TESTING.md) | 自测覆盖与结果 |
| [故障排查](docs/TROUBLESHOOTING.md) | 常见问题与错误码 |
| [更新日志](CHANGELOG.md) | 版本历史 |

---

## ⚠️ 兼容性与限制

- 仅支持 **Emby Server 4.9.5.0**，不兼容 Emby 4.10+ 或 Jellyfin。
- 服务器停止通知为尽力而为（2 秒短超时），强杀 / 断电无法保证送达。
- 播放方式依赖客户端上报 `PlayMethod`，缺失时不推送。
- Live TV 未启用时该功能不加载，不影响其他事件。

---

## 📄 许可

本项目仅供个人学习和使用，任何商业用途需单独授权。

**版本**：v1.4.0.0 ｜ **最后更新**：2026-07-12
