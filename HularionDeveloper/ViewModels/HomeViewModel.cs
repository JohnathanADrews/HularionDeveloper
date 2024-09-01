#region License
/*
MIT License

Copyright (c) 2023-2024 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionCore.Pattern.Functional;
using HularionDeveloper.Infrastructure;
using HularionExperience.Embedded.ViewModels;
using HularionExperience;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using HularionExperience.Embedded;
using System.Windows.Data;
using HularionExperience.Plugin.Styles.Routes.Response;

namespace HularionDeveloper.ViewModels
{
    internal class HomeViewModel : ViewModel
    {

        public object Display { get; private set; }


        #region MenuThings

        public ICommand ShowDevTools { get; private set; }
        public ICommand Refresh { get; private set; }

        public bool ToolsIsEnabled { get; private set; } = true;
        public bool ShowDevToolsIsEnabled { get; private set; } = true;
        public bool RefreshIsEnabled { get; private set; } = true;

        #endregion

        public HXScreenController ScreenController { get; }

        public HularionExperienceApplication Hularion { get; }

        public ObservableCollection<DynamicMenuItem> StyleMenuItems { get; } = new ObservableCollection<DynamicMenuItem>();

        public HomeViewModel(HularionExperienceApplication hularion, HXScreenController screenController)
        {
            this.Hularion = hularion;

            Display = new BrowserViewModel(hularion, screenController);

            BindingOperations.EnableCollectionSynchronization(StyleMenuItems, StyleMenuItems);

            hularion.StyleCategoryRouter.SetCategoryUpdateRouteHandler(request =>
            {
                var response = new StyleCategoryUpdatedNotifyResponse();

                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    StyleMenuItems.Clear();
                    foreach (var category in request.Categories)
                    {
                        var categoryItem = new DynamicMenuItem() { Name = category.Name };
                        foreach (var option in category.Options)
                        {
                            var optionItem = new DynamicMenuItem() { Name = option };
                            optionItem.Command = new RelayCommand(parameter => true, parameter =>
                            {
                                hularion.StyleCategoryRouter.ProcessCategoryChanged(hularion.Router, category.Name, option);
                            });
                            categoryItem.Items.Add(optionItem);
                        }
                        StyleMenuItems.Add(categoryItem);
                    }
                });
                return response;
            });

            Refresh = new RelayCommand(parameter => true, parameter =>
            {
                screenController.ReloadHandler.Handler();
            });

            ShowDevTools = new RelayCommand(parameter => true, parameter =>
            {
                screenController.ShowDevToolsHandler.Handler();
            });
        }

    }
}
