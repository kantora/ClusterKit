﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="ClusterKit">
//   All rights reserved
// </copyright>
// <summary>
//   ClusterKit Node launcher
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterKit.NodeManager.Launcher
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml;

    using ClusterKit.NodeManager.Launcher.Messages;

    using RestSharp;

    /// <summary>
    /// ClusterKit Node launcher
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Program"/> class from being created.
        /// </summary>
        private Program()
        {
            if (!File.Exists("nuget.exe"))
            {
                Console.WriteLine("nuget.exe not found");
                this.IsValid = false;
            }

            EnStartMode startMode;
            if (Enum.TryParse(ConfigurationManager.AppSettings["startMode"], out startMode))
            {
                this.StartMode = startMode;
            }

            EnStopMode stopMode;
            if (Enum.TryParse(ConfigurationManager.AppSettings["stopMode"], out stopMode))
            {
                this.StopMode = stopMode;
            }

            if (stopMode == EnStopMode.RunAction
                && string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["stopAction"]))
            {
                Console.WriteLine("stopAction is not configured");
                this.IsValid = false;
            }

            try
            {
                this.ConfigurationUrl = new Uri(ConfigurationManager.AppSettings["nodeManagerUrl"]);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("nodeManagerUrl not configured");
                this.IsValid = false;
            }
            catch (UriFormatException)
            {
                Console.WriteLine("nodeManagerUrl is not a valid URL");
                this.IsValid = false;
            }

            this.WorkingDirectory = ConfigurationManager.AppSettings["workingDirectory"];
            if (string.IsNullOrWhiteSpace(this.WorkingDirectory))
            {
                Console.WriteLine("workingDirectory is not configured");
                this.IsValid = false;
            }
            else if (!Directory.Exists(this.WorkingDirectory))
            {
                try
                {
                    Directory.CreateDirectory(this.WorkingDirectory);
                }
                catch (Exception)
                {
                    Console.WriteLine("workingDirectory does not extist");
                    this.IsValid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(this.WorkingDirectory)
                && Directory.Exists(this.WorkingDirectory)
                && !CheckDirectoryAccess(this.WorkingDirectory))
            {
                Console.WriteLine("workingDirectory is not accessable");
                this.IsValid = false;
            }

            this.ContainerType = ConfigurationManager.AppSettings["containerType"];
            if (string.IsNullOrWhiteSpace(this.WorkingDirectory))
            {
                Console.WriteLine("containerType is not configured");
                this.IsValid = false;
            }
        }

        /// <summary>
        /// First start mode
        /// </summary>
        public enum EnStartMode
        {
            /// <summary>
            /// Will request cluster for node configuration
            /// </summary>
            CleanStart,

            /// <summary>
            /// At first will start node as is. In case of restart - will upgrade node according to configuration
            /// </summary>
            LaunchPredefinedNode
        }

        /// <summary>
        /// Action to be done after node stops
        /// </summary>
        public enum EnStopMode
        {
            /// <summary>
            /// Will clean node data and request cluster for new node configuration
            /// </summary>
            CleanRestart,

            /// <summary>
            /// Will run action defined in Config
            /// </summary>
            RunAction
        }

        /// <summary>
        /// Url of cluster configuration service
        /// </summary>
        public Uri ConfigurationUrl { get; }

        /// <summary>
        /// Type of container assigned to current machine
        /// </summary>
        public string ContainerType { get; }

        /// <summary>
        /// Gets the value indicating that configuration is correct
        /// </summary>
        public bool IsValid { get; } = true;

        /// <summary>
        /// First start mode
        /// </summary>
        public EnStartMode StartMode { get; } = EnStartMode.CleanStart;

        /// <summary>
        /// Action to be done after node stops
        /// </summary>
        public EnStopMode StopMode { get; } = EnStopMode.CleanRestart;

        public string WorkingDirectory { get; }

        /// <summary>
        /// Checks the ability to create and write to a file in the supplied directory.
        /// </summary>
        /// <param name="directory">String representing the directory path to check.</param>
        /// <returns>True if successful; otherwise false.</returns>
        private static bool CheckDirectoryAccess(string directory)
        {
            bool success = false;
            string fullPath = Path.Combine(directory, "tmp.tmp");

            if (Directory.Exists(directory))
            {
                try
                {
                    using (FileStream fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        fs.WriteByte(0xff);
                    }

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Launcher main entry point
        /// </summary>
        private static void Main()
        {
            var program = new Program();
            if (program.IsValid)
            {
                program.Start();
            }
        }

        /// <summary>
        /// Removes all files and subdirectories from working directory
        /// </summary>
        private void CleanWorkingDir()
        {
            foreach (var dir in Directory.GetDirectories(this.WorkingDirectory))
            {
                Directory.Delete(dir, true);
            }

            foreach (var file in Directory.GetFiles(this.WorkingDirectory))
            {
                File.Delete(file);
            }
        }

        /// <summary>
        /// Prepares all node software for launch
        /// </summary>
        private void ConfigureNode()
        {
            Console.WriteLine("Configuring node...");

            var client = new RestClient(this.ConfigurationUrl);
            var request = new RestRequest { Method = Method.POST };
            request.AddBody(new NewNodeTemplateRequest { ContainerType = this.ContainerType });

            var config = client.Execute<NodeStartUpConfiguration>(request);

            while (config.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                var retryHeader = config.Headers.FirstOrDefault(
                    h => "Retry - After".Equals(h.Name, StringComparison.InvariantCultureIgnoreCase));
                int timeToWait;
                if (!int.TryParse(retryHeader?.Value?.ToString(), out timeToWait))
                {
                    timeToWait = 60;
                }

                Console.WriteLine($"Config service anavailable, retrying in {timeToWait} seconds...");
                Thread.Sleep(TimeSpan.FromSeconds(timeToWait));
                config = client.Execute<NodeStartUpConfiguration>(request);
            }

            if (config.StatusCode != HttpStatusCode.OK || config.Data == null)
            {
                throw new Exception("Could not get configuration from service");
            }

            Console.WriteLine($"Got {config.Data.NodeTemplate} configuration");
            this.CleanWorkingDir();
            this.PrepareNuGetConfig(config.Data);
            this.InstallPackages(config.Data);
            this.CreateService(config.Data);
            this.FixAssemblyVersions();
        }

        private void CreateService(NodeStartUpConfiguration configuration)
        {
            Console.WriteLine(@"Creating service");
            var serviceDir = Path.Combine(this.WorkingDirectory, "service");
            Directory.CreateDirectory(serviceDir);

            var matches = new[]
                              {
                                  new Regex("(^|\\+)net45", RegexOptions.IgnoreCase),
                                  new Regex("(^|\\+)net40", RegexOptions.IgnoreCase),
                                  new Regex("(^|\\+)net35", RegexOptions.IgnoreCase),
                                  new Regex("(^|\\+)net20", RegexOptions.IgnoreCase),
                                  new Regex("(^|\\+)dotnet", RegexOptions.IgnoreCase),
                                  new Regex("(^|\\+)netcore45", RegexOptions.IgnoreCase),
                              };

            foreach (var directory in Directory.GetDirectories(Path.Combine(this.WorkingDirectory, "packages")))
            {
                if (!Directory.Exists(Path.Combine(directory, "lib")))
                {
                    continue;
                }

                var specificDirs = Directory.GetDirectories(Path.Combine(directory, "lib")).Select(d => d.Split(Path.DirectorySeparatorChar).Last()).ToList();

                var endDir =
                    matches
                        .Select(m => specificDirs.FirstOrDefault(d => m.IsMatch(d)))
                        .FirstOrDefault(d => d != null);

                endDir = endDir != null ? Path.Combine(directory, "lib", endDir) : Path.Combine(directory, "lib");
                Console.WriteLine($"Installing {Path.GetDirectoryName(directory)} from {endDir}");
                foreach (var file in Directory.GetFiles(endDir))
                {
                    File.Copy(file, Path.Combine(this.WorkingDirectory, "service", Path.GetFileName(file)), true);
                }
            }

            File.WriteAllText(Path.Combine(serviceDir, "akka.hocon"), configuration.Configuration);

            string startConfig = $@"{{
                ClusterKit.NodeManager.NodeTemplate = {configuration.NodeTemplate}
                ClusterKit.NodeManager.ContainerType = {this.ContainerType}
                ClusterKit.NodeManager.RequestId = {configuration.RequestId}
                akka.cluster.seed-nodes = [{string.Join(", ", configuration.Seeds.Select(s => $"\"{s}\""))}]
            }}";
            File.WriteAllText(Path.Combine(serviceDir, "start.hocon"), startConfig);
        }

        private void FixAssemblyVersions()
        {
            var dirName = Path.Combine(this.WorkingDirectory, "service");
            var configName = Path.Combine(dirName, "ClusterKit.Core.Service.exe.config");

            XmlDocument confDoc = new XmlDocument();
            confDoc.Load(configName);
            var documentElement = confDoc.DocumentElement;
            if (documentElement == null)
            {
                Console.WriteLine($"Configuration file {configName} is broken");
                return;
            }

            documentElement = (XmlElement)documentElement.SelectSingleNode("/configuration");
            if (documentElement == null)
            {
                Console.WriteLine($"Configuration file {configName} is broken");
                return;
            }

            var runTimeNode = documentElement.SelectSingleNode("./runtime")
                              ?? documentElement.AppendChild(confDoc.CreateElement("runtime"));

            var nameTable = confDoc.NameTable;
            var namespaceManager = new XmlNamespaceManager(nameTable);
            const string uri = "urn:schemas-microsoft-com:asm.v1";
            namespaceManager.AddNamespace("urn", uri);

            var assemblyBindingNode = runTimeNode.SelectSingleNode("./urn:assemblyBinding", namespaceManager)
                                      ?? runTimeNode.AppendChild(confDoc.CreateElement("assemblyBinding", uri));

            foreach (var lib in Directory.GetFiles(dirName, "*.dll"))
            {
                var parameters = AssemblyName.GetAssemblyName(lib);
                var dependentNode =
                    assemblyBindingNode.SelectSingleNode($"./urn:dependentAssembly[./urn:assemblyIdentity/@name='{parameters.Name}']", namespaceManager)
                    ?? assemblyBindingNode.AppendChild(confDoc.CreateElement("dependentAssembly", uri));

                dependentNode.RemoveAll();
                var assemblyIdentityNode = (XmlElement)dependentNode.AppendChild(confDoc.CreateElement("assemblyIdentity", uri));
                assemblyIdentityNode.SetAttribute("name", parameters.Name);
                assemblyIdentityNode.SetAttribute("publicKeyToken", BitConverter.ToString(parameters.GetPublicKeyToken()).Replace("-", "").ToLower(CultureInfo.InvariantCulture));
                var bindingRedirectNode = (XmlElement)dependentNode.AppendChild(confDoc.CreateElement("bindingRedirect", uri));
                bindingRedirectNode.SetAttribute("oldVersion", $"0.0.0.0-{parameters.Version}");
                bindingRedirectNode.SetAttribute("newVersion", parameters.Version.ToString());

                // Console.WriteLine($"{parameters.Name} {parameters.Version} {BitConverter.ToString(parameters.GetPublicKeyToken()).Replace("-", "").ToLower(CultureInfo.InvariantCulture)}");
                Console.WriteLine($"{parameters.Name} {parameters.Version}");
            }

            confDoc.Save(configName);
        }

        private void InstallPackages(NodeStartUpConfiguration configuration)
        {
            foreach (var package in configuration.Packages)
            {
                Console.WriteLine($"Downloading {package}...");
                var process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WorkingDirectory = Path.GetFullPath(this.WorkingDirectory);
                process.StartInfo.FileName = "nuget.exe";
                process.StartInfo.Arguments = $"install {package} -PreRelease -NonInteractive -ConfigFile nuget.config -OutputDirectory packages -DisableParallelProcessing";
                process.Start();
                process.WaitForExit();
                process.Dispose();
            }
        }

        /// <summary>
        /// Starts node software
        /// </summary>
        private void LaunchNode()
        {
            Console.WriteLine(@"Starting node");
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = Path.Combine(Path.GetFullPath(this.WorkingDirectory), "service");
            process.StartInfo.FileName = Path.Combine(Path.GetFullPath(this.WorkingDirectory), "service", "ClusterKit.Core.Service.exe");
            process.StartInfo.Arguments = "-config:start.hocon";
            process.Start();

            process.WaitForExit();
            process.Dispose();
        }

        /// <summary>
        /// Sets NuGet configuration file from parameters
        /// </summary>
        /// <param name="configuration">Current node configuration</param>
        private void PrepareNuGetConfig(NodeStartUpConfiguration configuration)
        {
            var configPath = Path.Combine(this.WorkingDirectory, "nuget.config");

            File.Copy(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "nuget.exe"),
                Path.Combine(this.WorkingDirectory, "nuget.exe"));

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Resources.NugetConfig);
            var root = doc.SelectSingleNode("/configuration/packageSources");

            int index = 0;
            foreach (var packageSource in configuration.PackageSources)
            {
                var addNode = doc.CreateElement("add");
                addNode.SetAttribute("key", $"s{index++}");
                addNode.SetAttribute("value", packageSource);
                root?.AppendChild(addNode);
            }

            doc.Save(configPath);
        }

        /// <summary>
        /// Starts the new session
        /// </summary>
        private void Start()
        {
            switch (this.StopMode)
            {
                case EnStopMode.CleanRestart:
                    if (this.StartMode == EnStartMode.LaunchPredefinedNode)
                    {
                        try
                        {
                            this.LaunchNode();
                        }
                        catch (ThreadAbortException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine($"Failed to start in predefined mode, {exception.Message}");
                            Console.WriteLine(exception.StackTrace);
                        }
                    }

                    while (true)
                    {
                        try
                        {
                            this.ConfigureNode();
                            this.LaunchNode();
                        }
                        catch (ThreadAbortException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine($"Failed to launch, {exception.Message}");
                            Console.WriteLine(exception.StackTrace);
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        }
                    }
                default:
                    Console.WriteLine("Unsupported stop mode");
                    return;
            }
        }
    }
}