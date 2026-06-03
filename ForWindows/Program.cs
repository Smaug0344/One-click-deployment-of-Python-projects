using System;
using System.IO;
using AutoInstall.Services;

namespace AutoInstall
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "pyenv 环境部署（项目本地）";

            PrintBanner();

            try
            {
                // ========== 1. 路径初始化 ==========
                var batDir = AppDomain.CurrentDomain.BaseDirectory;
                batDir = batDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                Console.WriteLine("工作目录: {0}", batDir);
                Console.WriteLine();

                // ========== 2. 读取配置 ==========
                var config = ConfigReader.Read(batDir);
                Console.WriteLine("[配置信息]");
                Console.WriteLine("Python 版本 : {0}", config.Version);
                Console.WriteLine("虚拟环境名称: {0}", config.VenvName);
                if (!string.IsNullOrWhiteSpace(config.DefaultExecution))
                    Console.WriteLine("DefaultExecution : {0}", config.DefaultExecution);
                Console.WriteLine();

                // ========== 3. 安装 pyenv（项目本地） ==========
                var pyenvBat = PyenvService.Install(batDir);

                // ========== 4. 安装 Python 到项目本地 ==========
                // Python 安装到 .pyenv\pyenv-win\versions\ 下，不写入系统
                var pythonExe = PyenvService.InstallPython(pyenvBat, config.Version, batDir);

                // ========== 5. 创建虚拟环境 ==========
                // 使用项目本地的 Python 创建 venv，venv 是独立的隔离环境
                var venvPath = Path.Combine(batDir, config.VenvName);
                var venvPython = VenvService.GetVenvPython(venvPath);
                VenvService.Create(pythonExe, venvPath);

                // ========== 6. 安装 Node.js ==========
                if (!string.IsNullOrWhiteSpace(config.AutoInstallNodeJs) &&
                    string.Equals(config.AutoInstallNodeJs, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    var packagesDir = Path.Combine(batDir, "packages");
                    NodeJsService.Install(packagesDir, batDir);
                }

                // ========== 7. 安装 pip 依赖 ==========
                var upgradePip = !string.Equals(config.UpgradePip, "no", StringComparison.OrdinalIgnoreCase);
                VenvService.InstallRequirements(venvPath, batDir, upgradePip);

                // ========== 8. 生成一键启动 ==========
                StartupGenerator.Generate(batDir, config.VenvName, venvPath, venvPython,
                    config.DefaultExecution);

                PrintFooter(true);
                return 0;
            }
            catch (DeployException ex)
            {
                Console.WriteLine();
                Console.WriteLine("==============================================");
                Console.WriteLine("  部署失败");
                Console.WriteLine("==============================================");
                Console.WriteLine("错误：{0}", ex.Message);
                PrintFooter(false);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("==============================================");
                Console.WriteLine("  发生未预期的错误");
                Console.WriteLine("==============================================");
                Console.WriteLine(ex.ToString());
                PrintFooter(false);
                return 2;
            }
            finally
            {
                // 始终暂停，让用户看到结果
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
        }

        private static void PrintBanner()
        {
            Console.WriteLine();
            Console.WriteLine("==============================================");
            Console.WriteLine("      pyenv 全自动部署（项目本地）");
            Console.WriteLine("==============================================");
            Console.WriteLine();
        }

        private static void PrintFooter(bool success)
        {
            Console.WriteLine();
            Console.WriteLine("==============================================");
            if (success)
                Console.WriteLine("                部署完成！");
            else
                Console.WriteLine("     部署完成（存在错误，请查看上方消息）");
            Console.WriteLine("==============================================");
            Console.WriteLine("pyenv 与 Python 均安装于项目目录内，未写入系统");
        }
    }
}
