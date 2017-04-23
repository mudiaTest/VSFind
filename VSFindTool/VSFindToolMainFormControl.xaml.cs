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

namespace VSFindTool
{
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
        }

        private void tb_Unchecked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(0);
            last_rowFlat.Height = new GridLength(1, GridUnitType.Star);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string snapshotNumber = (tbcMain.Items.Count - 2).ToString();
            string snapshotTag = GetSnapshotTag(snapshotNumber);

            TabItem newTab = new TabItem() { Name = "tbi" + snapshotTag, 
                                             Header = "Snap " + (tbcMain.Items.Count - 2).ToString() };
            tbcMain.Items.Add(newTab);
            Grid newGrid = new Grid();
            newGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30, GridUnitType.Pixel) });
            newGrid.RowDefinitions.Add(new RowDefinition() { Name = snapshotTag + "_rowFlat" });
            newGrid.RowDefinitions.Add(new RowDefinition() { Name = snapshotTag + "t_rowTree", Height = new GridLength(0, GridUnitType.Pixel) });
            //Todo - dodać nową zakładkę o nazwie odebranej od uzytkowniak
            //TODO dodać na zakładkę nowe obiekty
            //todo dodać navigatora
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