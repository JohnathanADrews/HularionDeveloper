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
using HularionMesh.Repository;
using HularionMesh.User;

namespace HXUserState.State
{

    /// <summary>
    /// Defines how to get and save an HX state.
    /// </summary>
    public interface IHXStateStore
    {
        /// <summary>
        /// The name of the configuration provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The description of the configuration provider.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The user using the store.
        /// </summary>
        UserProfile UserProfile { get; }

        ///// <summary>
        ///// Loads the Hualrion Experience state.
        ///// </summary>
        ///// <returns>The Hualrion Experience state.</returns>
        //HXState LoadState();

        ///// <summary>
        ///// Saves the Hualrion Experience state.
        ///// </summary>
        ///// <param name="state">The Hualrion Experience state.</param>
        //void SaveState(HXState state);

        /// <summary>
        /// Gets the repository for the state store.
        /// </summary>
        /// <returns>The repository for the state store.</returns>
        MeshRepository StateRepository { get; }

    }
}
