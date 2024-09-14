#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionCore.General;
using HularionCore.Logic;
using HularionCore.Pattern.Functional;
using HularionCore.Pattern.Topology;
using HularionExperience;
using HularionExperience.PackageBuilder;
using HularionExperience.PackageBuilder.Storage;
using HularionMesh;
using HularionMesh.DomainValue;
using HularionMesh.SystemDomain;
using HularionMesh.SystemDomain.Active;
using HularionMesh.User;
using HularionPlugin.Route;
using HXUserState.Definition.Extensions;
using HXUserState.PackageManagement;
using HXUserState.Plugin.State.Routes.Request;
using HXUserState.Plugin.State.Routes.Response;
using HXUserState.State;
using HXUserState.State.Mesh;
using System.Data;
using static HXUserState.Plugin.State.Routes.Response.GetSourcesPackagesResponse;

namespace HXUserState.Plugin.State
{

    public class ApplicationStateRouter : IRouteProvider
    {
        public string Name => "HX State Provider";

        public string Key => "HularionExperience.State";

        public string Purpose => "Provides state information about Hularion Experience.";

        public IEnumerable<HularionRoute> Routes => routes;

        private List<HularionRoute> routes { get; set; } = new List<HularionRoute>();

        public string BaseRoute { get; private set; } = string.Format("{0}/state/", HularionExperienceKeyword.BaseRoute.Value);

