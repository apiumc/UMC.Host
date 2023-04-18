﻿using System;
using System.Collections;
using System.IO;
using UMC.Net;
using System.Net;
using UMC.Data;
using System.Threading;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

[assembly: UMC.Web.Mapping]
namespace UMC.Host
{
    public class Program
    {


        public static void Main(string[] args)
        { 
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var variable = System.Environment.GetEnvironmentVariable("UMC");

            if (String.IsNullOrEmpty(variable))
            {
                if (args.Length > 0)
                {
                    variable = args[0];
                }
            }
            if (String.IsNullOrEmpty(variable) == false)
            {
                switch (variable)
                {
                    case "start":
                        if (IsRun() == false)
                        {
                            StartUp(args.Length == 2 ? args[1] : String.Empty);
                        }
                        return;
                }
            }
            if (OperatingSystem.IsWindows())
            {
                if (Process.GetCurrentProcess().MainModule.FileName.Contains("\\Local\\Temp\\"))
                {
                    Console.WriteLine("Apiumc网关不能在临时文件夹中运行，请转移到非临时文件夹中。");
                    return;
                }
            }
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("#  [start] \t开启Apiumc网关服务");
            Console.WriteLine("#  [http  0-9] \t重置Http服务端口");
            Console.WriteLine("#  [https 0-9] \t重置Https服务端口");
            Console.WriteLine("#  [ssl   a-z] \t申请SSL/TLS证书");
            Console.WriteLine("#  [stop] \t停止Apiumc网关服务");
            Console.WriteLine("#  [vpn] \t查看Web VPN服务");
            Console.WriteLine("#  [vpn start] \t开启Web VPN服务");
            Console.WriteLine("#  [vpn stop] \t停止Web VPN服务");
            Console.WriteLine("#  [exit] \t退出指令程序");

            Console.WriteLine();
            Console.ResetColor();

            // var p = Assembly.GetEntryAssembly().GetCustomAttributes().First(r => r is System.Reflection.AssemblyInformationalVersionAttribute) as System.Reflection.AssemblyInformationalVersionAttribute;
            // Console.WriteLine($"运行版本：{p.InformationalVersion}");
            ConfigDbProvider();


            Start();

            Write("info");

            while (true)
            {
                var cmds = Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (cmds.Length > 0)
                {
                    if (cmds[0] == "exit")
                    {
                        break;
                    }
                    Command(cmds);

                }
            }
        }

