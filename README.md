# pyenv 环境全自动部署脚本

该项目用于自动化完成 Python 开发环境初始化，当前已实现 Windows 版本（Batch 脚本），Linux/macOS 版本处于规划中。

支持内容：

- pyenv 解压与调用
- 指定 Python 版本安装
- 虚拟环境自动创建
- requirements 依赖安装
- 可选 Node.js 自动安装（MSI/ZIP）
- 生成一键启动脚本（支持占位符替换）

## 当前目录结构

```text
├── AutoInstall.bat          # Windows 主部署脚本（已实现）
├── AutoInstall.sh           # Linux/macOS 主部署脚本（待开发）
├── Config/                  # 跨系统通用配置目录
│   └── config.ini           # 核心配置文件
|   └── 任意名称.txt          # 自定义一键启动bat命令
├── packages/                # 安装包目录（Windows 专用）
│   ├── pyenv-win.zip        # pyenv-win 压缩包（Windows）
│   ├── node-vX.X.X-x64.msi  # 可选：Node.js MSI安装包（Windows）
│   └── node-vX.X.X-win-x64.zip # 可选：Node.js ZIP安装包（Windows）
└── requirements.txt         # 跨系统通用：Python依赖清单
```


## 配置文件说明

配置文件路径：`ForWindows/Config/config.ini`

示例：

```ini
[settings]
# Python版本
VERSION=3.13.2
# 虚拟环境名称
VENV_NAME=你的虚拟环境名称
# 是否自动安装NodeJs
AUTO_INSTALL_NodeJs=yes
# 默认Python执行命令
DefaultExecution=python main.py
```

## 使用步骤（Windows）

1. 准备文件

- 将`ForWindows`下的所有文件移动到目标项目根目录下
- 将 `pyenv-win.zip` 放到 `packages/`
- 可选：将 Node.js 安装包（`.msi` 或 `.zip`）放到 `packages/`下  [Node.js官网](https://nodejs.org/en/download)
- 可选：在 `requirements.txt` 中写入 Python 依赖或直接使用目标项目包中的`requirements.txt`
- 可选：将`Remove_env.bat`放到目标项目根目录下

1. 配置参数

- 编辑 `/Config/config.ini`
- 至少确认 `VERSION` 与 `VENV_NAME` 两项
- 如果不需要Node.js自动安装则需要把`Config/config.ini`中的[AUTO_INSTALL_NodeJs]设置为[no]
  
1. 执行部署

```bat
cd ForWindows
AutoInstall.bat
```

建议使用管理员权限运行（尤其是 MSI 安装 Node.js 时）。

4. 启动使用

- 部署完成后，运行脚本生成的一键启动文件（通常为 `ForWindows/一键启动.bat`）
- 该脚本会激活虚拟环境，并执行 `DefaultExecution` 或自定义启动模板内容
  
5.其他说明
- 如果中途安装失败，请先检查控制台报错信息，确认网络状况，并清理根目录下的`.pyenv` `pyenv_temp` `你配置的虚拟环境名称(默认venvName)`三个文件夹然后重试

## 一键启动脚本自定义

脚本会在 `ForWindows/Config/` 下查找第一个 `.txt` 文件，并据此生成一键启动脚本。

可用占位符：

- `{BAT_DIR}`：脚本目录（Windows）
- `{VENV_NAME}`：虚拟环境名称
- `{VENV_PATH}`：虚拟环境完整路径
- `{VENV_PYTHON}`：虚拟环境 Python 可执行文件路径

Windows 模板示例：

```text
@echo off
call {VENV_PATH}\Scripts\activate.bat
echo 虚拟环境已激活，当前 Python 路径：{VENV_PYTHON}
cd {BAT_DIR}\src
python main.py
pause
```

## 常见问题

1. pyenv 安装 Python 失败

- 检查网络
- 检查 `VERSION` 格式是否受 pyenv-win 支持

2. Node.js 安装失败

- 检查安装包是否完整
- 查看日志：`%TEMP%\node_install.log`

3. requirements 安装失败

- 检查 `requirements.txt` 格式
- 检查虚拟环境是否创建成功

## 跨系统规划

| 功能 | Windows (Batch) | Linux (Bash) | macOS (Bash) |
| --- | --- | --- | --- |
| pyenv 环境部署 | 已实现 | 待开发 | 待开发 |
| Python 版本安装 | 已实现 | 待开发 | 待开发 |
| 虚拟环境创建 | 已实现 | 待开发 | 待开发 |
| requirements 安装 | 已实现 | 待开发 | 待开发 |
| Node.js 自动安装 | 已实现 | 待开发 | 待开发 |
| 一键启动脚本生成 | 已实现 | 待开发 | 待开发 |

Linux/macOS 后续适配方向：

- pyenv 安装：apt/yum/brew 或源码方式
- Node.js 安装：nvm、包管理器或官方二进制包
- 虚拟环境路径与激活命令的跨平台适配
- 系统环境变量写入策略适配

## 注意事项

1. 脚本使用 UTF-8，若终端出现乱码，请确认控制台编码设置
2. 运行前请确认系统可用 PowerShell（用于解压）
3. 若安装失败，请优先查看控制台报错与 Node.js 安装日志
4. pyenv 支持的 Python 版本：
- Windows：参考 [pyenv-win](https://github.com/pyenv-win/pyenv-win)
- Linux/macOS：参考 [pyenv](https://github.com/pyenv/pyenv)
## 许可证
本脚本为开源版本，可自由修改和分发，使用时请遵守 pyenv、Node.js 等相关软件的开源协议。