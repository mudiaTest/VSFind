//------------------------------------------------------------------------------
// <copyright file="VSFindToolMainFormControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows.Media;

namespace VSFindTool
{
    using System.Windows.Controls.Primitives;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    /// <summary>
    /// Interaction logic for VSFindToolMainFormControl.
    /// </summary>
    public partial class VSFindToolMainFormControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VSFindToolMainFormControl"/> class.
        /// </summary>
        public VSFindToolMainForm parentToolWindow;
        public EnvDTE.Window LastDocWindow;
        EnvDTE80.DTE2 dte {
            get
            {
                return ((VSFindToolPackage)parentToolWindow.Package).dte2;
            }
        }
        /*IComponentModel componentModel
        {
            get
            {
                return ((IComponentModel)parentToolWindow.Package).componentModel;
            }
        }*/
        Dictionary<string, FindSettings> findSettings = new Dictionary<string, FindSettings>();
        IVsTextManager textManager
        {
            get
            {
                return ((VSFindToolPackage)parentToolWindow.Package).textManager;
            }
        }


        public VSFindToolMainFormControl()
        {
            this.InitializeComponent();
            last_shortDir.IsChecked = true;
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]


        public void OpenResultDocLine(object src, EventArgs args)
        {
            ResultLineData resultLine = dictResultLines[(TreeViewItem)src];
            FindSettings settings = dictSearchSettings[(TreeViewItem)src];
            if (dte != null)
            {
                EnvDTE.Window docWindow = dte.ItemOperations.OpenFile(resultLine.linePath, Constants.vsViewKindTextView);
                TextSelection selection = ((EnvDTE.TextSelection)dte.ActiveDocument.Selection);
                if (selection != null)
                {
                    selection.SelectAll();
                    int lastLine = selection.CurrentLine;
                    selection.MoveToLineAndOffset(Math.Max(1, resultLine.lineInFileNumbe.Value - 2), 1, false);
                    selection.MoveToLineAndOffset(Math.Min(lastLine, resultLine.lineInFileNumbe.Value + 4), 1, true); 
                    selection.EndOfLine(true);
                    dictTBPreview[(TreeViewItem)src].Text = selection.Text;

                    selection.GotoLine(resultLine.lineInFileNumbe.Value, false);
                    if (settings.chkRegExp == true)
                        Debug.Assert(false, "Brak obsługi RegExp");
                    else
                    {
                        selection.MoveToLineAndOffset(resultLine.lineInFileNumbe.Value+1, resultLine.textInLineNumer+1, false);
                        selection.MoveToLineAndOffset(resultLine.lineInFileNumbe.Value+1, resultLine.textInLineNumer+settings.tbPhrase.Length+1, true);
                    }
                    //Add action to set focus no doc window after finishing all action in queue (currenty there should be only double click event) 
                    Action showAction = () => docWindow.Activate();
                    this.Dispatcher.BeginInvoke(showAction);
                }
            }
            else
                Debug.Assert(false, "Brak DTE");
        }

        public void PreviewResultDoc(object src, EventArgs args)
        {
            ResultLineData resultLine = dictResultLines[(TreeViewItem)src];
            FindSettings settings = dictSearchSettings[(TreeViewItem)src];
            TextBox tbPreview = dictTBPreview[(TreeViewItem)src];
            tbPreview.Text = "";
            int lineNumber = 0;
            using (var reader = new StreamReader(resultLine.linePath))
            {
                string line;
                while (lineNumber <= Math.Max(0, resultLine.lineInFileNumbe.Value + 2))
                {
                    lineNumber++;
                    line = reader.ReadLine();
                    if (lineNumber >= Math.Max(0, resultLine.lineInFileNumbe.Value - 2) && lineNumber <= Math.Max(0, resultLine.lineInFileNumbe.Value + 2))
                        tbPreview.AppendText((tbPreview.Text != "" ? Environment.NewLine : "") + line);
                }
            }
        }


