# 部署文档

## 一、获取插件文件

编译完成后，插件文件位于：

```
src/EmbyFeishu/bin/Release/EmbyFeishu.dll
```

**只需要这一个文件。** 不要复制 `.deps.json`、`.pdb`，也不要复制 `lib/emby/` 目录下的任何 DLL。

## 二、找到 Emby 的插件目录

插件目录的位置取决于你的 Emby 安装方式：

### Windows

通常在以下位置之一：

```
C:\Users\<你的用户名>\AppData\Roaming\Emby-Server\plugins
C:\ProgramData\Emby-Server\plugins
```

也可以在 Emby 后台 → 服务器 → 系统路径 中查看数据目录，插件目录就在数据目录下的 `plugins` 文件夹。

### Linux

```
/var/lib/emby/plugins
```

或

```
~/.emby-server/plugins
```

### Docker

如果你映射了数据目录（假设映射到 `/emby-data`）：

```
/emby-data/plugins
```

可以用以下命令复制文件到容器中：

```bash
docker cp EmbyFeishu.dll emby:/config/plugins/
```

### 群晖 NAS

通常在共享文件夹中：

```
/volume1/Emby/plugins
```

具体路径以你实际设置的 Emby 数据目录为准。

## 三、上传插件

1. 将 `EmbyFeishu.dll` 复制到上一步找到的 `plugins` 目录中。
2. **不要**上传以下文件，它们已经在 Emby Server 中存在，上传会导致冲突：
   - MediaBrowser.Common.dll
   - MediaBrowser.Controller.dll
   - MediaBrowser.Model.dll
   - Emby.Web.GenericEdit.dll
   - 任何其他 Emby 自带的 DLL

## 四、重启 Emby Server

### Windows

1. 打开"服务"（按 Win+R，输入 `services.msc`）
2. 找到"Emby Server"
3. 右键 → 重新启动

或在 Emby 后台 → 服务器 → 关机 → 重启服务器

### Linux

```bash
sudo systemctl restart emby-server
```

### Docker

```bash
docker restart emby
```

容器名称以你实际创建时的名称为准。

## 五、确认插件加载成功

1. 打开 Emby 后台管理页面（通常是 `http://你的服务器IP:8096`）
2. 点击左侧菜单 → **插件**
3. 在插件列表中应该看到"**Emby 飞书通知**"
4. 如果没有看到，请查看 [故障排查文档](TROUBLESHOOTING.md)

## 六、配置插件

1. 在插件列表中点击"**Emby 飞书通知**"
2. 进入设置页面后，你会看到以下选项：
   - **启用插件**：打开总开关
   - **飞书 Webhook 地址**：粘贴你的飞书群机器人 Webhook 地址

### 获取飞书 Webhook 地址

1. 打开飞书桌面版或网页版
2. 进入你要接收通知的群
3. 点击群名称 → 群设置 → 群机器人 → 添加机器人
4. 选择"自定义机器人"
5. 填写机器人名称（如"Emby 通知"）
6. 复制 Webhook 地址（以 `https://open.feishu.cn/open-apis/bot/v2/hook/` 开头）
7. 将地址粘贴到插件设置中

### 推荐配置

- **启用插件**：✅ 开启
- **飞书 Webhook 地址**：粘贴你的 Webhook 地址
- **仅通知视频播放**：✅ 开启（推荐，避免音频干扰）
- **通知播放开始**：✅ 开启
- **通知播放停止**：✅ 开启
- **通知播放暂停**：根据需要（频繁暂停会产生大量通知）

3. 点击保存

## 七、测试配置

### 方法一：使用测试推送（推荐）

1. 在插件设置页面，勾选 **"发送测试通知"**
2. 点击 **保存**
3. 插件会立即向飞书发送一条测试消息
4. 保存完成后，查看 **"上次测试结果"** 字段：
   - 显示 ✅ 表示发送成功，去飞书群确认是否收到
   - 显示 ❌ 表示发送失败，根据错误信息排查
5. "发送测试通知"会自动取消勾选，不影响后续正常使用

### 方法二：播放视频测试

1. 确保插件已启用并保存
2. 在 Emby 中播放任意一个视频
3. 等待几秒钟
4. 查看飞书群是否收到通知消息
5. 停止播放，检查是否收到停止通知

如果没有收到通知，请查看 [故障排查文档](TROUBLESHOOTING.md)。

## 八、查看 Emby 日志

当出现问题时，查看日志有助于排查：

### Windows

日志目录通常在：

```
C:\Users\<你的用户名>\AppData\Roaming\Emby-Server\logs
```

### Linux

```
/var/lib/emby/logs
```

### Docker

```
/config/logs
```

在日志文件中搜索 `[EmbyFeishu]` 可以看到插件的所有日志。

也可以在 Emby 后台 → 服务器 → 日志 中直接查看。

## 九、卸载插件

1. 停止 Emby Server
2. 从 plugins 目录删除 `EmbyFeishu.dll`
3. 重启 Emby Server

## 十、升级插件

1. 编译或获取新版本的 `EmbyFeishu.dll`
2. 停止 Emby Server
3. 用新文件替换 plugins 目录中的旧 `EmbyFeishu.dll`
4. 重启 Emby Server

**你之前的配置会自动保留，不需要重新设置。**
