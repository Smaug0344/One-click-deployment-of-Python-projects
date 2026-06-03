using System;
using System.IO;

namespace AutoInstall.Services
{
    /// <summary>
    /// pyenv 解压、安装、Python 版本管理
    /// </summary>
    public static class PyenvService
    {
        public static string Install(string batDir)
        {
            var packagesDir = Path.Combine(batDir, "packages");
            var pyenvZip = Path.Combine(packagesDir, "pyenv-win.zip");
            var unzipTemp = Path.Combine(batDir, "pyenv_temp");
            var pyenvRoot = Path.Combine(batDir, ".pyenv");
            var targetPyenvWin = Path.Combine(pyenvRoot, "pyenv-win");

            if (!Directory.Exists(packagesDir))
                throw new DeployException("请创建 packages 文件夹");
            if (!File.Exists(pyenvZip))
                throw new DeployException("请将 pyenv-win.zip 放入 packages");

            // ---- 解压 pyenv ----
            if (Directory.Exists(unzipTemp) && Directory.GetDirectories(unzipTemp).Length > 0)
            {
                Console.WriteLine("[OK] pyenv 已解压，跳过");
            }
            else
            {
                if (Directory.Exists(unzipTemp))
                {
                    Console.WriteLine("清理上次残留的 pyenv_temp ...");
                    try { Directory.Delete(unzipTemp, true); } catch { }
                }
                ConsoleProgress.ExtractZip(pyenvZip, unzipTemp, "解压 pyenv-win.zip");
            }

            // ---- 查找 pyenv.bat ----
            string pyenvBat = FindPyenvBat(unzipTemp);
            if (pyenvBat == null)
                throw new DeployException(string.Format(
                    "未在 {0} 下找到 pyenv.bat，请检查 pyenv-win.zip 是否完整。", unzipTemp));

            var pyenvWinRoot = Directory.GetParent(Directory.GetParent(pyenvBat).FullName).FullName;
            Console.WriteLine("找到 pyenv: {0}", pyenvBat);

            // ---- 安装到 .pyenv\pyenv-win ----
            if (!Directory.Exists(targetPyenvWin))
            {
                ConsoleProgress.CopyDirectoryWithProgress(pyenvWinRoot, targetPyenvWin,
                    "安装 pyenv 到 .pyenv\\pyenv-win");

                try { Directory.Delete(unzipTemp, true); }
                catch { Console.WriteLine("[WARN] 无法删除临时目录 {0}", unzipTemp); }

                pyenvBat = Path.Combine(targetPyenvWin, "bin", "pyenv.bat");
            }

            if (!File.Exists(pyenvBat))
                throw new DeployException(string.Format("未找到 pyenv 主程序: {0}", pyenvBat));

            var pyenvBin = Path.GetDirectoryName(pyenvBat);

            // ---- 设置环境变量 ----
            var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PATH", pyenvBin + ";" + currentPath, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYENV_ROOT", pyenvRoot, EnvironmentVariableTarget.Process);

            return pyenvBat;
        }

        /// <summary>
        /// 使用 pyenv 安装指定版本的 Python。
        /// 优先从 packages 文件夹查找本地安装包，避免重复下载。
        /// </summary>
        public static string InstallPython(string pyenvBat, string version, string batDir)
        {
            ConsoleProgress.Step(string.Format("安装 Python {0}", version));

            // ---- 检查 packages 中是否有本地安装包 ----
            TryUseLocalInstaller(version, batDir);

            // pyenv install（如果 install_cache 有缓存则跳过下载）
            int exitCode = ConsoleProgress.RunCmdWithOutput(pyenvBat,
                string.Format("install {0}", version));
            if (exitCode != 0)
                throw new DeployException(string.Format("pyenv install 返回错误码 {0}。", exitCode));

            ConsoleProgress.RunCmdWithOutput(pyenvBat, string.Format("local {0}", version));
            ConsoleProgress.RunCmdWithOutput(pyenvBat, "rehash");

            var pythonPath = ConsoleProgress.RunCmdAndCapture(pyenvBat, "which python");
            if (string.IsNullOrWhiteSpace(pythonPath) || !File.Exists(pythonPath.Trim()))
                throw new DeployException("无法通过 pyenv which 获取 Python 路径。");

            pythonPath = pythonPath.Trim();
            Console.WriteLine();
            Console.WriteLine("Python 路径: {0}", pythonPath);

            return pythonPath;
        }

        /// <summary>
        /// 扫描 packages 文件夹，查找与目标版本匹配的 Python 安装包。
        /// 找到后复制到 pyenv 的 install_cache，pyenv 会自动使用缓存跳过下载。
        /// </summary>
        private static void TryUseLocalInstaller(string version, string batDir)
        {
            var packagesDir = Path.Combine(batDir, "packages");
            var installCache = Path.Combine(batDir, ".pyenv", "pyenv-win", "install_cache");

            if (!Directory.Exists(packagesDir))
                return;

            // 构建匹配模式：python-3.11.9-amd64.exe, python-3.11.9.exe, python-3.11.9-amd64.msi 等
            var patterns = new[]
            {
                string.Format("python-{0}-amd64.exe", version),
                string.Format("python-{0}-amd64.msi", version),
                string.Format("python-{0}.exe", version),
                string.Format("python-{0}.msi", version),
                string.Format("python-{0}-win32.exe", version),
                string.Format("*python*{0}*.exe", version),
                string.Format("*python*{0}*.msi", version),
            };

            string found = null;
            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(packagesDir, pattern, SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    found = files[0];
                    break;
                }
            }

            if (found == null)
            {
                Console.WriteLine("packages 中未找到 Python {0} 的本地安装包，将在线下载。", version);
                return;
            }

            // 找到本地安装包，复制到 install_cache
            Console.WriteLine("在 packages 中找到本地安装包: {0}", Path.GetFileName(found));
            Directory.CreateDirectory(installCache);

            var destFile = Path.Combine(installCache, Path.GetFileName(found));
            if (File.Exists(destFile))
            {
                Console.WriteLine("install_cache 中已存在，跳过复制。");
            }
            else
            {
                Console.Write("复制到 install_cache ... ");
                File.Copy(found, destFile);
                Console.WriteLine("完成");
            }

            Console.WriteLine("[OK] 将使用本地安装包，跳过下载。");
        }

        private static string FindPyenvBat(string rootDir)
        {
            var candidate = Path.Combine(rootDir, "bin", "pyenv.bat");
            if (File.Exists(candidate)) return candidate;

            string best = null;
            foreach (var bat in Directory.GetFiles(rootDir, "pyenv.bat", SearchOption.AllDirectories))
                if (best == null || bat.Length < best.Length) best = bat;
            return best;
        }
    }
}
