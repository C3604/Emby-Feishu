# EmbyFeishu — Emby 飞书通知插件

![GitHub release](https://img.shields.io/badge/release-v1.3.0.0-blue)
![License](https://img.shields.io/badge/license-Personal%20Use-lightgrey)
![.NET](https://img.shields.io/badge/.NET-netstandard2.0-512BD4)
![Emby](https://img.shields.io/badge/Emby-4.9.5.0-green)

**将 Emby Server 4.9.5.0 的事件实时推送到飞书群组**

支持 45+ 种事件类型（播放、登录、用户、媒体库、任务、直播、服务器），提供文本与卡片两种消息格式、四档详细程度，内置去重、限流、聚合与全面脱敏。

---

## ✨ 核心特性

### 事件覆盖（8 大类，45+ 类型）

| 事件类别 | 支持类型 | 数量 |
|---------|--------|------|
| 📺 **播放** | 开始 / 停止 / 暂停 / 恢复 / 完成 / 放弃 / 方式变化 / 里程碑 | 8 |
| 🔐 **登录与会话** | 成功 / 失败、会话开始 / 结束、远程控制断开、同步播放加入 / 离开 | 7 |
| 👤 **用户管理** | 锁定、改密、创建 / 删除 / 更新、策略 / 配置更新 | 7 |
| 📚 **媒体库** | 新增 / 更新 / 删除、扫描聚合 | 4 + 聚合 |
| 👁️ **用户行为** | 收藏、标记已看 / 未看、评分变化 | 5 |
| ⚙️ **计划任务** | 失败 / 完成 / 取消、扫描 / 元数据 / 备份 | 7 |
| 📡 **Live TV** | 录制 / 定时事件（可选）| 8 |
| 🖥️ **服务器** | 启动 / 停止 / 更新 / 重启 / 维护模式 | 6 |

### 智能消息处理

- **双格式支持**：纯文本 + 飞书交互卡片（按严重程度自动配色，失败回退文本）
- **四档详细程度**：Simple / Standard / Detailed / Custom（默认保持旧版外观）
- **标题格式化**：自动识别 S01E02、电影名称、客户端与设备识别

### 稳定性与性能

- **去重与限流**：有界去重缓存 + 每分钟滑动窗口限流，安全事件智能豁免
- **媒体库聚合**：窗口聚合防扫描消息风暴，逐条 → 汇总自动切换
- **异步不阻塞**：后台异步发送，网络异常不影响 Emby 播放
- **150 项自测**：全覆盖单元测试，Debug/Release 0 警告 0 错误

### 安全与隐私

- **完全脱敏**：Webhook（域名+末四位）、IP（掩码）、设备 ID（末四位）、异常路径
- **绝不记录敏感信息**：永不读取/存储/推送密码、AccessToken、完整 Webhook URL
- **自动摘要清理**：任务失败仅推送简短摘要，无堆栈、路径、Token

### 灵活配置

- **60+ 配置项**（12 组）：通过 Emby 后台 Simple UI 配置，无需编写 HTML
- **向后兼容**：旧配置平滑加载，新字段安全默认，GUID/Webhook 不变
- **用户过滤**：全部 / 仅包含 / 排除，支持多用户独立控制

---

## 🚀 快速开始

### 环境要求

- **Emby Server** 4.9.5.0
- **.NET SDK** 8.0（仅编译时需要）
- **飞书群机器人** Webhook URL（[获取方法](https://open.feishu.cn/document/client-docs/bot-v3/add-custom-bot)）

### 编译与部署

#### 选项 A：自己编译

```bash
# 克隆仓库
git clone https://github.com/YourUsername/Emby-Feishu.git
cd Emby-Feishu

# 编译插件
dotnet build src/EmbyFeishu/EmbyFeishu.csproj -c Release

# 复制到 Emby 插件目录（Windows 示例）
copy "src\EmbyFeishu\bin\Release\EmbyFeishu.dll" "C:\ProgramData\Emby-Server\plugins\"

# 或 Linux 示例
cp src/EmbyFeishu/bin/Release/EmbyFeishu.dll /var/lib/emby/plugins/
```

#### 选项 B：下载预编译版本

前往 [GitHub Releases](../../releases) 下载最新的 `EmbyFeishu.dll`。

### 配置插件

1. **重启 Emby Server** 使插件生效
2. 登录 Emby 后台 → **插件** → **Emby 飞书通知**
3. 粘贴飞书群机器人 Webhook 地址
4. 配置其他选项（可选）
5. **启用插件** 并保存

详见 [部署文档](docs/DEPLOYMENT.md)。

---

## 📋 使用示例

### 消息格式对比

**Simple**（极简）
```
🎬 播放开始：The Shawshank Redemption
```

**Standard**（标准，推荐）
```
🎬 The Shawshank Redemption
用户：admin | 时间：2h 22m
```

**Detailed**（详细，含技术信息）
```
🎬 The Shawshank Redemption
用户：admin | 客户端：Web | 设备：Chrome
流媒体：Direct Play | 分辨率：1920×1080
播放时间：2h 22m | 服务器：MainServer
```

**Custom**（自定义）
```
根据旧版字段开关自定义显示内容，保持向后兼容
```

### 飞书卡片示例

卡片格式支持：
- 🟦 蓝色（Information）、🟩 绿色（Success）、🟧 橙色（Warning）、🟥 红色（Error/Security）
- 双列字段布局，页脚显示事件时间和服务器名称
- 特殊字符自动转义，支持表情符号

---

## ⚙️ 配置概览

所有配置在 Emby 后台 Simple UI 中设置，立即生效无需重启。

### 配置分组

| 分组 | 说明 | 主要选项 |
|------|------|--------|
| ① 飞书连接 | Webhook 地址、超时、启用开关 | Webhook URL、3-60s 超时 |
| ② 消息格式 | 格式选择、详细程度、脱敏 | Text/FeishuCard、Simple/Detailed |
| ③ 播放通知 | 开始/停止/完成/里程碑 | 25%/50%/75% 里程碑阈值 |
| ④ 登录与安全 | 登录/会话/同步播放事件 | 失败推送、锁定通知 |
| ⑤ 用户管理 | 创建/删除/更新/策略 | 用户事件开关 |
| ⑥ 媒体库 | 新增/更新/删除、聚合 | 窗口 10-600s、上限 0-50 |
| ⑦ 用户行为 | 收藏、标记已看、评分 | 用户行为事件开关 |
| ⑧ 计划任务 | 失败/完成/扫描/备份 | 任务事件开关 |
| ⑨ Live TV | 录制与定时事件 | 可选（不可用不阻断） |
| ⑩ 服务器 | 启动/停止/更新/重启 | 服务器事件开关 |
| ⑪ 高级 | 用户过滤、限流、聚合 | All/IncludeOnly/Exclude、30/min |
| ⑫ 诊断 | 测试推送、结果查看 | 一键测试 |

完整说明见 [配置文档](docs/CONFIGURATION.md)。

---

## 📚 文档

| 文档 | 内容 |
|------|------|
| [📖 事件目录](docs/EVENT-CATALOG.md) | 9 类事件矩阵、推送默认、字段可用性 |
| [💬 消息格式](docs/MESSAGE-FORMATS.md) | 文本/卡片格式对比、脱敏规则详解 |
| [⚙️ 配置说明](docs/CONFIGURATION.md) | 60+ 选项完整参考、迁移指南 |
| [📝 更新日志](docs/CHANGELOG.md) | v1.3.0.0 大幅扩展、已知限制 |
| [🔨 部署文档](docs/DEPLOYMENT.md) | 编译、安装、Emby 集成 |
| [✅ 测试文档](docs/TESTING.md) | 150 项自测覆盖、真实环境验证 |
| [🏗️ 开发文档](docs/DEVELOPMENT.md) | 架构设计、扩展指南 |
| [🔧 故障排查](docs/TROUBLESHOOTING.md) | 常见问题、日志分析 |

---

## 🏗️ 项目结构

```
EmbyFeishu/
├── src/EmbyFeishu/                      # 插件核心
│   ├── Models/                          # 数据模型（NotificationEvent、枚举）
│   ├── Events/                          # 事件处理（8 个源 + 策略/聚合/缓存）
│   ├── Messaging/                       # 消息格式化（文本/卡片）
│   ├── Feishu/                          # 飞书 Webhook 客户端
│   ├── Infrastructure/                  # 工具类（脱敏、分类）
│   ├── Configuration/                   # 配置校验、迁移
│   ├── EntryPoint.cs                    # 容器组装、事件订阅
│   ├── Plugin.cs                        # 插件主类（Simple UI）
│   ├── PluginOptions.cs                 # 配置选项（60+ 字段）
│   └── logo/                            # 插件图标 PNG
├── tools/
│   └── EmbyFeishu.SelfTest/             # 150 项自测工具
├── lib/
│   └── emby/4.9.5.0/                    # Emby 引用 DLL（gitignore）
└── docs/                                # 完整文档（11 份）
```

---

## 🔒 安全保证

### 绝不读取/记录敏感信息

✅ **密码与 Token**
- `AuthenticationRequest.Password` 永不读取/记录
- `AuthenticationResult.AccessToken` 永不存储/推送
- 登录失败消息自动排除密码字段

✅ **Webhook 脱敏**
- 完整 URL 仅在内存中使用
- 日志中仅保留域名 + 末四位 Token
- 配置文件存储时已脱敏

✅ **异常路径清理**
- 任务失败摘要自动移除绝对路径
- 无堆栈跟踪、无系统路径泄露

### 网络与事件线程安全

✅ **异步 + 不阻塞**
- 所有 HTTP 请求异步发送，绝不在事件回调中同步等待
- Webhook 失败不影响 Emby 播放

✅ **资源隔离**
- 后台 `SemaphoreSlim` 单循环处理队列
- 无无限队列/缓存/内存泄露

✅ **优雅关闭**
- 插件卸载时对称解除所有事件订阅
- 聚合器安全刷新未推送消息

---

## 📊 测试与质量

| 指标 | 状态 |
|------|------|
| **单元测试** | ✅ 150/150 通过 |
| **Debug 编译** | ✅ 0 警告 0 错误 |
| **Release 编译** | ✅ 0 警告 0 错误 |
| **测试覆盖** | ✅ 标题/时间/脱敏/过滤/聚合/限流/去重 |
| **依赖** | ✅ netstandard2.0（仅 System.Memory 4.6.0） |
| **部署产物** | ✅ EmbyFeishu.dll（单一文件） |

### 自测项目

- 媒体标题格式化（电影/剧集/空值）
- 时间格式化（HH:mm:ss / mm:ss / null）
- Webhook 脱敏（URL + Token）
- IP 显示模式（Hidden / Masked / Full）
- 设备 ID 脱敏（末四位）
- 用户过滤（All / IncludeOnly / Exclude）
- 配置校验与迁移
- 去重缓存与滑动窗口限流
- 播放状态机与多会话隔离
- 媒体库聚合（逐条 → 汇总）

详见 [测试文档](docs/TESTING.md)。

---

## 🎯 版本历史

### v1.3.0.0（当前）

**重大功能扩展**

- 统一事件模型 `NotificationEvent` 与 8 个独立事件源架构
- 45+ 新事件类型（播放增强、登录、会话、媒体库、用户、任务、直播、服务器）
- 飞书交互卡片格式（按严重程度配色）+ 四档详细程度
- 去重 + 限流 + 聚合完整策略体系
- 全面敏感信息脱敏（Webhook/IP/设备ID/异常/路径）
- 150 项自测全过，0 警告 0 错误

**向后兼容**
- 插件 GUID、配置文件名、Webhook 地址不变
- 旧配置平滑加载，新字段安全默认
- 默认 Custom 模式保持旧版播放通知外观

### v1.2.0.0

- 安全加固：Webhook 脱敏、HttpClient 日志关闭、异常消息清理

### v1.0 - v1.1

- 初始版本：播放通知、用户过滤、Simple UI 配置

详见 [CHANGELOG.md](docs/CHANGELOG.md)。

---

## ⚠️ 已知限制

| 限制 | 说明 |
|------|------|
| **登录失败聚合** | 30 秒去重 + 安全豁免，不支持复杂时间窗口 |
| **服务器停止通知** | 2 秒短超时，强杀/断电无法保证送达 |
| **播放方式** | 依赖客户端上报 `PlayMethod`，缺失时不推送 |
| **同步播放** | 事件受 Emby 4.9.5.0 载荷限制，仅标题级 |
| **Live TV** | 服务器未启用时本功能不加载（不影响其他） |

---

## 💡 常见问题

**Q: 能否在 Emby 4.10+ 或 Jellyfin 上使用？**  
A: 否。本插件专为 Emby 4.9.5.0 设计，API 存在差异。

**Q: 如何查看日志？**  
A: 日志在 Emby 后台 → 控制台 → 日志，所有敏感信息已脱敏。

**Q: 可以修改消息内容吗？**  
A: 支持通过 Custom 详细程度自定义字段显示，完整修改需编译源码。

**Q: 插件会影响播放性能吗？**  
A: 否。所有处理异步进行，绝不阻塞事件回调线程。

详见 [故障排查](docs/TROUBLESHOOTING.md)。

---

## 🤝 贡献

本项目为个人学习项目。

欢迎提 Issue 反馈 bug、提出建议或报告安全问题。

---

## 📄 许可

本项目仅供个人学习和使用。任何商业用途需单独授权。

---

**需要帮助？**

- 📖 查看 [部署文档](docs/DEPLOYMENT.md) 了解详细安装步骤
- 🔧 查看 [故障排查](docs/TROUBLESHOOTING.md) 解决常见问题
- ✅ 查看 [真实环境验证清单](docs/REAL-ENVIRONMENT-VERIFICATION.md) 验证部署成功

---

**最后更新**：2026-07-12 | **版本**：v1.3.0.0
