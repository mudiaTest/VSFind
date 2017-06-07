using System.Windows.Media;

namespace VSFindTool
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    public partial class VSFindToolMainFormControl : UserControl
    {
        private void CopyItems(ItemCollection src, ItemCollection dst, FindSettings findSettings, TextBox tbPreview)
        {
            foreach (TreeViewItem item in src)
            {
                TreeViewItem newItem = new TreeViewItem() { Header = item.Header };
                dst.Add(newItem);
                dictSearchSettings.Add(newItem, findSettings);
                //only leafs
                if (item.Items.Count == 0)
                {
                    newItem.MouseDoubleClick += OpenResultShowPreview;
                    newItem.MouseUp += PreviewResultDoc;
                    newItem.MouseRightButtonUp += ShowResultTreeContextMenu;
                    dictResultItems.Add(newItem, dictResultItems[item].GetCopy());
                }
                newItem.FontWeight = item.FontWeight;  //FontWeights.Bold - FontWeights is a static class, so it's ok
                CopyItems(item.Items, newItem.Items, findSettings, tbPreview);
            }
        }

        private void FillSnapshotFromLast(string snapshotTag, TreeView flattv, TreeView treetv, FindSettings findSettings, TextBox tbPreview)
        {
            dictTBPreview.Add(findSettings, tbPreview);

            CopyItems(last_tvResultFlatTree.Items, flattv.Items, findSettings, tbPreview);
            CopyItems(last_tvResultTree.Items, treetv.Items, findSettings, tbPreview);
            dictTVData[flattv] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName)
            };
            dictTVData[treetv] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName)
            };
        }

        private void AddSmapshotTab(string infoContent)
        {
            string snapshotNumber = (tbcMain.Items.Count - 2).ToString();
            string snapshotTag = GetSnapshotTag(snapshotNumber);

            //new tab
            TabItem newTab = new TabItem()
            {
                Name = "tbi" + snapshotTag,
                Header = "Snap " + (tbcMain.Items.Count - 2).ToString(),
            };

            //add new tab
            tbcMain.Items.Add(newTab);
            //add main grid in tab and row definitions
            Grid grid = new Grid()
            {
                Background = this.FindResource(SystemColors.ControlLightBrushKey) as Brush
            };

            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(20, GridUnitType.Pixel)
            });

            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(30, GridUnitType.Pixel)
            });

            RowDefinition rowFlat = new RowDefinition()
            {
                Name = snapshotTag + "_rowFlat",
                Height = new GridLength(1, GridUnitType.Star)
            };
            grid.RowDefinitions.Add(rowFlat);

            RowDefinition rowTree = new RowDefinition()
            {
                Name = snapshotTag + "_rowTree",
                Height = new GridLength(0, GridUnitType.Pixel)
            };
            grid.RowDefinitions.Add(rowTree);

            grid.RowDefinitions.Add(new RowDefinition()
            {
                Name = snapshotTag + "_preview",
                Height = new GridLength(100, GridUnitType.Pixel)
            });

            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(20, GridUnitType.Pixel)
            });

            //add grid as the main element in tab
            newTab.Content = grid;
            //Set snapshot tab as selected
            //tbcMain.SelectedItem = newTab;

            //Info Wraper
            WrapPanel infoWrapPanel = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                Name = snapshotTag + "_infoWrapPanel",
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            grid.Children.Add(infoWrapPanel);
            Grid.SetRow(infoWrapPanel, 0);
            Grid.SetColumn(infoWrapPanel, 0);
            FillWraperPanel(lastSearchSettings, infoWrapPanel);

            //navigator
            Grid navGrid = new Grid()
            {
                Background = this.FindResource(SystemColors.ControlLightBrushKey) as Brush,
            };
            navGrid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(33, GridUnitType.Pixel)
            });
            grid.Children.Add(navGrid);
            Grid.SetRow(navGrid, 1);

            //upper menu wrap panel
            WrapPanel upperMenuWrapPanel = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                Name = snapshotTag + "upperMenuWrapPanel",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            navGrid.Children.Add(upperMenuWrapPanel);
            Grid.SetColumn(upperMenuWrapPanel, 0);

            //toggle button Flat/Tree
            ToggleButton tbFlatTree = new ToggleButton()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(3, 1, 3, 1),
                Width = 34,
                BorderBrush = this.FindResource(SystemColors.ControlDarkBrushKey) as Brush,
                Height = 21,
                Content = "Tree"
            };
            tbFlatTree.Click += (o, e) =>
            {
                ToggleButton tb = o as ToggleButton;
                if (tb.IsChecked == false)
                {
                    rowFlat.Height = new GridLength(1, GridUnitType.Star);
                    rowTree.Height = new GridLength(0, GridUnitType.Pixel);
                    tb.ClearValue(ToggleButton.ForegroundProperty);
                }
                else
                {
                    rowFlat.Height = new GridLength(0, GridUnitType.Pixel);
                    rowTree.Height = new GridLength(1, GridUnitType.Star);
                    tb.Foreground = Brushes.Red;
                }
            };
            upperMenuWrapPanel.Children.Add(tbFlatTree);

            //Button Expand all              
            Button btnExpAll = new Button()
            {
                Name = snapshotTag + "_ExpAll",
                Width = 21,
                Height = 21,
                Content = "+",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                Padding = new Thickness(0, -5, 0, 0)
            };
            upperMenuWrapPanel.Children.Add(btnExpAll);

            //Button UnExpand all           
            Button btnUnExpAll = new Button()
            {
                Name = snapshotTag + "_UnExpAll",
                Width = 21,
                Height = 21,
                Content = "-",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                Padding = new Thickness(0, -5, 0, 0)
            };
            upperMenuWrapPanel.Children.Add(btnUnExpAll);


            //toggle button Flat/Tree
            ToggleButton tbShortDir = new ToggleButton()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(3, 1, 3, 1),
                Width = 40,
                BorderBrush = this.FindResource(SystemColors.ControlDarkBrushKey) as Brush,
                Height = 21,
                Content = "Short"
            };

            upperMenuWrapPanel.Children.Add(tbShortDir);

            //Button Find again              
            Button btnFindAgain = new Button()
            {
                Content = "Find again",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(2, 0, 2, 0),
                Height = 21
            };
            upperMenuWrapPanel.Children.Add(btnFindAgain);

            //Button remove snapshot              
            Button btnRemoveSnapshot = new Button()
            {
                Content = "X",
                Foreground = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 0, 0),
                Height = 21,
                Width = 21
            };
            navGrid.Children.Add(btnRemoveSnapshot);
            Grid.SetColumn(btnRemoveSnapshot, 1);

            //add Flat view
            TreeView flattv = new TreeView()
            {
                Name = snapshotTag + "_tvResultFlatTree",
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            grid.Children.Add(flattv);
            Grid.SetRow(flattv, 2);
            Grid.SetColumnSpan(flattv, 1);

            //add Tree view
            TreeView treetv = new TreeView()
            {
                Name = snapshotTag + "_tvResultFlatTree",
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            grid.Children.Add(treetv);
            Grid.SetRow(treetv, 3);
            Grid.SetColumnSpan(treetv, 2);

            //add preview
            TextBox tbPreview = new TextBox()
            {
                Name = snapshotTag + "_TBPreview",
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            grid.Children.Add(tbPreview);
            Grid.SetRow(tbPreview, 4);
            Grid.SetColumnSpan(tbPreview, 2);


            //Result summary
            Label infoLabel = new Label()
            {
                Name = snapshotTag + "labelInfo",
                Padding = new Thickness(5, 2, 5, 5),
                Content = infoContent
            };

            grid.Children.Add(infoLabel);
            Grid.SetRow(infoLabel, 5);
            Grid.SetColumnSpan(infoLabel, 2);

            //Events
            btnExpAll.Click += (o, e) =>
            {
                SetExpandAllInLvl(flattv.Items, true);
                SetExpandAllInLvl(treetv.Items, true);
            };

            btnUnExpAll.Click += (o, e) =>
            {
                SetExpandAllInLvl(flattv.Items, false);
                SetExpandAllInLvl(treetv.Items, false);
            };

            btnFindAgain.Click += (o, e) =>
            {
                findSettings[snapshotTag].Settings2Form(this);
                tbiSearch.Focus();
            };

            btnRemoveSnapshot.Click += (o, e) =>
            {
                tbcMain.Items.Remove(newTab);
                findSettings.Remove(snapshotTag);
            };

            tbShortDir.Checked += (o, e) =>
            {
                 (o as ToggleButton).Foreground = Brushes.Red;
                 SetHeaderShortLong(flattv, flattv.Items, true);
                 SetHeaderShortLong(treetv, treetv.Items, true);
            };

            tbShortDir.Unchecked += (o, e) =>
            {
                (o as ToggleButton).ClearValue(ToggleButton.ForegroundProperty);
                SetHeaderShortLong(flattv, flattv.Items, false);
                SetHeaderShortLong(treetv, treetv.Items, false);
            };



            //Copy settings for snapshot
            FindSettings snapSettings = lastSearchSettings.GetCopy();
            findSettings.Add(snapshotTag, snapSettings);


            //Copy ResultSummary for snapshot
            ResultSummary resultSummary = dictResultSummary[lastSearchSettings].GetCopy();
            dictResultSummary.Add(snapSettings, resultSummary);


            //Populate new TreeViews from "last"
            FillSnapshotFromLast(snapshotTag, flattv, treetv, findSettings[snapshotTag], tbPreview);

            //Fill ResultSummary label
            FillResultSummary(infoLabel, resultSummary);

            //Expand new views
            SetExpandAllInLvl(flattv.Items, true);
            SetExpandAllInLvl(treetv.Items, true);

            //Snapshot views are filled from last views, so it's important to set short/long button also.
            tbShortDir.IsChecked = last_shortDir.IsChecked;
        }

        private string GetSnapshotTag(int number)
        {
            return "snap" + number.ToString();
        }

        private string GetSnapshotTag(string number)
        {
            return "snap" + number;
        }

    }
}