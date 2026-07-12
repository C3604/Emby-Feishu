# 消息格式（MESSAGE-FORMATS）

插件支持两种飞书消息格式与四档详细程度，均由统一事件模型渲染，两种格式内容一致。

## 消息格式

| 值 | 说明 |
| --- | --- |
| `Text`（默认） | 纯文本，与旧版外观一致，升级不会突变 |
| `FeishuCard` | 飞书交互卡片，带配色标题与双列字段 |

卡片发送失败时，若开启「卡片失败时回退文本」，会用文本重发一次；同一业务事件最终最多成功一次，不会无限回退或重试。

## 详细程度

| 值 | 内容 |
| --- | --- |
| `Simple` | 标题 + 最关键字段（如用户、媒体） |
| `Standard` | 标题 + 用户/对象/客户端/设备/主要状态/时间 |
| `Detailed` | 在 Standard 基础上按分类增加技术细节（编码、分辨率、脱敏 IP 等，需开启「显示敏感技术细节」） |
| `Custom`（默认） | 播放事件沿用旧的字段开关（`显示用户名`/`显示媒体标题`…），其余分类等同 Standard |

> 默认 `Custom` 是为了让**旧用户升级后播放通知外观完全不变**。新用户如需更丰富信息可切到 Standard/Detailed。

字段为空一律省略，不会出现“字段名：”后为空的情况。

---

## 文本示例

### 开始播放（Custom 默认）

```
▶️ 开始播放

用户：张三
媒体：权力的游戏 S01E02 - 国王大道
客户端：Emby Web
设备：Chrome
时间：2026-07-12 22:30:15
```

### 播放完成

```
✅ 播放完成

用户：张三
媒体：星际穿越
完成进度：100%
服务器：MyEmby
时间：2026-07-12 23:43:40
```

### 登录失败（Security）

```
🚨 Emby 登录失败

用户：admin
客户端：Emby for Android
设备：Pixel 7
远程地址：203.0.*.*
时间：2026-07-12 09:12:01
```

### 新内容入库

```
🎬 新媒体入库

名称：奥本海默
类型：电影
年份：2023
媒体库：电影
时间：2026-07-12 03:00:11
```

### 媒体库汇总

```
📚 媒体库更新

新增电影：3
新增剧集：12
更新项目：2
时间：2026-07-12 03:05:00
```

### 计划任务失败

```
❌ Emby 计划任务失败

任务：扫描媒体库文件
分类：Library
状态：失败
耗时：1分12秒
错误摘要：无法访问 …/movie.mkv
时间：2026-07-12 04:00:00
```

### 服务器需要重启

```
🔄 Emby Server 需要重启

更新或配置等待应用
当前版本：4.9.5.0
服务器：MyEmby
时间：2026-07-12 05:00:00
```

---

## 卡片示例（结构）

卡片使用结构化对象构造并由 Emby `IJsonSerializer` 序列化，**不做 JSON 字符串拼接**。

- **Header**：标题（含图标），`template` 颜色按严重程度映射
  - Information→`blue`，Success→`green`，Warning→`orange`，Error/Security→`red`
- **正文**：摘要（如有）
- **Fields**：双列关键字段（`is_short:true`）
- **Divider**：`hr`
- **Footer**：`note`，显示服务器名与时间

对应 JSON（简化）：

```json
{
  "msg_type": "interactive",
  "card": {
    "config": { "wide_screen_mode": true },
    "header": {
      "template": "blue",
      "title": { "tag": "plain_text", "content": "▶️ 开始播放" }
    },
    "elements": [
      { "tag": "div", "fields": [
        { "is_short": true, "text": { "tag": "lark_md", "content": "**用户**\n张三" } },
        { "is_short": true, "text": { "tag": "lark_md", "content": "**媒体**\n权力的游戏 S01E02" } }
      ]},
      { "tag": "hr" },
      { "tag": "note", "elements": [ { "tag": "lark_md", "content": "MyEmby  ·  2026-07-12 22:30:15" } ] }
    ]
  }
}
```

## 敏感信息显示

| 配置 | 取值 | 默认 |
| --- | --- | --- |
| IP 显示方式 | Hidden / Masked / Full | Masked（如 `203.0.*.*`） |
| 设备 ID 显示方式 | Hidden / Masked / Full | Masked（如 `****3456`） |
| 显示敏感技术细节 | 开/关 | 关（Detailed 下才展示编码/分辨率/IP） |

Webhook 地址在任何日志中都只显示脱敏形式（域名+末四位）。
