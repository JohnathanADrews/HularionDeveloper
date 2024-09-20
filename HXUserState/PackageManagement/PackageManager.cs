#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionExperience.Definition.Package;
using HularionMesh.DomainValue;
using HularionMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HularionMesh.SystemDomain;
using HularionText.Language.Json;
using HularionExperience.Definition;
using HularionExperience.Definition.PackageServer;
using HularionExperience.PackageBuilder;
using HularionMesh.SystemDomain.Active;
using HularionCore.Pattern.Topology;
using HularionExperience.PackageBuilder.Storage;
using static HXUserState.Plugin.State.Routes.Response.GetSourcesPackagesResponse;
using System.IO;
using HularionExperience.Definition.PackageClient;
using System.Reflection;
using HularionCore.Pattern.Identifier;
using HularionPlugin.Route;
using HularionExperience;
using HXUserState.State.Mesh;
using HXUserState.State;
using HularionExperience.Client;

namespace HXUserState.PackageManagement
{
    public class PackageManager 
    {



        private Dictionary<string, HXPartial> systemPackages { get; set; } = new Dictionary<string, HXPartial>();

        private Dictionary<string, Dictionary<string, HXPartial>> packageStore { get; set; } = new Dictionary<string, Dictionary<string, HXPartial>>();

        private IHXStateStore stateStore;

        public string BaseDirectory { get; set; }

        private JsonSerializer serializer = new(new HularionText.StringCase.StringCaseModifier(HularionText.StringCase.StringCaseDefinition.StartLower));

        /// <summary>
        /// key: parent diretory, value: { key: packageKey, value: project directory }
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> projectDirectories = new();

        private HularionRouter router;

        public PackageManager(IHXStateStore stateStore, string baseDirectory, HularionRouter router)
        {
            this.stateStore = stateStore;
            BaseDirectory = string.Format("{0}", baseDirectory).Trim(new char[] { ' ', '\t', '\n', '\r', '\\' });
            this.router = router;
        }


