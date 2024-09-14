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
    /// <summary>
    /// Information referring to a package.
    /// </summary>
    [HXStateStorage]
    public class HXPackageReference
    {
        /// <summary>
        /// The key for the repository store.
        /// </summary>
        [DomainProperty(DomainObjectPropertySelector.Key)]
        public IMeshKey Key { get; set; }

        /// <summary>
        /// The unique identifier for the package.
        /// </summary>
        public string PackageKey { get; set; }

        /// <summary>
        /// The name of the package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Additional details abou the package.
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// The product to which the package belongs.
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// The version of the package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The stage of the package. i.e. package or project.
        /// </summary>
        public string Stage { get; set; }
        //public HXPackageStage Stage { get { return String.IsNullOrWhiteSpace(stage) ? HXPackageStage.Package : (HXPackageStage)Enum.Parse(typeof(HXPackageStage), stage); } set { stage = value.ToString(); } }

        /// <summary>
        /// The location of the project.
        /// </summary>
        public string ProjectLocation { get; set; }

        /// <summary>
        /// The applications available within the package.
        /// </summary>
        public UniqueSet<HXApplicationDetail> Applications { get; set; } = new UniqueSet<HXApplicationDetail>();


        public HXPackageStage GetStage()
        {
            return string.IsNullOrWhiteSpace(Stage) ? HXPackageStage.Package : (HXPackageStage)Enum.Parse(typeof(HXPackageStage), Stage);
        }
    }
}
