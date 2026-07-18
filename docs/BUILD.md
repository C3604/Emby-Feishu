# 编译文档

## 环境要求

- .NET SDK 8.0（已测试 8.0.422）
- Emby Server 4.9.5.0 的引用 DLL

## 准备引用 DLL

将以下 DLL 从 Emby Server 的 `system` 目录复制到 `lib/emby/4.9.5.0/`：

- MediaBrowser.Common.dll
- MediaBrowser.Controller.dll
- MediaBrowser.Model.dll
- Emby.Web.GenericEdit.dll

## 编译命令

### Debug 模式

```bash
dotnet build src/EmbyFeishu/EmbyFeishu.csproj
```

### Release 模式（部署用）

```bash
dotnet build src/EmbyFeishu/EmbyFeishu.csproj -c Release
```

### 编译整个解决方案（含自测工具）

```bash
dotnet build EmbyFeishu.sln -c Release
```

## 一键 Release

仓库根目录提供了 `release.cmd` 和 `scripts/release.ps1`，用于一键执行完整发布流程：

- 读取 `Directory.Build.props` 中的当前版本号
- 校验 `lib/emby/4.9.5.0/` 下的 4 个 Emby 引用 DLL
- 执行 `dotnet restore`
- 执行 `dotnet build EmbyFeishu.sln -c Release`
- 执行 SelfTest
- 生成 `release/EmbyFeishu-v版本/`
- 输出 `EmbyFeishu.dll`、`SHA256SUMS.txt`、`RELEASE-NOTES.md`
- 额外生成 `release/EmbyFeishu-v版本.zip`

### Windows 命令行

```powershell
.\release.cmd
```

### PowerShell

```powershell
.\scripts\release.ps1
```

### 常用参数

```powershell
.\scripts\release.ps1 -Force
.\scripts\release.ps1 -SkipSelfTest
.\scripts\release.ps1 -EmbyReferencePath D:\Emby\system
```

## 输出文件

编译完成后，插件 DLL 位于：

- Debug：`src/EmbyFeishu/bin/Debug/EmbyFeishu.dll`
- Release：`src/EmbyFeishu/bin/Release/EmbyFeishu.dll`

部署时只需要 `EmbyFeishu.dll` 一个文件。

## 自定义 DLL 路径

如果 Emby DLL 不在默认位置，可以通过 MSBuild 属性指定：

```bash
dotnet build src/EmbyFeishu/EmbyFeishu.csproj -p:EmbyReferencePath=/path/to/emby/dlls
```
