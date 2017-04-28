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
using System.IO;
using System.Windows.Forms;

namespace VSFindTool
{
    using System.Windows.Controls.Primitives;
    /// <summary>
    /// Interaction logic for VSFindToolMainFormControl.xaml
    /// </summary>
    public partial class VSFindToolMainFormControl : System.Windows.Controls.UserControl
    {
        public VSFindToolMainForm parentToolWindow;
        //List<string> resList;
        public EnvDTE.Window LastDocWindow;
        EnvDTE80.DTE2 dte {
            get
            {
                return ((VSFindToolPackage)parentToolWindow.Package).dte2;
            }
        }
        string originalFindResult2;
        Dictionary<string, FindSettings> findSettings = new Dictionary<string, FindSettings>();

        //ResultTreeViewModel resultTree;

        public VSFindToolMainFormControl()
        {
            InitializeComponent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        //[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]

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

        private void rbLocation_Click(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = rbLocation.IsChecked == true;
            btnGetLocation.IsEnabled = rbLocation.IsChecked == true;
        }

        private void btnGetLocation_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (tbLocation.Text != "" && Directory.Exists(tbLocation.Text))
                dlg.SelectedPath = tbLocation.Text;
            if (DialogResult.OK == dlg.ShowDialog())
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
    }
}