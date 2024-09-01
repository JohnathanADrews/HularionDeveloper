using CefSharp.DevTools.Page;
using HularionDeveloper.Infrastructure;
using HularionDeveloper.ViewModels;
using HularionDeveloper.Views;
using HularionExperience;
using HularionExperience.Boot;
using HularionExperience.Embedded;
using HularionExperience.PackageBuilder;
using HularionExperience.PackageStores;
using HularionExperience.PackageStores.Composite;
using HularionExperience.PackageStores.DirectoryStore;
using HularionExperience.PackageStores.Embedded;
using HularionExperience.PackageStores.ProjectDirectoryStore;
using HularionExperience.PackageStores.RouteInjector;
using HularionExperience.Plugin.Packages;
using HularionExperience.Registration;
using HularionMesh.Serializer.Json;
using HularionPlugin.PluginMeta;
using HularionPlugin.Route;
using HXCore;
using HXFileDialog;
using HXUserState.PackageManagement;
using HXUserState.Plugin.State;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace HularionDeveloper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The Hularion Experience Application.
        /// </summary>
        public HularionExperienceApplication HXApplication { get; private set; }

        /// <summary>
        /// Enables reloading the application using ReloadHandler. i.e. refreshing the UI page. 
        /// </summary>
        public HXScreenController HXScreenController { get; private set; }

        public App()
        {
            var baseDirectory = ".hx";
            var hularionBuilder = new EmbeddedHXAppBuilder();
            hularionBuilder.SetAppIndexTitle("Hularion Developer");
            //hularionBuilder.HXAppBuilder.SetStateStore(new LocalDBMeshStateStore(directory: baseDirectory));
            HXScreenController = new HXScreenController();
            hularionBuilder.HXAppBuilder.ScreenController = HXScreenController;
            hularionBuilder.HXAppBuilder.SetBaseDirectory(baseDirectory);
            var appStartup = new ApplicationStartup();
            appStartup.PackageKey = "HularionExperienceDeveloper";
            appStartup.PackageVersion = "embedded";
            appStartup.StartApplication = "HularionDeveloperApplication";
            hularionBuilder.HXAppBuilder.SetApplicationStartup(appStartup);
            HXApplication = hularionBuilder.Build();
            var stateStore = new LocalDBMeshStateStore(directory: baseDirectory);

            MeshJsonSerializer.SetSerializerTypes(HXApplication.Router.Deserializer);
            MeshJsonSerializer.SetSerializerTypes(HXApplication.Router.Serializer);

            var metaRouter = new MetaRouter(HXApplication.Router);
            HXApplication.Router.RegisterRouteProvider(metaRouter);

            var packageManager = new HXUserState.PackageManagement.PackageManager(stateStore, ".hx", HXApplication.Router);
            var stateRouter = new ApplicationStateRouter(stateStore, packageManager);

            var packageRouter = new PackageRouter(HXApplication.PackageManager);

            var routedPackageStore = new RouteInjectorPackageStore();
            var routeProviders = new List<IRouteProvider>() { stateRouter, packageRouter };
            routedPackageStore.AddRouteProvider("HularionExperienceDeveloper", new Func<IRouteProvider[]>(() => routeProviders.ToArray()));
            HXApplication.PackageManager.AddPackageStore(routedPackageStore);

            var directoryStore = new DirectoryPackageStore(".hx");
            HXApplication.PackageManager.AddPackageStore(directoryStore);

            var runDebug = new Action(() =>
            {
                var projectProvider = new DevProjectFileProvider();
                List<string> projectDirectories = new();
                projectDirectories.AddRange(projectProvider.LocateHXFromCSharpProject(Assembly.GetExecutingAssembly()));
                projectDirectories.AddRange(projectProvider.LocateHXFromCSharpType<HXCoreAssemblyReference>());
                projectDirectories.AddRange(projectProvider.LocateHXFromCSharpType<FileDialogAssemblyReference>());
                var projectLocators = projectDirectories.Distinct().Select(x => new ProjectLocator()
                {
                    Directory = x,
                    SearchMethod = ProjectDirectorySearchMethod.ImmediateDirectory
                }).ToArray();
                var fileProjectStore = new ProjectFilePackageStore();
                fileProjectStore.AddLocators(projectLocators);
                routedPackageStore.PackageStore = fileProjectStore;
                HXApplication.EnableKernelProjectFileStore = true;
            });

            var runRelease = new Action(() =>
            {
                var compositeProvider = new CompositePackageStore();

                var fileProjectStore = new ProjectFilePackageStore();
                compositeProvider.AddPackageStore(fileProjectStore);

                var projectProvider = new EmbeddedPackageStore();
                projectProvider.AddAssembly(Assembly.GetExecutingAssembly());
                projectProvider.AddTypeAssembly<HXCoreAssemblyReference>();
                projectProvider.AddTypeAssembly<FileDialogAssemblyReference>();
                projectProvider.MatchAnyVersion = true;
                projectProvider.Initialize();

                compositeProvider.AddPackageStore(projectProvider);

                routedPackageStore.PackageStore = compositeProvider;
            });

#if DEBUG
            runDebug();
            //runRelease();
#else
            runRelease();
#endif



        }



    }
}
