# Python 环境全自动部署工具 (.NET 4.5)

该项目用于自动化完成 Python 开发环境初始化。**pyenv 与 Python 均安装于项目目录内，不写入系统 PATH 或注册表，Python 仅用于创建和运行虚拟环境。**

支持功能：

- pyenv-win 自动解压与安装（项目本地）
- 指定 Python 版本安装到项目本地（优先使用本地安装包）
- 虚拟环境自动创建
- requirements.txt 依赖安装
- 可选 Node.js 自动安装（MSI / ZIP）
- 一键启动脚本生成（支持占位符替换）

---

## 目录结构

```text
项目根目录/
├── AutoInstall.exe              # .NET 4.5 控制台部署程序
├── Config/
│   ├── config.ini               # 核心配置文件
│   └── 任意名称.txt              # 自定义一键启动模板
├── packages/
│   ├── pyenv-win.zip            # pyenv-win 压缩包（必选）
│   ├── python-x.x.x-amd64.exe   # 可选：Python 本地安装包（离线加速）
│   ├── node-vX.X.X-x64.msi      # 可选：Node.js MSI 安装包
│   └── node-vX.X.X-win-x64.zip  # 可选：Node.js ZIP 安装包
└── requirements.txt             # Python 依赖清单

```

---

## 配置文件说明

配置文件路径：`Config/config.ini`

```ini
# Python 版本（必填）
VERSION=3.11.9
# 虚拟环境名称（必填）
VENV_NAME=venv
# 是否自动安装 Node.js（yes / no）
AUTO_INSTALL_NodeJs=yes
# Node.js 版本号（参考用，实际以 packages 中的安装包为准）
NODE_VERSION=20.10.0
# 一键启动默认执行命令
DefaultExecution=python main.py
```

---

## 使用方式

将 `AutoInstall.exe` 放到项目根目录（与 `packages/`、`Config/`、`requirements.txt` 同级），双击运行。

---

## 部署流程

```
1. 读取 Config/config.ini
        │
2. 解压 pyenv-win.zip → 复制到 .pyenv/pyenv-win/（项目本地）
        │
3. 扫描 packages/ 查找本地 Python 安装包
   ├── 找到 → 复制到 install_cache → 跳过下载
   └── 未找到 → 在线下载
        │
4. pyenv install <VERSION> → 安装到 .pyenv/pyenv-win/versions/
        │
5. python -m venv <VENV_NAME>  → Python 仅作用于虚拟环境
        │
6. [可选] 安装 Node.js
   ├── 已安装 → 跳过
   ├── *.msi → msiexec /i 静默安装
   └── *.zip → 解压到 nodejs/ 并加入 PATH
        │
7. pip install -r requirements.txt  → 依赖安装到虚拟环境
        │
8. 生成 一键启动.bat（从 Config/*.txt 模板或默认内容）
        │
9. 完成
```

> **关键设计**：pyenv 安装到 `.pyenv/`（项目本地），Python 安装到 `.pyenv/pyenv-win/versions/`（项目本地）。环境变量仅设置在当前进程中（`EnvironmentVariableTarget.Process`），`AutoInstall.exe` 退出后自动失效，系统不受任何影响。Python 的唯一用途是创建虚拟环境 —— 所有依赖和运行均在 venv 内完成。

---

## 本地安装包加速

将对应版本的 Python 安装包放到 `packages/` 文件夹，程序会自动检测并复制到 pyenv 的 `install_cache`，**跳过在线下载**。

**文件名匹配规则**（按优先级）：

```
python-3.11.9-amd64.exe    ← 精确匹配（推荐命名）
python-3.11.9-amd64.msi
python-3.11.9.exe
python-3.11.9.msi
python-3.11.9-win32.exe
*python*3.11.9*.exe        ← 模糊匹配兜底
*python*3.11.9*.msi
```

> 安装包可从 [Python 官方下载页](https://www.python.org/downloads/) 获取。

---

## 一键启动脚本自定义

程序会在 `Config/` 下递归查找第一个 `.txt` 文件作为启动模板。若未找到模板，则生成默认内容：

```bat
call %~dp0<VENV_NAME>\Scripts\activate.bat
<DefaultExecution>
```

**模板占位符**：

| 占位符 | 替换内容 |
|--------|---------|
| `{BAT_DIR}` | 脚本所在目录完整路径 |
| `{VENV_NAME}` | 虚拟环境名称 |
| `{VENV_PATH}` | 虚拟环境完整路径 |
| `{VENV_PYTHON}` | 虚拟环境 Python.exe 完整路径 |

**模板示例**：

```bat
@echo off
chcp 65001 >nul
call {VENV_PATH}\Scripts\activate.bat
echo 虚拟环境已激活
echo Python: {VENV_PYTHON}
cd /d {BAT_DIR}\src
python main.py
pause
```

---

## 编译（开发者）

### 前置条件

- Windows 系统
- .NET Framework 4.5 或更高版本

### 编译命令

```bat
:: 使用 csc.exe 直接编译
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe ^
    /out:AutoInstall.exe ^
    /target:exe ^
    /reference:System.dll ^
    /reference:System.Core.dll ^
    /reference:System.IO.Compression.dll ^
    /reference:System.IO.Compression.FileSystem.dll ^
    /recurse:*.cs

:: 或使用 MSBuild
MSBuild AutoInstall.csproj /p:Configuration=Release
```

### 源码结构

| 文件 | 职责 |
|------|------|
| `Program.cs` | 入口 + 主流程编排 |
| `ConfigReader.cs` | Config\config.ini 解析与验证 |
| `ConsoleProgress.cs` | 进度条 / 实时进程输出 |
| `DeployException.cs` | 自定义部署异常 |
| `NodeJsService.cs` | Node.js 检测 + MSI/ZIP 安装 |
| `PyenvService.cs` | pyenv 本地解压 / Python 版本管理 |
| `StartupGenerator.cs` | 一键启动.bat 生成（占位符替换） |
| `VenvService.cs` | 虚拟环境创建 + pip 依赖安装 |

---

## 常见问题

### 1. 闪退看不到错误

使用命令行运行 `AutoInstall.exe`，所有错误路径都会暂停并显示详细信息。

### 2. pyenv 安装 Python 失败

- 检查网络连接
- 确认 `VERSION` 格式被 pyenv-win 支持（如 `3.11.9`）
- 将 Python 安装包放入 `packages/` 使用离线安装

### 3. Node.js 安装失败

- 检查 MSI/ZIP 安装包完整性
- MSI 安装日志：`%TEMP%\node_install.log`
- ZIP 安装失败可手动解压到 `nodejs/` 目录

### 4. requirements.txt 安装失败

- 检查依赖包名和版本号格式
- 确认虚拟环境创建成功（`<VENV_NAME>/Scripts/python.exe` 存在）

### 5. pyenv_temp 残留

程序会在安装完成后自动清理 `pyenv_temp`。若手动中断导致残留，删除该目录后重新运行即可。


---

## 清理与重置

如果部署中途失败需要重新开始，删除以下目录：

```
.pyenv/           # pyenv 安装目录
<VENV_NAME>/      # 虚拟环境目录（如 venv/）
nodejs/           # Node.js 解压目录（如存在）
```

> 所有文件均位于项目目录内，删除上述目录即可完全清理，系统中不留任何痕迹。

---

## 许可证

本项目为开源工具，可自由修改和分发。使用时请遵守 pyenv、Node.js、Python 等相关软件的许可协议。