        public ApplicationStateRouter(IHXStateStore stateStore, PackageManager packageManager)
        {

            //Route = string.Format("{0}get", BaseRoute),
            routes.Add(new HularionRoute<GetStateRequest, GetStateResponse>()
            {
                Name = "Get Hularion Experience State",
                Route = string.Format("{0}get", BaseRoute),
                Usage = "Gets the Hularion Experience State",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<GetStateRequest>, RoutedResponse<GetStateResponse>>(request =>
                {
                    var repository = stateStore.StateRepository;
                    //public HXState LoadState()
                    //{
                    //    var state = repository.QueryTree<HXState>(WhereExpressionNode.ReadAll).Result.FirstOrDefault();
                    //    if(state == null)
                    //    {
                    //        state = new HXState();
                    //        SaveState(state);
                    //    }
                    //    return state;
                    //}

                    //public void SaveState(HXState state)
                    //{
                    //    repository.Save(UserProfile.DefaultUser, state);
                    //}

                    var state = repository.QueryTree<HXState>(WhereExpressionNode.ReadAll).Result.FirstOrDefault();
                    if (state == null)
                    {
                        state = new HXState();
                        repository.Save(stateStore.UserProfile, state);
                    }

                    return new RoutedResponse<GetStateResponse>()
                    {
                        Detail = new GetStateResponse()
                        {
                            State = state
                        }
                    };
                })
            });

            //Route = string.Format("{0}context/create", BaseRoute),
            routes.Add(new HularionRoute<CreateHXContextRequest, CreateHXContextResponse>()
            {
                Name = "Creates a HularionExperience Context",
                Route = string.Format("{0}context/create", BaseRoute),
                Usage = "Creates a HularionExperience Context",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<CreateHXContextRequest>, RoutedResponse<CreateHXContextResponse>>(request =>
                {
                    var repository = stateStore.StateRepository;
                    var response = request.CreateResponse<CreateHXContextResponse>();
                    var state = repository.QueryTree<HXState>(WhereExpressionNode.ReadAll).Result.FirstOrDefault();
                    var context = new HXContext() { Name = request.Detail.ContextName };
                    state.Contexts.Add(context);
                    repository.Save(UserProfile.DefaultUser, state);
                    //stateStore.SaveState(state);
                    return response;
                })
            });

            //Route = string.Format("{0}context/packagesources/get", BaseRoute),
            routes.Add(new HularionRoute<GetUserContextPackageSourceRequest, GetUserContextPackageSourceResponse>()
            {
                Name = "GetContextPackageSource",
                Route = string.Format("{0}context/packagesources/get", BaseRoute),
                Usage = "Gets Packages Sources for an HX Context",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<GetUserContextPackageSourceRequest>, RoutedResponse<GetUserContextPackageSourceResponse>>(request =>
                {
                    var response = request.CreateResponse<GetUserContextPackageSourceResponse>();
                    var repository = stateStore.StateRepository;
                    var context = repository.QueryTree<HXContext>(request.Detail.ContextKey);
                    response.Detail.PackageSources = context.First.PackageSources.ToList();
                    return response;
                })
            });

            //Route = string.Format("{0}context/packagesources/createorupdate", BaseRoute),
            routes.Add(new HularionRoute<CreateOrUpdatePackageSourceRequest, CreateOrUpdatePackageSourceResponse>()
            {
                Name = "CreateOrUpdatePackageSource",
                Route = string.Format("{0}context/packagesources/createorupdate", BaseRoute),
                Usage = "Creates or updates a package source for an HX Context",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<CreateOrUpdatePackageSourceRequest>, RoutedResponse<CreateOrUpdatePackageSourceResponse>>(request =>
                {
                    var response = request.CreateResponse<CreateOrUpdatePackageSourceResponse>();
                    var repository = stateStore.StateRepository;
                    var source = request.Detail.GetPacakageSource();
                    if (MeshKey.KeyIsNull(request.Detail.Key))
                    {
                        var context = repository.QueryTree<HXContext>(request.Detail.ContextKey).First;
                        var activeSet = new ActiveUniqueSet<HXPackageSource>(context.PackageSources.Key, repository, stateStore.UserProfile);
                        activeSet.Add(source);
                        //context.PackageSources.Add(source);
                        //repository.Save(stateStore.UserProfile, context);
                        response.Detail.Key = source.Key;
                    }
                    else
                    {
                        var service = repository.GetDomainService<HXPackageSource>();
                        var domainObject = new DomainObject() { Key = source.Key };
                        domainObject.Values.Add(nameof(HXPackageSource.Name), source.Name);
                        domainObject.Values.Add(nameof(HXPackageSource.Location), source.Location);
                        var updater = new DomainObjectUpdater(domainObject);
                        var affector = new DomainValueAffectUpdate() { Updater = updater };
                        service.AffectProcessor.Process(new DomainValueAffectRequest() { Affected = new IDomainValueAffectItem[] { affector } });
                    }
                    return response;
                })
            });

            //Route = string.Format("{0}context/packagesources/delete", BaseRoute),
            routes.Add(new HularionRoute<DeletePackageSourceRequest, DeletePackageSourceResponse>()
            {
                Name = "DeletePackageSource",
                Route = string.Format("{0}context/packagesources/delete", BaseRoute),
                Usage = "Deletes a package source for an HX Context",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<DeletePackageSourceRequest>, RoutedResponse<DeletePackageSourceResponse>>(request =>
                {
                    var response = request.CreateResponse<DeletePackageSourceResponse>();
                    var repository = stateStore.StateRepository;
                    if (MeshKey.KeyIsNull(request.Detail.Key))
                    {
                        response.Messages.Add(new RoutedResponseMessage()
                        {
                            IsError = false,
                            Type = RoutedResponseMessageType.Warn,
                            Header = "The provided package source key is invalid.",
                            Message = "The provided package source key is invalid."
                        });
                    }
                    else
                    {
                        repository.Delete(stateStore.UserProfile, request.Detail.Key);
                    }
                    return response;
                })
            });

            //Route = string.Format("{0}context/packagesources/get/packages", BaseRoute),
            routes.Add(new HularionRoute<GetSourcesPackagesRequest, GetSourcesPackagesResponse>()
            {
                Name = "GetSourcesPackages",
                Route = string.Format("{0}context/packagesources/get/packages", BaseRoute),
                Usage = "Gets the packages from the indicated sources.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<GetSourcesPackagesRequest>, RoutedResponse<GetSourcesPackagesResponse>>(request =>
                {
                    var response = request.CreateResponse<GetSourcesPackagesResponse>();
                    //response.Detail.Packages = new List<ClientPackageMeta>();
                    var repository = stateStore.StateRepository;
                    var sources = repository.QueryTree<HXPackageSource>(request.Detail.SourceKeys);
                    var packageReader = new PackageReader();
                    var mapper = new MemberMapper();
                    mapper.CreateMap<PackageSummary, ExtendedPackageSummary>();
                    var packages = packageManager.GetContextPackages(request.Detail.ContextKey);
                    foreach (var source in sources.Result)
                    {
                        //var summaries = new List<PackageSummary>();
                        if (Directory.Exists(source.Location))
                        {
                            var packageFiles = Directory.GetFiles(source.Location).Where(x => x.EndsWith(HularionExperienceKeyword.HXPackageExtension.Value)).ToList();
                            //var serializer = new JsonSerializer(new StringCaseModifier());
                            foreach (var packageFile in packageFiles)
                            {
                                var summary = new ExtendedPackageSummary();
                                mapper.Map(packageReader.GetPackageSummary(packageFile), summary);
                                summary.IsInstalled = packageManager.PackageIsInstalled(summary.PackageKey, summary.Version);
                                summary.IsAdded = packageManager.PackageIsAdded(packages, summary.PackageKey, summary.Version);
                                summary.IsPackage = true;
                                summary.SourceKey = source.Key.ToString();
                                summary.SourceLocation = source.Location;
                                response.Detail.Packages.Add(summary);
                            }
                        }
                        if (Directory.Exists(source.Location))
                        {
                            var directories = Directory.GetDirectories(source.Location).Where(x => Directory.GetFiles(x).Where(x => x.Contains(HularionExperienceKeyword.HXProjectName.Value)).Count() > 0).ToList();
                            if (Directory.GetFiles(source.Location).Where(x => x.Contains(HularionExperienceKeyword.HXProjectName.Value)).Count() > 0)
                            {
                                directories.Add(source.Location);
                            }

                            var projectFiles = new List<string>();
                            foreach (var directory in directories)
                            {
                                var files = Directory.GetFiles(directory);
                                files = files.Where(x => x.EndsWith(HularionExperienceKeyword.HXProjectName.Value)).ToArray();
                                if (files.Length > 0)
                                {
                                    projectFiles.Add(File.ReadAllText(files.First()));
                                }
                            }
                            //var projectFiles = directories.Select(x => File.ReadAllText(String.Format(@"{0}\{1}", x, HularionExperienceKeyword.HXProjectName.Value))).ToList();
                            var packageBuilder = new HularionPackageBuilder();
                            var resourceGroups = directories.Select(x => packageBuilder.DirectoryToResourcesTransform.Transform(x)).ToList();
                            var resources = new List<HularionProjectResources>();
                            foreach (var group in resourceGroups)
                            {
                                resources.AddRange(group);
                            }

                            foreach (var resource in resources)
                            {
                                var project = packageBuilder.ResourcesToProjectTransform.Transform(resource);
                                var package = packageBuilder.ProjectToPackageTransform.Transform(project);
                                var summary = new ExtendedPackageSummary();
                                mapper.Map(package.PackageSummary, summary);
                                summary.IsProject = true;
                                summary.IsAdded = packageManager.PackageIsAdded(packages, summary.PackageKey, HularionExperienceKeyword.HXProjectPackageVersionIndicator.Value);
                                summary.SourceLocation = resource.Location;
                                response.Detail.Packages.Add(summary);
                            }
                        }
                    }
                    return response;
                })
            });

