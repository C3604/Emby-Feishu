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
