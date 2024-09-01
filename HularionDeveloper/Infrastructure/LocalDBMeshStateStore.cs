#region License
/*
MIT License

Copyright (c) 2023-2024 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionMesh.Repository;
using HularionMesh.User;
using HXUserState.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HularionDeveloper.Infrastructure
{
    /// <summary>
    /// A configuration stored in a local database file.
    /// </summary>
    public class LocalDBMeshStateStore : IHXStateStore
    {

        public string Name => "HX Local DB Mesh State Store";
        public string Description => "Stores Hularion Experience states to a local database using HularionMesh.";

        private MeshRepository repository;

        public const string DEFAULT_NAME = "hx";
        public const string DEFAULT_SUFFIX = "state";

        /// <summary>
        /// Constructor. Creates the storage in the location of this assembly
        /// </summary>
        public LocalDBMeshStateStore()
        {
            var file = Assembly.GetExecutingAssembly().Location;
            var directory = file.Substring(0, file.LastIndexOf("\\"));
            repository = HularionMesh.Connector.Sqlite.SqliteMeshRepositoryBuilder.CreateRepository<HXStateStorageAttribute>(directory, databaseName: DEFAULT_NAME, databaseSuffix: DEFAULT_SUFFIX, useExisting: true);
        }

        /// <summary>
        /// Constructor. Creates the storage a the location of the directory.
        /// </summary>
        /// <param name="directory"></param>
        public LocalDBMeshStateStore(string directory, string? name = null)
        {
            name ??= DEFAULT_NAME;
            repository = HularionMesh.Connector.Sqlite.SqliteMeshRepositoryBuilder.CreateRepository<HXStateStorageAttribute>(directory, databaseName: name, databaseSuffix: DEFAULT_SUFFIX, useExisting: true);
        }

        /// <summary>
        /// The user using the store.
        /// </summary>
        public UserProfile UserProfile => UserProfile.DefaultUser;

        /// <summary>
        /// Gets the repository for the state store.
        /// </summary>
        /// <returns>The repository for the state store.</returns>
        public MeshRepository StateRepository => repository;


    }
}