            //Route = string.Format("{0}context/packages/add", BaseRoute),
            routes.Add(new HularionRoute<AddPackageToContextRequest, AddPackageToContextResponse>()
            {
                Name = "AddPackageToContext",
                Route = string.Format("{0}context/packages/add", BaseRoute),
                Usage = "Adds the package to the context packages.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<AddPackageToContextRequest>, RoutedResponse<AddPackageToContextResponse>>(request =>
                {
                    var response = request.CreateResponse<AddPackageToContextResponse>();

                    var repository = stateStore.StateRepository;
                    var context = repository.QueryTree<HXContext>(request.Detail.ContextKey).First;
                    var packageReader = new PackageReader();

                    if (context == null)
                    {
                        response.IsFailure = true;
                        response.Messages.Add(new RoutedResponseMessage() { IsError = true, Message = string.Format("The context with key '{0}' was not found.", request.Detail.ContextKey) });
                        return response;
                    }

                    if (request.Detail.IsPackage)
                    {
                        packageManager.AddPackageToContext(request.Detail.ContextKey, request.Detail.PackageKey.ToString(), request.Detail.Version);
                        return response;
                    }

                    if (request.Detail.IsProject)
                    {
                        packageManager.AddProjectToContext(request.Detail.ContextKey, request.Detail.PackageKey.ToString(), request.Detail.ProjectLocation);
                        return response;
                    }


                    return response;
                })
            });