        internal void MoveResultToTreeList(TreeView tvResultTree, FindSettings last_searchSettings, TextBox tbPreview)
        {
            ItemCollection treeItemColleaction;
            string pathAgg;
            TreeViewItem treeItem;
            TreeViewItem leafItem;

            dictTVData[tvResultTree] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName)
            };

            tvResultTree.Items.Clear();

            foreach (ResultLineData resultLineData in resultList)
            {
                treeItemColleaction = tvResultTree.Items;
                treeItem = null; 
                pathAgg = "";
                for (int i = 0; i < resultLineData.pathPartsList.Count; i++)
                {
                    if (pathAgg == "")
                        pathAgg = resultLineData.pathPartsList[i];
                    else
                        pathAgg = pathAgg + @"\" + resultLineData.pathPartsList[i];
                    if (Directory.Exists(pathAgg) || File.Exists(pathAgg))
                    {
                        treeItem = GetItem(treeItemColleaction, resultLineData.pathPartsList[i]);
                        if (treeItem == null)
                        {
                            treeItem = new TreeViewItem() { Header = resultLineData.pathPartsList[i], FontWeight = FontWeights.Bold };
                            treeItemColleaction.Add(treeItem);
                        }
                        treeItemColleaction = treeItem.Items;
                    }
                    if (i == resultLineData.pathPartsList.Count - 1)
                    {
                        leafItem = new TreeViewItem()
                        {
                            Header = "(" + resultLineData.lineInFileNumbe.ToString() + @"/" + resultLineData.textInLineNumer.ToString() + ") : " + resultLineData.lineContent,
                            FontWeight = FontWeights.Normal
                        };
                        leafItem.MouseDoubleClick += OpenResultDocLine;
                        leafItem.MouseUp += PreviewResultDoc;
                        dictResultLines.Add(leafItem, resultLineData);
                        dictSearchSettings.Add(leafItem, last_searchSettings);
                        dictTBPreview.Add(leafItem, tbPreview);
                        treeItemColleaction.Add(leafItem);
                    }
                }
            }

            foreach (TreeViewItem tmpItem in tvResultTree.Items)
                JoinNodesWOLeafs(tmpItem);
            SetExpandAllInLvl(tvResultTree.Items, true);
        }

        internal void MoveResultToFlatTreeList(TreeView tvResultFlatTree, FindSettings last_searchSetting, TextBox tbPreview)
        {
            TreeViewItem treeItem;
            TreeViewItem leafItem;

            dictTVData[tvResultFlatTree] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName)
            };

            tvResultFlatTree.Items.Clear();
            foreach (ResultLineData resultLineData in resultList)
            {
                treeItem = GetItem(tvResultFlatTree.Items, resultLineData.linePath);
                if (treeItem == null)
                {
                    treeItem = new TreeViewItem() { Header = resultLineData.linePath, FontWeight = FontWeights.Bold };
                    tvResultFlatTree.Items.Add(treeItem);
                }
                leafItem = new TreeViewItem()
                {
                    Header = "(" + resultLineData.lineInFileNumbe.ToString() + @"/" + resultLineData.textInLineNumer.ToString() + ") : " + resultLineData.lineContent,
                    FontWeight = FontWeights.Normal
                };
                leafItem.MouseDoubleClick += OpenResultDocLine;
                leafItem.MouseUp += PreviewResultDoc;
                this.Focusable = false;
                dictResultLines.Add(leafItem, resultLineData);
                dictSearchSettings.Add(leafItem, last_searchSettings);
                dictTBPreview.Add(leafItem, tbPreview);
                treeItem.Items.Add(leafItem);
            }
            SetExpandAllInLvl(tvResultFlatTree.Items, true);
        }


        internal void SetExpandAllInLvl(ItemCollection treeItemColleaction, bool value)
        {
            if (treeItemColleaction == null || treeItemColleaction.Count == 0)
                return;
            foreach (TreeViewItem item in treeItemColleaction)
            {
                item.IsExpanded = value;
                SetExpandAllInLvl(item.Items, value);
            }
        }

        internal void SetHeaderShortLong(TreeView treeView, ItemCollection collection, bool blShort)
        {
            if (dictTVData.Count == 0)
                return;
            Debug.Assert(dictTVData.ContainsKey(treeView), "There is no tvData in the dictionary for " + treeView.Name);

            TVData tvData = dictTVData[treeView];
            if (blShort)
            {
                foreach(TreeViewItem item in collection)
                {
                    if (item.Items.Count != 0)
                    {
                        if (item.Header.ToString().StartsWith(tvData.longDir))
                            item.Header = tvData.shortDir + item.Header.ToString().Substring(tvData.longDir.Length);
                    }
                    else
                        SetHeaderShortLong(treeView, item.Items, blShort);
                }
            }
            else
            {
                foreach (TreeViewItem item in collection)
                {
                    if (item.Items.Count != 0)
                    {
                        if (item.Header.ToString().StartsWith(tvData.shortDir))
                            item.Header = tvData.longDir + item.Header.ToString().Substring(tvData.shortDir.Length);
                    }
                    else
                        SetHeaderShortLong(treeView, item.Items, blShort);
                }
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
                    treeItem.Header += @"\" + treeItem2.Header;
                }
            }
        }

        private void tb_Checked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(1, GridUnitType.Star);
            last_rowFlat.Height = new GridLength(0);
            last_tbFlatTree.Foreground = Brushes.Red;
        }

        private void tb_Unchecked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(0);
            last_rowFlat.Height = new GridLength(1, GridUnitType.Star);
            last_tbFlatTree.ClearValue(ToggleButton.ForegroundProperty);
        }

        private void btnAddSnapshot_Click(object sender, RoutedEventArgs e)
        {
            AddSmapshotTab();
            //TODO dodać na zakładkę nowe obiekty
            //todo dodać skrót wlaczający tool na pierwszą zakładkę
        }

        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            StartSearch();
        }

        private void btnUnExpAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpandAllInLvl(last_tvResultFlatTree.Items, false);
            SetExpandAllInLvl(last_tvResultTree.Items, false);
        }

        private void btnExpAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpandAllInLvl(last_tvResultFlatTree.Items, true);
            SetExpandAllInLvl(last_tvResultTree.Items, true);
        }

        private void rbLocation_Click(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = rbLocation.IsChecked == true;
            btnGetLocation.IsEnabled = rbLocation.IsChecked == true;
        }

        private void btnGetLocation_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (tbLocation.Text != "" && Directory.Exists(tbLocation.Text))
                dlg.SelectedPath = tbLocation.Text;
            if (System.Windows.Forms.DialogResult.OK == dlg.ShowDialog())
                tbLocation.Text = dlg.SelectedPath;
        }

        private void rbLocation_Checked(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = true;
            btnGetLocation.IsEnabled = true;
        }

        private void rbLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = false;
            btnGetLocation.IsEnabled = false;
        }

        private void last_shortDir_Checked(object sender, RoutedEventArgs e)
        {
            SetHeaderShortLong(last_tvResultFlatTree, last_tvResultFlatTree.Items, true);
            SetHeaderShortLong(last_tvResultTree, last_tvResultTree.Items, true);
            last_shortDir.Foreground = Brushes.Red;
        }

        private void last_shortDir_Unchecked(object sender, RoutedEventArgs e)
        {
            SetHeaderShortLong(last_tvResultFlatTree, last_tvResultFlatTree.Items, false);
            SetHeaderShortLong(last_tvResultTree, last_tvResultTree.Items, false);
            last_shortDir.ClearValue(ToggleButton.ForegroundProperty);
        }
    }
}