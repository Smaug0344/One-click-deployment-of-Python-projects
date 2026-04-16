@echo off
chcp 65001
setlocal enabledelayedexpansion
set "ERROR_OCCURRED=0"
title pyenv 环境部署
echo.
echo ==============================================
echo      pyenv 全自动部署
echo ==============================================
echo.

rem 读取配置（从 Config\config.ini）

:: 检查 Config 文件夹是否存在
if not exist "%~dp0Config\" (
    echo 错误：未找到 Config 文件夹，请确认 %~dp0Config 存在。
    set "ERROR_OCCURRED=1"
    goto END_DEPLOY
)
set "DEFAULT_EXEC="
for /f "usebackq tokens=1,2 delims==" %%a in ("%~dp0Config\config.ini") do (
    if "%%a"=="VERSION" set "PY_VERSION=%%b"
    if "%%a"=="VENV_NAME" set "VENV_NAME=%%b"
    if "%%a"=="DefaultExecution" set "DEFAULT_EXEC=%%b"
    if "%%a"=="AUTO_INSTALL_NodeJs" set "AUTO_INSTALL_NODEJS=%%b"
    if "%%a"=="NODE_VERSION" set "NODE_VERSION=%%b"
)

goto :main

:: ---------------- subroutines ----------------
:__wait_for_node
:: Usage: call :__wait_for_node <timeout_seconds>
setlocal enabledelayedexpansion
set /a wait=0
set /a max=%1
:__wait_loop
where node >nul 2>&1
if not errorlevel 1 (
    endlocal
    exit /b 0
)
if %wait% GEQ %max% (
    endlocal
    exit /b 1
)
timeout /t 3 >nul
set /a wait+=3
goto __wait_loop

:main
echo [配置信息]
echo Python 版本 : %PY_VERSION%
echo 虚拟环境名称: %VENV_NAME%
if defined DEFAULT_EXEC echo DefaultExecution : %DEFAULT_EXEC%
echo.

:: ====================== 路径定义 ======================
set "BAT_DIR=%~dp0"
set "BAT_DIR=%BAT_DIR:~0,-1%"
set "PACKAGES=%BAT_DIR%\packages"
set "PYENV_ZIP=%PACKAGES%\pyenv-win.zip"
set "UNZIP_TEMP=%BAT_DIR%\pyenv_temp"
set "PYENV_ROOT=%BAT_DIR%\.pyenv"
set "VENV_PATH=%BAT_DIR%\%VENV_NAME%"
set "REQUIREMENTS=%BAT_DIR%\requirements.txt"
set "VENV_PYTHON=%VENV_PATH%\Scripts\python.exe"
set "VENV_PIP=%VENV_PATH%\Scripts\pip.exe"

:: ====================== 检查必备文件 ======================
if not exist "%PACKAGES%" (
    echo 错误：请创建 packages 文件夹
    set "ERROR_OCCURRED=1"
    goto END_DEPLOY
)
if not exist "%PYENV_ZIP%" (
    echo 错误：请将 pyenv-win.zip 放入 packages
    set "ERROR_OCCURRED=1"
    goto END_DEPLOY
)

:: ====================== 解压 pyenv ======================
echo 正在解压 pyenv...
if exist "%UNZIP_TEMP%" (
    echo ✅ pyenv 已解压，跳过
) else (
    echo 解压路径: %UNZIP_TEMP%
    powershell -NoProfile -Command "$ProgressPreference='SilentlyContinue'; Expand-Archive -Path '%PYENV_ZIP%' -DestinationPath '%UNZIP_TEMP%' -Force" >nul 2>&1
)

:: ====================== 自动获取第一个文件夹 ======================
for /d %%F in ("%UNZIP_TEMP%\*") do (
    set "PYENV_DIR=%%~fF"
    set "PYENV_BAT=!PYENV_DIR!\pyenv-win\bin\pyenv.bat"
    set "PYENV_BIN=!PYENV_DIR!\pyenv-win\bin"
    goto found_pyenv
)
:found_pyenv
:: Ensure we actually found a pyenv folder
if not defined PYENV_DIR (
    echo 错误：未在 %UNZIP_TEMP% 下找到 pyenv 解压的子文件夹。
    set "ERROR_OCCURRED=1"
    goto END_DEPLOY
)

:: 如果目标 %PYENV_ROOT% 下还没有 pyenv-win，则将解压内容移动/复制到 %PYENV_ROOT% 以便统一管理
if not exist "%PYENV_ROOT%\pyenv-win" (
    if exist "%PYENV_DIR%\pyenv-win" (
        echo 正在将 pyenv 安装到 %PYENV_ROOT% ... 
        if not exist "%PYENV_ROOT%" mkdir "%PYENV_ROOT%" >nul 2>&1
        rem 使用 robocopy 复制目录（更健壮），若缺失则回退到 xcopy
        robocopy "%PYENV_DIR%\pyenv-win" "%PYENV_ROOT%\pyenv-win" /E /NFL /NDL >nul 2>&1
        if errorlevel 1 (
            echo robocopy 复制失败，尝试 xcopy 备选方案...
            xcopy "%PYENV_DIR%\pyenv-win\*" "%PYENV_ROOT%\pyenv-win\" /E /I /Y >nul 2>&1
        )
        rem 尝试删除临时解压目录（忽略错误）
        if exist "%PYENV_DIR%" rd /s /q "%PYENV_DIR%" >nul 2>&1
        rem 更新变量指向新位置
        set "PYENV_DIR=%PYENV_ROOT%"
        set "PYENV_BAT=!PYENV_DIR!\pyenv-win\bin\pyenv.bat"
        set "PYENV_BIN=!PYENV_DIR!\pyenv-win\bin"
    ) else (
        rem 如果解压目录中没有 pyenv-win，则保留原有 PYENV_DIR
        rem 继续使用已解压位置
    )
)

