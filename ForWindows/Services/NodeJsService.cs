using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AutoInstall.Services
{
    /// <summary>
    /// Node.js 检测与安装（从 packages 文件夹）
    /// </summary>
    public static class NodeJsService
    {
        public static bool IsNodeInstalled()
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c where node 2>nul",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                p.Start(); p.WaitForExit(3000);
                return p.ExitCode == 0;
            }
            catch { return false; }
        }

        private static string GetNodeVersion()
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c node --version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                var v = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(3000);
                return v;
            }
            catch { return "unknown"; }
        }

        public static void Install(string packagesDir, string batDir)
        {
            ConsoleProgress.Step("Node.js 自动安装");

            if (IsNodeInstalled())
            {
                Console.WriteLine("[OK] 已检测到 Node.js，版本：{0}", GetNodeVersion());
                return;
            }

            // MSI 优先
            var msiFiles = Directory.GetFiles(packagesDir, "*node*.msi", SearchOption.TopDirectoryOnly);
            if (msiFiles.Length > 0)
            {
                InstallFromMsi(msiFiles[0]);
                return;
            }

            // ZIP 备选
            var zipFiles = Directory.GetFiles(packagesDir, "*node*.zip", SearchOption.TopDirectoryOnly);
            if (zipFiles.Length > 0)
            {
                InstallFromZip(zipFiles[0], batDir);
                return;
            }

            Console.WriteLine("[WARN] 未在 packages 中找到 Node.js 安装包，跳过。");
        }

        private static void InstallFromMsi(string msiPath)
        {
            Console.WriteLine("安装 MSI: {0}", Path.GetFileName(msiPath));
            var logFile = Path.Combine(Path.GetTempPath(), "node_install.log");

            // msiexec 静默安装（日志写入文件）
            var exitCode = ConsoleProgress.RunWithOutput("msiexec.exe",
                string.Format("/i \"{0}\" /qn /norestart /l*v \"{1}\"", msiPath, logFile));
            Console.WriteLine("msiexec 返回码: {0}", exitCode);

            // 等待 node.exe 就绪
            Console.Write("等待 Node.js 就绪");
            int waited = 0;
            while (!IsNodeInstalled() && waited < 180)
            {
                Thread.Sleep(3000); waited += 3;
                Console.Write(".");
            }
            Console.WriteLine();

            if (IsNodeInstalled())
                Console.WriteLine("[OK] Node.js 已安装，版本：{0}", GetNodeVersion());
            else
            {
                Console.WriteLine("[ERROR] 等待超时，未检测到 node.exe。");
                if (File.Exists(logFile))
                {
                    Console.WriteLine("最后 20 行安装日志：");
                    try
                    {
                        var lines = File.ReadAllLines(logFile);
                        for (int i = Math.Max(0, lines.Length - 20); i < lines.Length; i++)
                            Console.WriteLine("  {0}", lines[i]);
                    }
                    catch { /* ignore */ }
                }
            }
        }

        private static void InstallFromZip(string zipPath, string batDir)
        {
            Console.WriteLine("安装 ZIP: {0}", Path.GetFileName(zipPath));
            var nodeDir = Path.Combine(batDir, "nodejs");

            if (Directory.Exists(nodeDir))
                try { Directory.Delete(nodeDir, true); } catch { }

            ConsoleProgress.ExtractZip(zipPath, nodeDir, "解压 Node.js");

            var nodeExe = Path.Combine(nodeDir, "node.exe");
            if (!File.Exists(nodeExe))
            {
                foreach (var sub in Directory.GetDirectories(nodeDir))
                {
                    var subExe = Path.Combine(sub, "node.exe");
                    if (File.Exists(subExe)) { nodeDir = sub; nodeExe = subExe; break; }
                }
            }

            if (File.Exists(nodeExe))
            {
                var curPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? "";
                Environment.SetEnvironmentVariable("PATH", nodeDir + ";" + curPath, EnvironmentVariableTarget.Process);
                Console.WriteLine("[OK] Node.js 版本：{0}", GetNodeVersion());
            }
            else
                Console.WriteLine("[ERROR] 未能在解压位置找到 node.exe。");
        }
    }
}