        public static bool IsRun()
        {
            var ls = Utility.Reader(UMC.Data.Utility.MapPath(".lock"));
            if (String.IsNullOrEmpty(ls) == false)
            {
                var hs = JSON.Deserialize<Hashtable>(ls);
                if (hs != null)
                {
                    var id = hs["Id"] as string;
                    try
                    {
                        var prc = System.Diagnostics.Process.GetProcessById(Convert.ToInt32(id));
                        if (prc != null)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }




        static void Write(params string[] args)
        {
            try
            {
                var key = UMC.Data.Utility.Parse36Encode(UMC.Data.Utility.IntParse(new Guid(UMC.Data.Utility.MD5(UMC.Data.Utility.MapPath("~"))))); ;
                using (var pipeClient = new NamedPipeClientStream(".", $"UMC.{key}", PipeDirection.InOut))
                {


                    pipeClient.Connect(10000);

                    pipeClient.Write(System.Text.Encoding.UTF8.GetBytes(String.Join(" ", args)));
                    var ls = new byte[0x200];
                    int size = ls.Length;
                    int index = 0;
                    int total = 0;
                    int start = 0;
                    do
                    {
                        total = pipeClient.Read(ls, index, size) + index;

                        for (var i = 0; i < total; i++)
                        {
                            switch (ls[i])
                            {
                                case 10:
                                    if (start < i)
                                    {

                                        Console.Write(System.Text.Encoding.UTF8.GetString(ls, start, i - start));

                                    }
                                    start = i + 1;
                                    Console.ResetColor();
                                    Console.WriteLine();
                                    break;
                                case 12:
                                    if (start < i)
                                    {
                                        Console.Write(System.Text.Encoding.UTF8.GetString(ls, start, i - start));
                                    }
                                    start = i + 1;
                                    Console.ResetColor();
                                    break;
                                case 7:
                                    if (start < i)
                                    {
                                        Console.Write(System.Text.Encoding.UTF8.GetString(ls, start, i - start));
                                    }
                                    start = i + 1;
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    break;
                                case 8:
                                    if (start < i)
                                    {
                                        Console.Write(System.Text.Encoding.UTF8.GetString(ls, start, i - start));
                                    }
                                    start = i + 1;
                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    break;
                            }
                        }

                        if (start < total)
                        {
                            Array.Copy(ls, start, ls, 0, total - start);
                            index = total - start;
                            size = ls.Length - index;
                            start = 0;
                        }
                    }
                    while (total > index);
                    if (start < total)
                    {
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(ls, start, total - start));
                    }
                    Console.WriteLine();


                }
            }
            catch
            {
                Console.WriteLine("指令接收失败，请重新输入。");
                // Start();

            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("指令：");
            Console.ResetColor();


        }
        static void Command(string[] args)
        {
            switch (args[0])
            {
                case "stop":
                    if (IsRun())
                    {
                        Write(args);
                    }
                    else
                    {
                        Console.WriteLine("Apiumc网关服务未开启，请开启。");
                    }
                    break;
                case "ssl":
                case "vpn":
                case "clear":
                case "https":
                case "http":
                case "pool":
                    if (IsRun())
                    {
                        Write(args);
                    }
                    else
                    {
                        Console.WriteLine("Apiumc网关服务未开启，请开启。");
                    }
                    break;
                case "start":
                    if (IsRun() == false)
                    {
                        Start();

                        Write("info");
                    }
                    else
                    {
                        Write(args);
                    }
                    break;
                default:
                    Console.WriteLine($"不能识别指令：[{args[0]}]");
                    break;
            }
        }
        static void Start()
        {
            if (IsRun() == false)
            {
                Excel("start");
            }
        }
        static void Excel(string arg)
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            if (String.Equals(process.ProcessName.ToLower(), "dotnet"))
            {
                var file = Environment.GetCommandLineArgs()[0];
                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.Environment.Add("UMC", arg);
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = false; ;
                startInfo.Arguments = System.IO.Path.GetFileName(file);
                startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(file);


                Process.Start(startInfo);

            }
            else
            {
                var file = process.MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(System.IO.Path.GetFileName(file));
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.Environment.Add("UMC", arg);
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = false;
                startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(file);
                Process.Start(startInfo);

            }
        }
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UMC.Data.Utility.Error("Unhandled", DateTime.Now, $"{e.ExceptionObject}");

        }
        static void ConfigDbProvider()
        {
            var urlKey = RuntimeInformation.RuntimeIdentifier;
            var native = "/native/cavif";

            if (OperatingSystem.IsWindows())
            {
                native = "/native/cavif.exe";

                if (urlKey.EndsWith("x86"))
                {
                    urlKey = "win-x86";

                }
                else if (urlKey.EndsWith("x64"))
                {
                    urlKey = "win-x64";
                }
                else if (urlKey.EndsWith("arm64"))
                {
                    urlKey = "win-arm64";
                }
                else if (urlKey.EndsWith("arm"))
                {
                    urlKey = "win-arm";

                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                urlKey = "osx-x64";
            }
            else if (OperatingSystem.IsLinux())
            {
                if (urlKey.EndsWith("musl-x64"))
                {
                    urlKey = "linux-musl-x64";

                }
                else if (urlKey.EndsWith("x64"))
                {
                    urlKey = "linux-x64";
                }
                else if (urlKey.EndsWith("arm64"))
                {
                    urlKey = "linux-arm64";
                }
                else if (urlKey.EndsWith("arm"))
                {
                    urlKey = "linux-arm";

                }
            }
            if (String.IsNullOrEmpty(urlKey) == false)
            {
                var file = UMC.Data.Utility.MapPath(native);
                if (System.IO.File.Exists(file) == false)
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    var url = new Uri($"https://wdk.oss-accelerate.aliyuncs.com/AppResources/{urlKey}.zip");
                    var downloadFile = file + ".download";
                    url.WebRequest().Get(r =>
                    {
                        if (r.StatusCode == HttpStatusCode.OK)
                        {
                            var count = r.ContentLength;
                            int size = 0;
                            var stream = Utility.Writer(downloadFile, false);
                            r.ReadAsData((b, c, l) =>
                            {
                                size += l;
                                Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
                                if (b.Length == 0)
                                {
                                    Console.WriteLine("下载完成");
                                    Console.WriteLine();

                                    stream.Flush();
                                    stream.Close();
                                    stream.Dispose();

                                    System.IO.Compression.ZipFile.ExtractToDirectory(downloadFile, UMC.Data.Utility.MapPath("/"), true);
                                    if (native.EndsWith("exe"))
                                    {
                                        Utility.Move(UMC.Data.Utility.MapPath("/cavif.exe"), file);
                                    }
                                    else
                                    {
                                        Utility.Move(UMC.Data.Utility.MapPath("/cavif"), file);
                                    }

                                    File.Delete(downloadFile);
                                    mre.Set();
                                }
                                else
                                {
                                    Console.Write($"正在下载图片组件{size * 100 / count:0}%");

                                    stream.Write(b, c, l);
                                }
                            });
                        }
                        else
                        {
                            Console.WriteLine("图片组件下载失败");
                            mre.Set();
                        }
                    });
                    mre.WaitOne();
                }
            }
        }
        public static void Register()
        {
            UMC.Web.WebClient.Register(() => new UMC.Proxy.WebFactory());
            UMC.Web.WebClient.Register(() => new UMC.Activities.AccountFlow());
            UMC.Web.WebClient.Register(() => new UMC.Activities.SettingsFlow());
            UMC.Web.WebClient.Register(() => new UMC.Activities.DesignFlow());
            UMC.Web.WebClient.Register(() => new UMC.Activities.UIFlow());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemFlow());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemSetupActivity());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemCellActivity());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemImageActivity());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemLinkActivity());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemPictureActivity());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemResourceActivity());
            UMC.Web.WebClient.Register(() => new UMC.Web.Activity.SystemScanningActivity());
            UMC.Web.WebClient.Register(() => new UMC.Host.HttpBridgeActivity());
            UMC.Web.WebClient.Register(() => new UMC.Activities.SettingsAreaActivity());
            UMC.Web.WebClient.Register(() => new UMC.Activities.SettingsSelectOrganizeActivity());
            UMC.Web.WebClient.Register(() => new UMC.Activities.SettingsSelectRoleActivity());
            UMC.Web.WebClient.Register(() => new UMC.Activities.SettingsSelectUserActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.SiteLogConfActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteAppActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteAuthActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteConfActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteConfImageActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteLogActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteMimeActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteUserActivity());
            UMC.Web.WebClient.Register(() => new UMC.Proxy.Activities.SiteServerActivity());

        }
        static void StartUp(string path)
        {
            Register();
            if (String.IsNullOrEmpty(path) == false)
            {
                UMC.Data.Reflection.Instance().SetBaseDirectory(path);
            }

            File.WriteAllText(UMC.Data.Utility.MapPath($".lock"), $"{{\"Id\":\"{System.Diagnostics.Process.GetCurrentProcess().Id}\"}}");

            // HotCache.Namespace("UMC.Proxy.Entities");
            UMC.Activities.DataFactory.Instance();
            UMC.Proxy.DataFactory.Instance();
            UMC.Data.DataFactory.Instance();
            HttpMimeServier.Start();

        }

    }
}
