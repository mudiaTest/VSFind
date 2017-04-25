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
        //List<string> resList;
        public EnvDTE.Window LastDocWindow;
        EnvDTE.DTE dte;
        string originalFindResult2;
        Dictionary<string, FindSettings> findSettings = new Dictionary<string, FindSettings>();

        //ResultTreeViewModel resultTree;

        public VSFindToolMainFormControl()
        {
            this.InitializeComponent();
            dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]

        public void SetExpandAllInLvl(ItemCollection treeItemColleaction, bool value)
        {
            if (treeItemColleaction == null || treeItemColleaction.Count == 0)
                return;
            foreach (TreeViewItem item in treeItemColleaction)
            {
                item.IsExpanded = value;
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
                    treeItem.Header += @"\" + treeItem2.Header;
                }
            }
        }

        private void tb_Checked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(1, GridUnitType.Star);
            last_rowFlat.Height = new GridLength(0);
            last_tb.Foreground = Brushes.Red;
        }

        private void tb_Unchecked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(0);
            last_rowFlat.Height = new GridLength(1, GridUnitType.Star);
            last_tb.ClearValue(ToggleButton.ForegroundProperty);
        }

        private void btnAddSnapshot_Click(object sender, RoutedEventArgs e)
        {
            AddSmapshotTab();
            //TODO dodać na zakładkę nowe obiekty
            //todo dodać skrót wlaczający tool na pierwszą zakładkę
        }

        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            ExecSearch();
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
    }
}