if not defined PYENV_BAT (
    set "PYENV_BAT=!PYENV_DIR!\pyenv-win\bin\pyenv.bat"
    set "PYENV_BIN=!PYENV_DIR!\pyenv-win\bin"
)

if not exist "!PYENV_BAT!" (
    echo 错误：未找到 pyenv 主程序
    set "ERROR_OCCURRED=1"
    goto END_DEPLOY
)
echo 已找到 pyenv:!PYENV_BAT!

:: ====================== 配置环境变量 ======================
set "PATH=!PYENV_BIN!;%PATH%"
set "PYENV_ROOT=%PYENV_ROOT%"

:: ====================== 安装 Python ======================
echo.
echo ==============================================
echo           正在下载 Python %PY_VERSION%
echo ==============================================
echo.

:: 使用 pyenv 安装 Python
echo 正在使用 pyenv 安装 Python %PY_VERSION%...
call "!PYENV_BAT!" install %PY_VERSION%
if errorlevel 1 (
    echo pyenv install 返回错误码 %errorlevel%。
    set "ERROR_OCCURRED=1"
    goto END_DEPLOY
)

:: 设置本地版本
call "!PYENV_BAT!" local %PY_VERSION%
call "!PYENV_BAT!" rehash

:: 获取 pyenv 真实 python 路径
for /f "delims=" %%i in ('"!PYENV_BAT!" which python') do set "PYENV_PYTHON=%%i"
echo.
echo pyenv 已使用Python:!PYENV_PYTHON!
echo.

:: ====================== 创建虚拟环境 ======================
if exist "%VENV_PATH%\Scripts\activate.bat" (
    echo ✅ 虚拟环境已存在
) else (
    echo 正在使用 pyenv Python 创建虚拟环境...
    "!PYENV_PYTHON!" -m venv "%VENV_PATH%"
    if errorlevel 1 (
        echo 创建虚拟环境失败，错误码 %errorlevel%。
        set "ERROR_OCCURRED=1"
        goto END_DEPLOY
    )
)

:: ====================== Node.js: 从 packages 查找并安装 (末尾执行) ======================
if defined AUTO_INSTALL_NODEJS (
    if /I "%AUTO_INSTALL_NODEJS%"=="yes" (
        echo.
        echo ==============================================
        echo           在 packages 中查找 Node.js 安装包并安装
        echo ==============================================
        :: 先检测是否已安装 Node.js
        where node >nul 2>&1
        if errorlevel 1 (
            :: 未安装，继续查找安装包
            rem
        ) else (
            echo 已检测到 Node.js，版本：
            node --version
            goto node_install_skip
        )
        set "FOUND_NODE_MSI="
        set "FOUND_NODE_ZIP="
        :: 在 packages 中查找 MSI 优先
        for /f "delims=" %%N in ('dir /b "%PACKAGES%\*node*.msi" 2^>nul') do (
            set "FOUND_NODE_MSI=%PACKAGES%\%%N"
            goto found_node_pkg
        )
        :: 查找 zip
        for /f "delims=" %%N in ('dir /b "%PACKAGES%\*node*.zip" 2^>nul') do (
            set "FOUND_NODE_ZIP=%PACKAGES%\%%N"
            goto found_node_pkg
        )
        echo 未在 packages 中找到 Node.js 安装包，跳过自动安装。
        goto node_install_skip

:found_node_pkg
        if defined FOUND_NODE_MSI (
            echo 找到 MSI: %FOUND_NODE_MSI%
            echo 开始静默安装 MSI...
            echo 安装日志将写入: %TEMP%\node_install.log   
            msiexec /i "%FOUND_NODE_MSI%" /qn /norestart /l*v "%TEMP%\node_install.log"
            set "MSI_EXIT=%errorlevel%"
            echo msiexec 返回码: %MSI_EXIT%
            rem 等待 node.exe 出现（使用子程序），超时时间秒数为参数
            call :__wait_for_node 180
            set "WAIT_RESULT=%ERRORLEVEL%"
            if "%WAIT_RESULT%"=="0" (
                echo Node.js 已安装，版本：
                node --version
            ) else (
                echo 等待超时或安装失败，未检测到 node.exe。查看日志： %TEMP%\node_install.log
                powershell -NoProfile -Command "Get-Content '%TEMP%\\node_install.log' -Tail 80" || type "%TEMP%\node_install.log"
            )
            goto node_install_cont
        ) else if defined FOUND_NODE_ZIP (
            echo 找到 ZIP: %FOUND_NODE_ZIP%
            echo 解压 ZIP 到 %BAT_DIR%\nodejs ...
            if exist "%BAT_DIR%\nodejs" rd /s /q "%BAT_DIR%\nodejs"
            powershell -NoProfile -Command "Expand-Archive -Path '%FOUND_NODE_ZIP%' -DestinationPath '%BAT_DIR%\nodejs' -Force" >nul 2>&1
            set "NEW_NODE_DIR=%BAT_DIR%\nodejs"
            if exist "%NEW_NODE_DIR%\node.exe" (
                set "PATH=%NEW_NODE_DIR%;%PATH%"
                echo 已从 ZIP 安装 Node.js，版本：
                node --version
            ) else (
                :: 尝试子目录
                for /f "delims=" %%D in ('dir /b /ad "%BAT_DIR%\nodejs" 2^>nul') do (
                    if exist "%BAT_DIR%\nodejs\%%D\node.exe" (
                        set "NEW_NODE_DIR=%BAT_DIR%\nodejs\%%D"
                        set "PATH=%NEW_NODE_DIR%;%PATH%"
                    )
                )
                if exist "%NEW_NODE_DIR%\node.exe" (
                    echo 已从 ZIP 安装 Node.js，版本：
                    node --version
                ) else (
                    echo 未能在解压位置找到 node.exe，Node.js 安装失败。
                )
            )
        )
:node_install_skip
        echo.
    )
)

