using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Windows.Media;
namespace VSFindTool
{
    using System.Windows.Controls.Primitives;
    /// <summary>
    /// Interaction logic for VSFindToolMainFormControl.xaml
    /// </summary>
    public partial class VSFindToolMainFormControl : UserControl
    {
        public VSFindToolMainForm parentToolWindow;
        //List<string> resList;
        public EnvDTE.Window LastDocWindow;
        EnvDTE.DTE dte;
        string originalFindResult2;
        Dictionary<string, FindSettings> findSettings = new Dictionary<string, FindSettings>();

        ResultTreeViewModel resultTree;

        public VSFindToolMainFormControl()
        {
            InitializeComponent();
            dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        //[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ExecSearch();
        }


        public void SetExpandAllInLvl(ItemCollection treeItemColleaction, bool value)
        {
            if (treeItemColleaction == null || treeItemColleaction.Count == 0)
                return;
            foreach (TreeViewItem item in treeItemColleaction)
            {
                item.IsExpanded = true;
                SetExpandAllInLvl(item.Items, value);
            }
        }

        public void JoinNodesWOLeafs(TreeViewItem treeItem)
        {
            List<TreeViewItem> list = new List<TreeViewItem>();
            if (treeItem.Items.Count == 1)
            {
                TreeViewItem treeItem2 = (TreeViewItem)treeItem.Items.GetItemAt(0);
                JoinNodesWOLeafs(treeItem2);
                if (treeItem2.Items.Count != 0)
                {
                    list.Clear();
                    treeItem.Items.RemoveAt(0);
                    foreach (TreeViewItem treeItem3 in treeItem2.Items)
                    {
                        list.Add(treeItem3);
                    }
                    foreach (TreeViewItem treeItem3 in list)
                    {
                        treeItem2.Items.Remove(treeItem3);
                        treeItem.Items.Add(treeItem3);
                    }
                    treeItem.Header += @"/" + treeItem2.Header;
                }
            }
        }

        private void tb_Checked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(1, GridUnitType.Star);
            last_rowFlat.Height = new GridLength(0);
            last_tb.Content = "Tree";
            last_tb.Foreground = Brushes.Red;
        }

