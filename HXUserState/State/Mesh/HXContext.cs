#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionMesh.Repository;
using HularionMesh;
using HularionMesh.SystemDomain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HXUserState.State.Mesh
{
    [HXStateStorage]
    public class HXContext
    {
        [DomainProperty(DomainObjectPropertySelector.Key)]
        public IMeshKey Key { get; set; }

        /// <summary>
        /// The name of the context as selected by the user.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The s
        /// </summary>
        //public UniqueSet<string> StartDirectories { get; set; } = new UniqueSet<string>();

        /// <summary>
        /// The package sources known to the context.
        /// </summary>
        public UniqueSet<HXPackageSource> PackageSources { get; set; } = new UniqueSet<HXPackageSource>();

        /// <summary>
        /// The data sources used by this context.
        /// </summary>
        public UniqueSet<HXDataSource> DataSources { get; set; } = new UniqueSet<HXDataSource>();

        /// <summary>
        /// The packages added to this context.
        /// </summary>
        public UniqueSet<HXPackageReference> Packages { get; set; } = new UniqueSet<HXPackageReference>();
    }
}