:: ====================== 只安装缺失的依赖 ======================
if exist "%REQUIREMENTS%" (
    echo.
    echo ==============================================
    echo           检查并安装缺失依赖
    echo ==============================================
    "%VENV_PYTHON%" -m pip install --upgrade pip
    "%VENV_PIP%" install -r "%REQUIREMENTS%"
    if errorlevel 1 (
        echo 依赖安装出现错误，错误码 %errorlevel%。
        set "ERROR_OCCURRED=1"
        goto END_DEPLOY
    )
    echo.
    echo ✅ 缺失依赖安装完成
) else (
    echo.   
    echo ⚠️ 未找到 requirements.txt，跳过依赖安装
)

:: Node.js 自动安装已移动到脚本末尾（从 packages 中查找安装包，版本不定）

:: ====================== 生成一键启动 ======================
set "STARTUP_BAT=%BAT_DIR%\一键启动.bat"

:: 在 Config 目录下递归查找第一个遇到的 .txt 文件（优先使用第一个找到的 .txt）
set "STARTUP_CONFIG="
if exist "%BAT_DIR%\Config\" (
    for /r "%BAT_DIR%\Config" %%G in (*.txt) do (
        set "STARTUP_CONFIG=%%~fG"
        goto startup_config_found
    )
)
:startup_config_found

:: 若未找到任何 .txt，保持兼容：使用默认 config 路径（若该文件存在则使用）
if not defined STARTUP_CONFIG (
    set "STARTUP_CONFIG=%BAT_DIR%\Config\启动虚拟环境.txt"
)

if not exist "%STARTUP_CONFIG%" (
    :: 兼容旧行为：写入激活命令，并在下一行写入 config\config.ini 中的 DefaultExecution（若有）
    echo call %%~dp0%VENV_NAME%\Scripts\activate.bat > "%STARTUP_BAT%"
    if defined DEFAULT_EXEC (
        >>"%STARTUP_BAT%" echo %DEFAULT_EXEC%
    )
) else (
    echo 正在根据 %STARTUP_CONFIG% 生成 %STARTUP_BAT% ...
    if exist "%STARTUP_BAT%" del "%STARTUP_BAT%"

    :: 使用 findstr /n 保留空行，然后逐行写入并替换占位符
    for /f "usebackq tokens=1* delims=:" %%A in (`findstr /n "^" "%STARTUP_CONFIG%"`) do (
        set "LINE=%%B"
        :: 支持占位符：{BAT_DIR} {VENV_NAME} {VENV_PATH} {VENV_PYTHON}
        set "LINE=!LINE:{BAT_DIR}=%BAT_DIR%!"
        set "LINE=!LINE:{VENV_NAME}=%VENV_NAME%!"
        set "LINE=!LINE:{VENV_PATH}=%VENV_PATH%!"
        set "LINE=!LINE:{VENV_PYTHON}=%VENV_PYTHON%!"
        if "%%B"=="" (
            >>"%STARTUP_BAT%" echo(
        ) else (
            >>"%STARTUP_BAT%" echo(!LINE!
        )
    )
)

:END_DEPLOY
echo.
echo ==============================================
if "%ERROR_OCCURRED%"=="1" (
    echo                部署完成（存在错误，请查看上方消息）
) else (
    echo                部署完成！
)
echo ==============================================
echo 已使用 pyenv 版本
echo 已安装缺失依赖
if exist "%STARTUP_BAT%" echo 已生成：%STARTUP_BAT%
echo.
echo 若有错误，请滚动查看上方日志。
pause >nul
if "%ERROR_OCCURRED%"=="1" exit /b 1
exit /b 0