            //Route = string.Format("{0}context/packages/install", BaseRoute),
            routes.Add(new HularionRoute<InstallPackageRequest, InstallPackageResponse>()
            {
                Name = "Install Package and add to context.",
                Route = string.Format("{0}context/packages/install", BaseRoute),
                Usage = "Installs the package and its dependencies and then adds it to the context packages.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<InstallPackageRequest>, RoutedResponse<InstallPackageResponse>>(request =>
                {
                    var response = request.CreateResponse<InstallPackageResponse>();

                    var repository = stateStore.StateRepository;
                    var context = repository.QueryTree<HXContext>(request.Detail.ContextKey).First;

                    if (context == null)
                    {
                        response.IsFailure = true;
                        response.Messages.Add(new RoutedResponseMessage() { IsError = true, Message = string.Format("The context with key '{0}' was not found.", request.Detail.ContextKey) });
                        return response;
                    }

                    var install = new PackageInstallationDetail()
                    {
                        ContextKey = request.Detail.ContextKey,
                        Source = request.Detail.Source
                    };
                    var installResult = packageManager.InstallPackage(request.Detail.ContextKey, install);

                    if (!installResult.WasSuccessful)
                    {
                        response.IsFailure = true;
                        response.Messages.Add(new RoutedResponseMessage() { IsError = true, Message = string.Format("{0}", installResult.Message) });
                        return response;
                    }

                    return response;
                })
            });

            //Route = string.Format("{0}context/packages/delete", BaseRoute),
            routes.Add(new HularionRoute<DeletePackageFromContextRequest, DeletePackageFromContextResponse>()
            {
                Name = "DeletePackageFromContext",
                Route = string.Format("{0}context/packages/delete", BaseRoute),
                Usage = "Deletes the package from the context packages.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<DeletePackageFromContextRequest>, RoutedResponse<DeletePackageFromContextResponse>>(request =>
                {
                    var response = request.CreateResponse<DeletePackageFromContextResponse>();

                    var repository = stateStore.StateRepository;
                    var context = repository.QueryTree<HXContext>(request.Detail.ContextKey).First;
                    var where = WhereExpressionNode.CreateMemberIn(nameof(HXPackageReference.PackageKey), new string[] { request.Detail.PackageKey.Serialized });
                    if (!string.IsNullOrWhiteSpace(request.Detail.Version))
                    {
                        where.CombineWithOperator(WhereExpressionNode.CreateMemberIn(nameof(HXPackageReference.Version), new string[] { request.Detail.Version }), BinaryOperator.AND);
                    }
                    var package = repository.QueryTree<HXPackageReference>(where).First;



                    if (context == null)
                    {
                        response.IsFailure = true;
                        response.Messages.Add(new RoutedResponseMessage() { IsError = true, Message = string.Format("The context with key '{0}' was not found.", request.Detail.ContextKey) });
                        return response;
                    }

                    if (package == null)
                    {
                        response.IsFailure = true;
                        response.Messages.Add(new RoutedResponseMessage() { IsError = true, Message = string.Format("The package with key '{0}' was not found.", request.Detail.PackageKey) });
                        return response;
                    }

                    var activeSet = new ActiveUniqueSet<HXPackageReference>(context.Packages.Key, repository, stateStore.UserProfile);
                    activeSet.Remove(MeshKey.Parse(package.Key));
                    repository.Delete(stateStore.UserProfile, package.Key);

                    return response;
                })
            });

            //Route = string.Format("{0}context/packages/get", BaseRoute),
            routes.Add(new HularionRoute<GetContextPackagesRequest, GetContextPackagesResponse>()
            {
                Name = "GetPackages",
                Route = string.Format("{0}context/packages/get", BaseRoute),
                Usage = "Gets the packages selected into the context.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<GetContextPackagesRequest>, RoutedResponse<GetContextPackagesResponse>>(request =>
                {
                    var response = request.CreateResponse<GetContextPackagesResponse>();
                    var repository = stateStore.StateRepository;
                    var query = repository.CreateQuery<HXContext>();
                    query.Where(WhereExpressionNode.CreateKeysIn(request.Detail.ContextKey));
                    query.Select(new DomainReadRequest() { Mode = DomainReadRequestMode.All });
                    query.Link(x => x.Packages);
                    var queryResult = query.Render();

                    var packageReferences = repository.QueryTree<UniqueSet<HXPackageReference>>(queryResult.First.Packages.Key).First.ToList();
                    foreach (var packageReference in packageReferences)
                    {
                        if (packageReference.Stage == HXPackageStage.Package.ToString())
                        {
                            var package = packageManager.GetPackage(packageReference.PackageKey, packageReference.Version);
                            if(package == null | package.ClientPackage == null)
                            {
                                response.Messages.Add(request.CreateErrorMessage(String.Format("Package Not Found {0}@{1}", packageReference.PackageKey, packageReference.Version)));
                                response.IsFailure = true;
                                response.State = RoutedResponseState.Failure;
                                continue;
                            }
                            var meta = package.ClientPackage.CreatePackageMeta();
                            meta.Source = new PackageSource()
                            {
                                Type = PackageManagement.PackageSourceType.Package,
                                Structure = PackageSourceStructure.Disk
                            };
                            response.Detail.Packages.Add(meta);
                        }
                        else
                        {
                            //if (packageReference.Locations.ToList().Count == 0)
                            //{
                            //    continue;
                            //}
                            //var directory = packageReference.Locations.ToList().First();
                            //var projectFile = File.ReadAllText(String.Format(@"{0}\{1}", directory, HularionExperienceKeyword.HXProjectName.Value));
                            var packageBuilder = new HularionPackageBuilder();
                            //var resources = packageBuilder.DirectoryToResourcesTransform.Transform(directory);
                            var resources = packageBuilder.DirectoryToResourcesTransform.Transform(packageReference.ProjectLocation);

                            foreach (var resource in resources)
                            {
                                var project = packageBuilder.ResourcesToProjectTransform.Transform(resource);
                                var package = packageBuilder.ProjectToPackageTransform.Transform(project);
                                var meta = package.ClientPackage.CreatePackageMeta();
                                meta.Source = new PackageSource()
                                {
                                    Location = resource.Location,
                                    Type = PackageManagement.PackageSourceType.Project,
                                    Structure = PackageSourceStructure.Disk
                                };
                                response.Detail.Packages.Add(meta);
                            }
                        }
                    }

                    //response.Detail.Packages = repository.QueryTree<UniqueSet<HXPackageReference>>(queryResult.First.Packages.Key).First.ToList();
                    return response;
                })
            });

            //Route = string.Format("{0}context/applications/get", BaseRoute),
            routes.Add(new HularionRoute<GetContextApplicationsRequest, GetContextApplicationsResponse>()
            {
                Name = "GetApplications",
                Route = string.Format("{0}context/applications/get", BaseRoute),
                Usage = "Gets the applications selected into the context.",
                Handler = ParameterizedFacade.FromSingle<RoutedRequest<GetContextApplicationsRequest>, RoutedResponse<GetContextApplicationsResponse>>(request =>
                {
                    var response = request.CreateResponse<GetContextApplicationsResponse>();
                    var repository = stateStore.StateRepository;
                    var query = repository.CreateQuery<HXContext>();
                    query.Where(WhereExpressionNode.CreateKeysIn(request.Detail.ContextKey));
                    query.Select(new DomainReadRequest() { Mode = DomainReadRequestMode.All });
                    query.Link(x => x.Packages);
                    var queryResult = query.Render();

                    var packageReferences = repository.QueryTree<UniqueSet<HXPackageReference>>(queryResult.First.Packages.Key).First.ToList();
                    foreach (var packageReference in packageReferences)
                    {

                        if (packageReference.Stage == HXPackageStage.Package.ToString())
                        {
                            var package = packageManager.GetPackage(packageReference.PackageKey, packageReference.Version);
                            if(package == null || package.ClientPackage == null)
                            {
                                response.SetAsFailure(new RoutedResponseMessage() { Header = "Package Not Found", Message = String.Format("The package '{0}@{1}' was not found", packageReference.PackageKey, packageReference.Version) });
                                return response;
                            }
                            var meta = package.ClientPackage.CreatePackageMeta();
                            meta.Source = new PackageSource()
                            {
                                Type = PackageManagement.PackageSourceType.Package,
                                Structure = PackageSourceStructure.Disk
                            };
                            response.Detail.Applications.AddRange(meta.applications.Select(x => new GetContextApplicationsResponse.ApplicationResponse()
                            {
                                Meta = x,
                                PackageKey = package.PackageKey,
                                PackageVersion = package.Version,
                                PackageName = package.ClientPackage.Name,
                                IsPackage = true
                            }));
                        }
                        else
                        {
                            //if (packageReference.Locations.ToList().Count == 0)
                            //{
                            //    continue;
                            //}
                            //var directory = packageReference.Locations.ToList().First();
                            //var projectFile = File.ReadAllText(String.Format(@"{0}\{1}", directory, HularionExperienceKeyword.HXProjectName.Value));
                            var packageBuilder = new HularionPackageBuilder();
                            var resources = packageBuilder.DirectoryToResourcesTransform.Transform(packageReference.ProjectLocation);
                            //var resources = packageBuilder.DirectoryToResourcesTransform.Transform(directory);

                            foreach (var resource in resources)
                            {
                                var project = packageBuilder.ResourcesToProjectTransform.Transform(resource);
                                var package = packageBuilder.ProjectToPackageTransform.Transform(project);
                                var meta = package.ClientPackage.CreatePackageMeta();
                                meta.Source = new PackageSource()
                                {
                                    Location = resource.Location,
                                    Type = PackageManagement.PackageSourceType.Project,
                                    Structure = PackageSourceStructure.Disk
                                };
                                response.Detail.Applications.AddRange(meta.applications.Select(x => new GetContextApplicationsResponse.ApplicationResponse()
                                {
                                    Meta = x,
                                    PackageKey = package.Key,
                                    PackageVersion = package.Version,
                                    PackageName = package.ClientPackage.Name,
                                    IsProject = true,
                                    ProjectLocation = resource.Location
                                }));
                            }
                        }
                    }
                    return response;
                })
            });

            

            //Route = string.Format("{0}install/details", BaseRoute),
            routes.Add(new HularionRoute<PackageInstallDetailsRequest, PackageInstallDetailsResponse>()
            {
                Name = "Get Install Package Details",
                Route = string.Format("{0}context/packages/install/details", BaseRoute),
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
