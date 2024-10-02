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

        public EmbeddedHularionExperienceApplication EmbeddedHXApplication { get; private set; }

        public App()
        {


            var baseDirectory = ".hx";
            var appStartup = new ApplicationStartup();
            appStartup.PackageKey = "HularionExperienceDeveloper";
            appStartup.PackageVersion = "embedded";
            appStartup.StartApplication = "HularionDeveloperApplication";
            EmbeddedHXApplication = new EmbeddedHularionExperienceApplication(appStartup);

            MeshJsonSerializer.SetSerializerTypes(EmbeddedHXApplication.HXApplication.Router.Serializer);
            MeshJsonSerializer.SetSerializerTypes(EmbeddedHXApplication.HXApplication.Router.Deserializer);

            var metaRouter = new MetaRouter(EmbeddedHXApplication.HXApplication.Router);
            EmbeddedHXApplication.HXApplication.Router.RegisterRouteProvider(metaRouter);

            var stateStore = new LocalDBMeshStateStore(directory: baseDirectory);
            var packageManager = new HXUserState.PackageManagement.PackageManager(stateStore, baseDirectory, EmbeddedHXApplication.HXApplication.Router);
            var stateRouter = new ApplicationStateRouter(stateStore, packageManager);

            var packageRouter = new PackageRouter(EmbeddedHXApplication.HXApplication.PackageManager);

            var routedPackageStore = new RouteInjectorPackageStore();
            var routeProviders = new List<IRouteProvider>() { stateRouter, packageRouter };
            routedPackageStore.AddRouteProvider("HularionExperienceDeveloper", new Func<IRouteProvider[]>(() => routeProviders.ToArray()));
            EmbeddedHXApplication.HXApplication.PackageManager.AddPackageStore(routedPackageStore);

            var directoryStore = new DirectoryPackageStore(baseDirectory);
            EmbeddedHXApplication.HXApplication.PackageManager.AddPackageStore(directoryStore);

            var modeStartup = new StartModePackageStoreSetup();
            modeStartup.AddCallerAssembly();
            modeStartup.AddAssembly<HXCoreAssemblyReference>();
            modeStartup.AddAssembly<FileDialogAssemblyReference>();
            modeStartup.AddFilePackageStore(StartModeOption.RELEASE);
            routedPackageStore.PackageStore = modeStartup.GetPackageStore();



        }



    }
}