        public ProvidedPackage GetPackage(PackageIndicator indicator)
        {
            var package = new ProvidedPackage();

            package.Partial = GetPartial(indicator);
            if(package.Partial == null)
            {
                GetPartial(indicator);
                return null;
            }
            if (!systemPackages.ContainsKey(indicator.PackageKey))
            {
                package.Partial.ClientPackage.RouteInformation.RouteKey = string.Format(@"{0}{1}", ObjectKey.CreateUniqueTag(), ObjectKey.CreateUniqueTag()).ToLower();
            }
            var serverDirectory = GetServerFilesDirectory(package.Partial.PackageKey, package.Partial.Version);
            if (indicator.IsProject)
            {
                serverDirectory = indicator.ProjectLocation;
            }
            foreach (var provider in package.Partial.ServerPackage.RouteProviders)
            {
                try
                {
                    var path = string.Format(@"{0}\{1}\{2}", Environment.CurrentDirectory, serverDirectory, provider.FilePath);
                    if (indicator.IsProject)
                    {
                        var parentDirectory = indicator.RootLocation.Substring(0, indicator.RootLocation.LastIndexOf(@"\"));
                        path = string.Format(@"{0}\{1}", projectDirectories[parentDirectory][indicator.PackageKey], provider.FilePath);
                    }
                    var assembly = Assembly.LoadFile(path);
                    var assemblyTypes = assembly.GetTypes().ToDictionary(x => x.FullName, x => x);
                    var prefixedProvider = new RouterProvider();
                    //var prefixedProvider = new RouterProvider(routeProvider, partial.ClientPackage.routeInformation.routeKey);
                    foreach (var providerType in provider.ProviderTypes)
                    {
                        if (!assemblyTypes.ContainsKey(providerType))
                        {
                            continue;
                        }
                        var routeProvider = (IRouteProvider)assembly.CreateInstance(providerType);
                        package.RouteProviders.Add(routeProvider);
                    }
                    router.RegisterRouteProvider(prefixedProvider);
                }
                catch
                {
                    //load issue.
                }
            }
            return package;
        }

        public void AddSystemPackages(params HXPartial[] packages)
        {
            lock (systemPackages)
            {
                foreach (var package in packages)
                {
                    systemPackages[package.PackageKey] = package;
                }
            }
        }

        public void AddPackages(params HXPartial[] packages)
        {
            lock (packageStore)
            {
                foreach (var package in packages)
                {
                    if (!packageStore.ContainsKey(package.PackageKey))
                    {
                        packageStore.Add(package.PackageKey, new Dictionary<string, HXPartial>());
                    }
                    if (FormatString(package.ClientPackage.Stage) != FormatString(HXPackageStage.Package.ToString()))
                    {
                        packageStore[package.PackageKey][HularionExperienceKeyword.HXProjectPackageVersionIndicator.Value] = package;
                        continue;
                    }
                    packageStore[package.PackageKey][FormatString(package.Version)] = package;
                }
            }
        }

        public void LoadContextPackages(IMeshKey contextKey)
        {
            var context = stateStore.StateRepository.QueryTree<HXContext>(contextKey).First;
            var packages = context.Packages.ToList();

            foreach (var package in packages)
            {
                LoadPartial(package.PackageKey, package.Version);
            }

        }

        public ClientPackage GetClientPackage(PackageIndicator indicator)
        {

            if (systemPackages.ContainsKey(indicator.PackageKey))
            {
                return systemPackages[indicator.PackageKey].ClientPackage;
            }

            indicator.Version = FormatString(indicator.Version);
            ClientPackage client = null;
            var packageBuilder = new HularionPackageBuilder();
            if (indicator.IsProject)
            {
                if (!string.IsNullOrWhiteSpace(indicator.ProjectLocation))
                {
                    var packages = packageBuilder.ResouresToPackageMetaTransform.Transform(indicator.ProjectLocation);
                    return packages.First().Package.ClientPackage;
                }
                else if (!string.IsNullOrWhiteSpace(indicator.RootLocation) && indicator.RootLocation.Trim('\\').Contains('\\'))
                {
                    var parent = indicator.RootLocation.Trim('\\');
                    parent = parent.Substring(0, parent.LastIndexOf(@"\"));

                    if (projectDirectories.ContainsKey(parent) && projectDirectories[parent].ContainsKey(indicator.PackageKey))
                    {
                        var package = packageBuilder.DirectoryToHXPackageTransform.Transform(projectDirectories[parent][indicator.PackageKey]).FirstOrDefault();
                        return package.ClientPackage;
                    }
                    else
                    {
                        lock (projectDirectories)
                        {
                            if (!projectDirectories.ContainsKey(parent))
                            {
                                projectDirectories.Add(parent, new Dictionary<string, string>());
                            }
                        }
                        var directories = Directory.GetDirectories(parent).Where(x => Directory.GetFiles(x).Where(x => x.Contains(HularionExperienceKeyword.HXProjectName.Value)).Count() > 0).ToList();
                        foreach (var directory in directories)
                        {
                            var package = packageBuilder.DirectoryToHXPackageTransform.Transform(directory).FirstOrDefault();
                            if (package != null)
                            {
                                projectDirectories[parent][package.Key] = directory;
                                if (package.Key == indicator.PackageKey)
                                {
                                    client = package.ClientPackage;
                                }
                            }
                        }
                        if (client != null)
                        {
                            return client;
                        }
                    }

                }

            }

            //installed package
            {
                var package = GetPackage(indicator.PackageKey, indicator.Version);
                if (package == null)
                {
                    return null;
                }
                client = package.ClientPackage;
            }

            return client;
        }

        public HXPartial GetPartial(PackageIndicator indicator)
        {

            if (systemPackages.ContainsKey(indicator.PackageKey))
            {
                return systemPackages[indicator.PackageKey];
            }

            indicator.Version = FormatString(indicator.Version);
            HXPartial partial = null;
            var packageBuilder = new HularionPackageBuilder();
            if (indicator.IsProject)
            {
                //if (!String.IsNullOrWhiteSpace(indicator.ProjectLocation))
                //{
                //    var packages = packageBuilder.ResouresToPackageMetaTransform.Transform(indicator.ProjectLocation);
                //    partial = packages.First().CreatePartial();
                //    return partial;
                //}
                if (!string.IsNullOrWhiteSpace(indicator.RootLocation) && indicator.RootLocation.Trim('\\').Contains(@"\"))
                {
                    var parent = indicator.RootLocation.Trim('\\');
                    parent = parent.Substring(0, parent.LastIndexOf(@"\"));

                    if (projectDirectories.ContainsKey(parent) && projectDirectories[parent].ContainsKey(indicator.PackageKey))
                    {
                        var package = packageBuilder.DirectoryToHXPackageTransform.Transform(projectDirectories[parent][indicator.PackageKey]).FirstOrDefault();
                        return package.GetPartial();
                    }
                    else
                    {
                        lock (projectDirectories)
                        {
                            if (!projectDirectories.ContainsKey(parent))
                            {
                                projectDirectories.Add(parent, new Dictionary<string, string>());
                            }
                        }
                        var directories = Directory.GetDirectories(parent).Where(x => Directory.GetFiles(x).Where(x => x.Contains(HularionExperienceKeyword.HXProjectName.Value)).Count() > 0).ToList();
                        foreach (var directory in directories)
                        {
                            var package = packageBuilder.DirectoryToHXPackageTransform.Transform(directory).FirstOrDefault();
                            if (package != null)
                            {
                                projectDirectories[parent][package.Key] = directory;
                                if (package.Key == indicator.PackageKey)
                                {
                                    partial = package.GetPartial();
                                }
                            }
                        }
                        if (partial != null)
                        {
                            return partial;
                        }
                    }

                }

            }

            //installed package
            partial = GetPackage(indicator.PackageKey, indicator.Version);

            return partial;
        }


        /// <summary>
        /// Gets the client package and loads any package routes.
        /// </summary>
        /// <returns>The client package.</returns>
        public HXPartial LoadPackage(PackageIndicator indicator)
        {
            var partial = GetPartial(indicator);
            if (!systemPackages.ContainsKey(indicator.PackageKey))
            {
                partial.ClientPackage.RouteInformation.RouteKey = string.Format(@"{0}{1}", ObjectKey.CreateUniqueTag(), ObjectKey.CreateUniqueTag()).ToLower();
            }
            var serverDirectory = GetServerFilesDirectory(partial.PackageKey, partial.Version);
            if (indicator.IsProject)
            {
                serverDirectory = indicator.ProjectLocation;
            }
            foreach (var provider in partial.ServerPackage.RouteProviders)
            {
                try
                {
                    var path = string.Format(@"{0}\{1}\{2}", Environment.CurrentDirectory, serverDirectory, provider.FilePath);
                    if (indicator.IsProject)
                    {
                        var parentDirectory = indicator.RootLocation.Substring(0, indicator.RootLocation.LastIndexOf(@"\"));
                        path = string.Format(@"{0}\{1}", projectDirectories[parentDirectory][indicator.PackageKey], provider.FilePath);
                    }
                    var assembly = Assembly.LoadFile(path);
                    var assemblyTypes = assembly.GetTypes().ToDictionary(x => x.FullName, x => x);
                    var prefixedProvider = new RouterProvider();
                    //var prefixedProvider = new RouterProvider(routeProvider, partial.ClientPackage.routeInformation.routeKey);
                    foreach (var providerType in provider.ProviderTypes)
                    {
                        if (!assemblyTypes.ContainsKey(providerType))
                        {
                            continue;
                        }
                        var routeProvider = (IRouteProvider)assembly.CreateInstance(providerType);
                        prefixedProvider.AddRouteProvider(routeProvider, partial.ClientPackage.RouteInformation.RouteKey);
                    }
                    router.RegisterRouteProvider(prefixedProvider);
                }
                catch
                {
                    //load issue.
                }
            }

            return partial;
        }

        public string FormatString(string version)
        {
            return string.Format("{0}", version).Trim().ToLower();
        }

        public HXPartial GetPackage(string packageKey, string version)
        {
            if (!packageStore.ContainsKey(packageKey) || (packageStore.Where(x => x.Key == version).Count() == 0))
            {
                if (!LoadPartial(packageKey, version))
                {
                    return null;
                }
            }
            //if (!packageStore.ContainsKey(packageKey) && !(packageStore.Where(x=>x.Key == version).Count() == 0  && !LoadPartial(packageKey, version))
            //{
            //    return null;
            //}
            var packageVersion = GetPackageVersion(packageKey, version);
            if (packageVersion == null)
            {
                return null;
            }
            return packageStore[packageKey][packageVersion];
        }

        public HXPartial GetPackage(HXPackageReference package)
        {
            if (!packageStore.ContainsKey(package.PackageKey) && !LoadPartial(package.PackageKey, package.Version))
            {
                return null;
            }
            var packageVersion = GetPackageVersion(package.PackageKey, package.Version);
            if (packageVersion == null)
            {
                return null;
            }
            return packageStore[packageVersion][packageVersion];
        }

        public string GetPackageVersion(string packageKey, string version)
        {
            if (!packageStore.ContainsKey(packageKey))
            {
                return null;
            }

            version = FormatString(version);
            if (string.IsNullOrWhiteSpace(version))
            {
                version = HularionExperienceKeyword.HXLatestPackageIndicator.Value;
            }

            var versions = packageStore[packageKey];
            var projectPackage = FormatString(HularionExperienceKeyword.HXProjectPackageVersionIndicator.Value);

            if (version == HularionExperienceKeyword.HXProjectPackageVersionIndicator.Value)
            {
                if (versions.ContainsKey(projectPackage))
                {
                    return projectPackage;
                }
                return null;
            }
            if (version == HularionExperienceKeyword.HXLatestPackageIndicator.Value)
            {
                lock (versions)
                {
                    string latest = null;
                    string project = null;
                    foreach (var package in versions)
                    {
                        if (latest == null && package.Key == projectPackage)
                        {
                            project = package.Key;
                            continue;
                        }
                        if (latest == null)
                        {
                            latest = package.Key;
                            continue;
                        }
                        if (GetGreaterVersion(latest, package.Key) != latest)
                        {
                            latest = package.Key;
                        }
                    }
                    latest ??= project;
                    if (latest == null)
                    {
                        return null;
                    }
                    return latest;
                }
            }

            if (versions.ContainsKey(version))
            {
                return version;
            }
            return null;
        }

        public string GetGreaterVersion(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) && !string.IsNullOrWhiteSpace(version2))
            {
                return version2;
            }
            if (string.IsNullOrWhiteSpace(version2))
            {
                return version1;
            }

            var parts1 = version1.Split('.');
            var parts2 = version2.Split('.');
            var length = parts1.Length;
            if (parts2.Length < parts1.Length)
            {
                length = parts2.Length;
            }

            int value1 = 0, value2 = 0;
            for (var i = 0; i < length; i++)
            {
                var part1 = FormatString(parts1[i]);
                var part2 = FormatString(parts2[i]);
                if (part1 == part2)
                {
                    continue;
                }
                if (int.TryParse(part1, out value1) && int.TryParse(part2, out value2))
                {
                    if (value1 > value2)
                    {
                        return version1;
                    }
                    else
                    {
                        return version2;
                    }
                }
                else
                {
                    if (new string[] { part1, part2 }.OrderBy(x => x).Last() == part1)
                    {
                        return version1;
                    }
                    else
                    {
                        return version2;
                    }
                }
            }
            return null;
        }

        public PackageInstallResult InstallPackage(IMeshKey contextKey, PackageInstallationDetail install)
        {
            var result = new PackageInstallResult();

            var packageReader = new PackageReader();
            var summary = packageReader.GetPackageSummary(install.Source.Location);
            if (install.Source.Location.EndsWith(HularionExperienceKeyword.HXPackageExtension.Value))
            {
                install.Source.Location = install.Source.Location.Substring(0, install.Source.Location.LastIndexOf(@"\"));
            }
            var dependencies = GetDependencies(summary.PackageKey, summary.Version, new PackageSource[] { install.Source });
            var failed = dependencies.Where(x => x.FailedToFind).ToList();
            if (failed.Count > 0)
            {
                result.WasSuccessful = false;
                var message = new StringBuilder();
                message.Append("Failed ot find the following necessary packages");
                foreach (var item in failed)
                {
                    message.Append(item.GetLowerFilename());
                    message.Append("\n");
                }
                result.Message = message.ToString();
                return result;
            }


            var context = stateStore.StateRepository.QueryTree<HXContext>(contextKey).First;
            var activeSet = new ActiveUniqueSet<HXPackageReference>(context.Packages.Key, stateStore.StateRepository, stateStore.UserProfile);
            var packageSet = stateStore.StateRepository.QueryTree<UniqueSet<HXPackageReference>>(context.Packages.Key).First.ToList();

            foreach (var dependency in dependencies)
            {
                var packageDirectory = GetPackageDirectory(dependency.Summary.PackageKey, dependency.Summary.Version);
                var sourceDirectory = string.Format(@"{0}\{1}", packageDirectory, HularionExperienceKeyword.HXSourcePackagesDirectory.Value);
                Directory.CreateDirectory(sourceDirectory);

                var packageLocation = string.Format(@"{0}\{1}", sourceDirectory, dependency.PackageFilename);
                if (File.Exists(packageLocation))
                {
                    result.WasAlreadyInstalled = true;
                }

                if (!result.WasAlreadyInstalled)
                {
                    File.Copy(dependency.Source, packageLocation);

                    var package = packageReader.ReadPackage(dependency.Source);
                    var partialDirectory = GetPartialDirectory(dependency.PackageKey, dependency.PackageVersion);
                    Directory.CreateDirectory(partialDirectory);
                    package.ClientPackage.Stage = HXPackageStage.Package.ToString();
                    var clientJson = serializer.Serialize(package.ClientPackage);
                    File.WriteAllText(string.Format(@"{0}\{1}.{2}", partialDirectory, HularionExperienceKeyword.HXClientPartialExtension.Value, HularionExperienceKeyword.HXPartialExtension.Value), clientJson);
                    var serverJson = serializer.Serialize(package.ServerPackage);
                    File.WriteAllText(string.Format(@"{0}\{1}.{2}", partialDirectory, HularionExperienceKeyword.HXServerPartialExtension.Value, HularionExperienceKeyword.HXPartialExtension.Value), serverJson);
                    var summaryJson = serializer.Serialize(summary);
                    File.WriteAllText(string.Format(@"{0}\{1}.{2}", partialDirectory, HularionExperienceKeyword.HXSummaryPartialExtension.Value, HularionExperienceKeyword.HXPartialExtension.Value), summaryJson);

                    var serverDirectory = string.Format(@"{0}\{1}", packageDirectory, HularionExperienceKeyword.HXServerPackagesDirectory.Value);
                    foreach (var file in package.ServerBinaryPackage.ServerFiles)
                    {
                        var serverFilename = string.Format(@"{0}\{1}", serverDirectory, file.Filename);
                        var fileDirectory = serverFilename.Substring(0, serverFilename.LastIndexOf(@"\"));
                        Directory.CreateDirectory(fileDirectory);
                        File.WriteAllBytes(serverFilename, file.Blob);
                    }
                }

                if (packageSet.Where(x => x.PackageKey == dependency.PackageKey && x.Version == dependency.PackageVersion).Count() == 0)
                {
                    var packageReference = new HXPackageReference() { Stage = HXPackageStage.Package.ToString(), PackageKey = dependency.PackageKey, Version = dependency.PackageVersion };
                    //packageReference.Locations.Add(packageDirectory);
                    stateStore.StateRepository.Save(stateStore.UserProfile, packageReference);
                    activeSet.Add(packageReference);
                }
            }

            return result;
        }

        public bool LoadPartial(string packageKey, string version)
        {
            try
            {
                var partial = new HXPartial()
                {
                    ClientPackage = GetClientPartial(packageKey, version),
                    ServerPackage = GetServerPartial(packageKey, version)
                };
                partial.AssignValuesFromClient();
                AddPackages(partial);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetPackageDirectory(string packageKey, string version)
        {
            return string.Format(@"{0}\{1}\{2}\{3}", BaseDirectory, HularionExperienceKeyword.HXPackagesDirectory.Value, packageKey, version);
        }

        public string GetPartialDirectory(string packageKey, string version)
        {
            return string.Format(@"{0}\{1}", GetPackageDirectory(packageKey, version), HularionExperienceKeyword.HXPartialPackagesDirectory.Value);
        }

        public string GetClientPartialFilename(string packageKey, string version)
        {
            return string.Format(@"{0}\{1}.{2}", GetPartialDirectory(packageKey, version), HularionExperienceKeyword.HXClientPartialExtension.Value, HularionExperienceKeyword.HXPartialExtension.Value);
        }

        public string GetServerPartialFilename(string packageKey, string version)
        {
            return string.Format(@"{0}\{1}.{2}", GetPartialDirectory(packageKey, version), HularionExperienceKeyword.HXServerPartialExtension.Value, HularionExperienceKeyword.HXPartialExtension.Value);
        }

        public string GetServerFilesDirectory(string packageKey, string version)
        {
            return string.Format(@"{0}\{1}", GetPackageDirectory(packageKey, version), HularionExperienceKeyword.HXServerPackagesDirectory.Value);
        }

        public ClientPackage GetClientPartial(string packageKey, string version)
        {
            var filename = GetClientPartialFilename(packageKey, version);
            var json = File.ReadAllText(filename);
            var partial = serializer.Deserialize<ClientPackage>(json);
            return partial;
        }

        public ServerPackage GetServerPartial(string packageKey, string version)
        {
            var filename = GetServerPartialFilename(packageKey, version);
            var json = File.ReadAllText(filename);
            var partial = serializer.Deserialize<ServerPackage>(json);
            return partial;
        }

        public PackageInstallDependency[] GetDependencies(string packageKey, string packageVersion, PackageSource[] sources)
        {

            var packages = new Dictionary<string, string>();
            var sourceMap = new Dictionary<string, string>();
            foreach (var source in sources)
            {
                if (source.Structure == PackageSourceStructure.Disk && Directory.Exists(source.Location))
                {
                    var packageFiles = Directory.GetFiles(source.Location).Where(x => x.EndsWith(HularionExperienceKeyword.HXPackageExtension.Value)).Select(x => x.Trim().ToLower()).ToList();
                    foreach (var file in packageFiles)
                    {
                        var filename = file;
                        if (filename.Contains(@"\"))
                        {
                            filename = file.Substring(filename.LastIndexOf(@"\") + 1);
                        }
                        packages[filename] = file;
                        sourceMap[file] = filename;
                    }
                }
            }

            var dependency = new PackageInstallDependency()
            {
                PackageKey = packageKey,
                PackageVersion = packageVersion
            };

            var traverser = new TreeTraverser<PackageInstallDependency>();
            var packageReader = new PackageReader();

            var plan = traverser.CreateEvaluationPlan(TreeTraversalOrder.ParentLeftRight, dependency, node =>
            {
                if (!packages.ContainsKey(node.GetLowerFilename()))
                {
                    node.FailedToFind = true;
                    return node.Dependencies.ToArray();
                }
                var summary = packageReader.GetPackageSummary(packages[node.GetLowerFilename()]);
                node.PackageName = summary.Name;
                node.Summary = summary;
                node.Source = packages[node.GetLowerFilename()];
                var parent = node.Parent;
                while (parent != null)
                {
                    if (parent.GetLowerFilename() == node.GetLowerFilename())
                    {
                        node.HasCircularReference = true;
                        return node.Dependencies.ToArray();
                    }
                }
                node.Dependencies = summary.PackageDependencies.Select(x => new PackageInstallDependency()
                {
                    PackageKey = x.PackageKey,
                    PackageVersion = x.Version
                }).ToList();
                return node.Dependencies.ToArray();
            }, true);

            return plan;
        }

        public bool PackageIsInstalled(string packageKey, string version)
        {
            return Directory.Exists(GetPackageDirectory(packageKey, version));
        }

        public IEnumerable<HXPackageReference> GetContextPackages(IMeshKey contextKey)
        {
            var context = stateStore.StateRepository.QueryTree<HXContext>(contextKey).First;
            var packageSet = stateStore.StateRepository.QueryTree<UniqueSet<HXPackageReference>>(context.Packages.Key).First.ToList();
            return packageSet;
        }

        public void AddPackageToContext(IMeshKey contextKey, string packageKey, string version)
        {
            var packages = GetContextPackages(contextKey);
            if (packages.Where(x => x.PackageKey == packageKey && x.Version == version).Count() == 0)
            {
                var context = stateStore.StateRepository.QueryTree<HXContext>(contextKey).First;
                var activeSet = new ActiveUniqueSet<HXPackageReference>(context.Packages.Key, stateStore.StateRepository, stateStore.UserProfile);
                var packageReference = new HXPackageReference() { Stage = HXPackageStage.Package.ToString(), PackageKey = packageKey, Version = version };
                //packageReference.Locations.Add(packageDirectory);
                stateStore.StateRepository.Save(stateStore.UserProfile, packageReference);
                activeSet.Add(packageReference);

            }
        }

        public void AddProjectToContext(IMeshKey contextKey, string packageKey, string location)
        {
            var packages = GetContextPackages(contextKey);
            if (packages.Where(x => x.PackageKey == packageKey && x.ProjectLocation == location).Count() == 0)
            {
                var context = stateStore.StateRepository.QueryTree<HXContext>(contextKey).First;
                var activeSet = new ActiveUniqueSet<HXPackageReference>(context.Packages.Key, stateStore.StateRepository, stateStore.UserProfile);
                var packageReference = new HXPackageReference() { Stage = HXPackageStage.Project.ToString(), PackageKey = packageKey, Version = HularionExperienceKeyword.HXProjectPackageVersionIndicator.Value };
                packageReference.ProjectLocation = location;
                stateStore.StateRepository.Save(stateStore.UserProfile, packageReference);
                activeSet.Add(packageReference);
            }
        }

        public bool PackageIsAdded(IMeshKey contextKey, string packageKey, string version)
        {
            var packages = GetContextPackages(contextKey);
            return packages.Where(x => x.PackageKey == packageKey && x.Version == version).Count() > 0;
        }

        public bool PackageIsAdded(IEnumerable<HXPackageReference> packages, string packageKey, string version)
        {
            //packageKey = FormatString(packageKey);
            return packages.Where(x => x.PackageKey == packageKey && x.Version == version).Count() > 0;
        }


        public bool HasPackage(PackageIndicator indicator)
        {
            throw new NotImplementedException();
        }
    }

}
