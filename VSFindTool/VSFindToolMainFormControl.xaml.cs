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


        public void TVResultSetExpandAllInLvl(ItemCollection treeItemColleaction, bool value)
        {            
            if (treeItemColleaction == null || treeItemColleaction.Count == 0)
                return;
            foreach (TreeViewItem item in treeItemColleaction)
            {
                item.IsExpanded = true;
                TVResultSetExpandAllInLvl(item.Items, value);
            }
        }
    }
}