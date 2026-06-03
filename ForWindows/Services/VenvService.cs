using System;
using System.IO;

namespace AutoInstall.Services
{
    /// <summary>
    /// 虚拟环境创建与 pip 依赖安装
    /// </summary>
    public static class VenvService
    {
        public static void Create(string pythonExe, string venvPath)
        {
            if (File.Exists(Path.Combine(venvPath, "Scripts", "activate.bat")))
            {
                Console.WriteLine("[OK] 虚拟环境已存在");
                return;
            }

            ConsoleProgress.Step("创建虚拟环境");
            int exitCode = ConsoleProgress.RunWithOutput(pythonExe,
                string.Format("-m venv \"{0}\"", venvPath));

            if (exitCode != 0)
                throw new DeployException(string.Format("创建虚拟环境失败，错误码 {0}。", exitCode));

            Console.WriteLine("虚拟环境: {0}", venvPath);
        }

        public static void InstallRequirements(string venvPath, string batDir)
        {
            var requirements = Path.Combine(batDir, "requirements.txt");
            var pythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
            var pipExe = Path.Combine(venvPath, "Scripts", "pip.exe");

            if (!File.Exists(requirements))
            {
                Console.WriteLine();
                Console.WriteLine("[WARN] 未找到 requirements.txt，跳过依赖安装");
                return;
            }

            ConsoleProgress.Step("安装 pip 依赖");

            // 升级 pip（输出少，快速）
            ConsoleProgress.RunWithOutput(pythonExe, "-m pip install --upgrade pip");

            // 安装依赖（pip 自带下载进度条，实时可见）
            int exitCode = ConsoleProgress.RunWithOutput(pipExe,
                string.Format("install -r \"{0}\"", requirements));

            if (exitCode != 0)
                throw new DeployException(string.Format("依赖安装出现错误，错误码 {0}。", exitCode));

            Console.WriteLine();
            Console.WriteLine("[OK] 所有依赖安装完成");
        }

        public static string GetVenvPython(string venvPath)
        {
            return Path.Combine(venvPath, "Scripts", "python.exe");
        }

        public static string GetVenvPip(string venvPath)
        {
            return Path.Combine(venvPath, "Scripts", "pip.exe");
        }
    }
}
