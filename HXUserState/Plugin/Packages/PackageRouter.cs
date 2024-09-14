#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionCore.Pattern.Functional;
using HularionCore.Pattern.Topology;
using HularionExperience.Definition;
using HularionExperience.PackageBuilder;
using HularionMesh.DomainValue;
using HularionMesh.SystemDomain;
using HularionMesh;
using HularionPlugin.Route;
using HularionText.Language.Json;
using HularionText.StringCase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HXUserState.Plugin.State.Routes.Response.GetSourcesPackagesResponse;
using HularionExperience.PackageBuilder.Storage;
using HXUserState.Plugin.Packages.Routes.Response;
using HXUserState.Plugin.Packages.Routes.Request;
using HXUserState.PackageManagement;
using HularionExperience;
using HXUserState.State.Mesh;
using HXUserState.State;

namespace HXUserState.Plugin.Packages
{
    public class PackageRouter : IRouteProvider
    {
        public string Name => "Package Provider";

        public string Key => "HularionExperience.Package";

        public string Purpose => "Provides package resources to Hularion Experience clients.";

        public IEnumerable<HularionRoute> Routes => routes;

        private List<HularionRoute> routes { get; set; } = new List<HularionRoute>();

        public string BaseRoute { get; private set; } = string.Format("{0}/package/", HularionExperienceKeyword.BaseRoute);

        JsonSerializer serializer = new(new StringCaseModifier());

        public PackageRouter(IHXStateStore stateStore, PackageManager packageManager)
        {


            //Route = string.Format("{0}get", BaseRoute),
            routes.Add(new HularionRoute<PackageRequest, PackageResponse>()
            {
                Name = "Get Package",
                Route = string.Format("{0}get", BaseRoute),
                Usage = "Finds the indicated package and returns it if found.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<PackageRequest>, RoutedResponse<PackageResponse>>(request =>
                {
                    var response = request.CreateResponse<PackageResponse>();
                    var package = packageManager.LoadPackage(request.Detail.Indicator).ClientPackage;
                    response.Detail.Package = package;
                    return response;
                })
            });

            //Route = string.Format("{0}build", BaseRoute),
            routes.Add(new HularionRoute<PackageBuildRequest, PackageBuildResponse>()
            {
                Name = "Build Package",
                Route = string.Format("{0}build", BaseRoute),
                Usage = "Builds a package from project files and saves the package where indicated..",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<PackageBuildRequest>, RoutedResponse<PackageBuildResponse>>(request =>
                {
                    var response = request.CreateResponse<PackageBuildResponse>();
                    var packageBuilder = new HularionPackageBuilder();
                    var outputLocation = string.Format("{0}", request.Detail.OutputLocation).Trim();
                    var packageWriter = new PackageWriter();
                    var packages = packageBuilder.DirectoryToHXPackageTransform.Transform(request.Detail.ProjectLocation);
                    foreach (var package in packages)
                    {
                        try
                        {
                            packageWriter.WritePackage(outputLocation, package, overwrite: request.Detail.Overwrite);
                        }
                        catch (Exception e)
                        {
                            response.Messages.Add(new RoutedResponseMessage()
                            {
                                IsError = true,
                                Message = string.Format("An exception occurred creating package '{0}'. {1}", package.Key, e.Message)
                            });
                        }
                    }
                    return response;
                })
            });

            //Route = string.Format("{0}install/details", BaseRoute),
            routes.Add(new HularionRoute<PackageInstallDetailsRequest, PackageInstallDetailsResponse>()
            {
                Name = "Get Install Package Details",
                Route = string.Format("{0}install/details", BaseRoute),
                Usage = "Gets the details for installing a package.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<PackageInstallDetailsRequest>, RoutedResponse<PackageInstallDetailsResponse>>(request =>
                {
                    var response = request.CreateResponse<PackageInstallDetailsResponse>();

                    var traverser = new TreeTraverser<PackageInstallDependency>();
                    var repository = stateStore.StateRepository;
                    var storedSources = repository.QueryTree<HXPackageSource>(WhereExpressionNode.ReadAll);
                    var packageSources = storedSources.Result.Select(x => new PackageSource()
                    {
                        Location = x.Location,
                        Type = PackageManagement.PackageSourceType.Package,
                        Structure = PackageSourceStructure.Disk
                    }).ToArray();

                    var root = packageManager.GetDependencies(request.Detail.PackageKey, request.Detail.PackageVersion, packageSources);

                    response.Detail.Dependencies = root.First().Dependencies;
                    response.Detail.Summary = root.First().Summary;
                    response.Detail.Source = root.First().Source;
                    return response;
                })
            });


        }

    }
}
