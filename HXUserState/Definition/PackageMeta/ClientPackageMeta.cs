#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HularionExperience.Definition.Package;
using HXUserState.PackageManagement;

namespace HXUserState.Definition.PackageMeta
{
    public class ClientPackageMeta
    {

        public string key { get; set; }

        public string name { get; set; }

        public string version { get; set; }

        public string stage { get; set; }

        public ClientApplicationMeta defaultApplication { get; set; }

        public List<ClientApplicationMeta> applications { get; set; } = new List<ClientApplicationMeta>();

        public List<ClientScriptSetMeta> scriptSets { get; set; } = new List<ClientScriptSetMeta>();

        public List<ClientPresenterSetMeta> presenterSets { get; set; } = new List<ClientPresenterSetMeta>();

        public List<ClientPackageReferenceMeta> packageReferences { get; set; } = new List<ClientPackageReferenceMeta>();

        public PackageSource Source { get; set; }

    }
}