        private void tb_Unchecked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(0);
            last_rowFlat.Height = new GridLength(1, GridUnitType.Star);
            last_tb.Content = "Flat";
            last_tb.ClearValue(ToggleButton.ForegroundProperty);
        }

        private void CopyItems(ItemCollection src, ItemCollection dst)
        {
            foreach (TreeViewItem item in src)
            {
                TreeViewItem newItem = new TreeViewItem() { Header = item.Header };
                dst.Add(newItem);
                newItem.FontWeight = item.FontWeight;  //FontWeights.Bold - FontWeights is a static class, so it's ok
                CopyItems(item.Items, newItem.Items);               
            }
        }

        private void FillSnapshotFromLast(string snapshotTag, TreeView flattv, TreeView treetv)
        {
            //TreeView flattv = (TreeView)this.FindName(snapshotTag + "_tvResultFlatTree");
            //TreeView treetv = (TreeView)this.FindName(snapshotTag + "_tvResultFlatTree");
            CopyItems(last_tvResultFlatTree.Items, flattv.Items);
            CopyItems(last_tvResultTree.Items, treetv.Items);
        }

        private void AddSmapshotTab()
        {
            string snapshotNumber = (tbcMain.Items.Count - 2).ToString();
            string snapshotTag = GetSnapshotTag(snapshotNumber);

            TabItem newTab = new TabItem()
            {
                Name = "tbi" + snapshotTag,
                Header = "Snap " + (tbcMain.Items.Count - 2).ToString(),
            };

            //add new tab
            tbcMain.Items.Add(newTab);
            //add main grid in tab and row definitions
            Grid grid = new Grid() {
                Background = this.FindResource(SystemColors.ControlLightBrushKey) as Brush };
            grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(18, GridUnitType.Pixel)
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
                Height = new GridLength(60, GridUnitType.Pixel)
            });
            //add grid as the main element in tab
            newTab.Content = grid;
            //Set snapshot tab as selected
            //tbcMain.SelectedItem = newTab;

            //Info label
            Label infoLabel = new Label()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(5, 0, 5, 5),
                Content = last_searchSettings.ToLabelString()
            };
            grid.Children.Add(infoLabel);
            Grid.SetRow(infoLabel, 0);

            //upper menu wrap panel
            WrapPanel upperMenuWrapPanel = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
            };
            grid.Children.Add(upperMenuWrapPanel);
            Grid.SetRow(upperMenuWrapPanel, 1);

                //toggle button Flat/Tree
                ToggleButton tbFlatTree = new ToggleButton(){
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(5, 3, 0, 0),
                    Padding = new Thickness(3, 1, 3, 1),
                    Width = 34,
                    BorderBrush = this.FindResource(SystemColors.ControlDarkBrushKey) as Brush,
                    Height = 21,
                    Content = "Flat"
                };
            upperMenuWrapPanel.Children.Add(tbFlatTree);
                //Button remove snapshot              
                Button btnRemoveSnapshot = new Button(){
                    Content = "X",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(5, 3, 0, 0),
                    Height = 21,
                };
                btnRemoveSnapshot.Click += (o, e) =>
                { 
                    tbcMain.Items.Remove(newTab);
                    findSettings.Remove(snapshotTag);                    
                };
            upperMenuWrapPanel.Children.Add(btnRemoveSnapshot);


            //add border and treeview for Flat view
            Border borderFlat = new Border()
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            grid.Children.Add(borderFlat);
            Grid.SetRow(borderFlat, 2);
            Grid.SetColumnSpan(borderFlat, 1);
            TreeView flattv = new TreeView()
            {
                Name = snapshotTag + "_tvResultFlatTree",
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            borderFlat.Child = flattv;

            //add border and treeview for Tree view
            Border borderTree = new Border()
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            grid.Children.Add(borderTree);
            Grid.SetRow(borderTree, 3);
            Grid.SetColumnSpan(borderTree, 2);
            TreeView treetv = new TreeView()
            {
                Name = snapshotTag + "_tvResultFlatTree",
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            borderTree.Child = treetv;

            //Populate new TreeViews from "last"
            FillSnapshotFromLast(snapshotTag, flattv, treetv);
            //Expand new views
            SetExpandAllInLvl(flattv.Items, true);
            SetExpandAllInLvl(treetv.Items, true);

            findSettings.Add(snapshotTag ,last_searchSettings.GetCopy());


            tbFlatTree.Click += (o, e) =>
            {
                ToggleButton tb = o as ToggleButton;
                if (tb.IsChecked == false)
                {
                    rowFlat.Height = new GridLength(1, GridUnitType.Star);
                    rowTree.Height = new GridLength(0, GridUnitType.Pixel);
                    tb.Content = "Flat";
                    //tb.ClearValue(ToggleButton.BackgroundProperty);
                    tb.ClearValue(ToggleButton.ForegroundProperty);
                }
                else
                {
                    rowFlat.Height = new GridLength(0, GridUnitType.Pixel);
                    rowTree.Height = new GridLength(1, GridUnitType.Star); 
                    tb.Content = "Tree";
                    //tb.Background = this.FindResource(SystemColors.ControlDarkBrushKey) as Brush;
                    tb.Foreground = Brushes.Red;
                }
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddSmapshotTab();
            //Todo - dodać nową zakładkę o nazwie odebranej od uzytkowniak
            //TODO dodać na zakładkę nowe obiekty
            //todo dodać navigatora
            //to do dodać FILL setting from settings
            //Todo podłaczyć do obiektów eventy
            //todo dodać skrót wlaczający tool na pierwszą zakładkę
        }

        //private void HandleCheck(object sender, RoutedEventArgs e)
        //{
        //    text2.Text = "Button is Checked";
        //}

        //private void HandleUnchecked(object sender, RoutedEventArgs e)
        //{
        //    text2.Text = "Button is unchecked.";
        //}
    }
}