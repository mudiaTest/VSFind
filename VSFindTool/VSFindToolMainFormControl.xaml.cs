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
namespace VSFindTool
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

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
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(
            //    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
            //    "VSFindToolMainForm");
            TestSearch1();
        }

        public string GetFindResults2()
        {
            var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
            var selection = findWindow.Selection as EnvDTE.TextSelection;
            selection.SelectAll();
            return selection.Text;

            /*var endPoint = selection.AnchorPoint.CreateEditPoint();
            endPoint.EndOfDocument();
            return endPoint.GetLines(1, endPoint.Line);*/
        }

        public void HideFindResult2Window()
        {
            var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
            findWindow.Visible = false; 
        }

        public void BringBackFindResult2Value()
        {
            var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
            var selection = findWindow.Selection as EnvDTE.TextSelection;
            selection.StartOfDocument();
            selection.Delete(2);
            //selection.Insert("rrr", 1);
            var i = 0;
        }

        public void GetResults()
        {
            tbResult.Text = GetFindResults2();
            List<string> list = tbResult.Text.Split('\n').ToList<string>();
            if (list[list.Count - 2].StartsWith("Matching lines"))
                BringBackFindResult2Value();
            else
                HideFindResult2Window();            
        }

        public void m_findEvents_FindDone(EnvDTE.vsFindResult Result, bool Cancelled)
        {
            GetResults();
        }

        public void m_WindowEvent(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
        {
            foreach(EnvDTE.Document doc in dte.Documents)
            {
                foreach(EnvDTE.Window docWindow in doc.Windows)
                {
                    if (docWindow == GotFocus)
                    {
                        LastDocWindow = GotFocus;
                        return;
                    }
                    
                }
            }
        }

        private void TestSearch1()
        {
            Boolean docSelected = true;
            LastDocWindow = ((VSFindTool.VSFindToolPackage)(this.parentToolWindow.Package)).LastDocWindow;
            tbResult.Text = "";
            if (dte != null)
            {
                var m_findEvents = dte.Events.FindEvents;
                m_findEvents.FindDone += new EnvDTE._dispFindEvents_FindDoneEventHandler(m_findEvents_FindDone);
            }

            Find find = dte.Find;
            find.Action = vsFindAction.vsFindActionFindAll;
            find.ResultsLocation = vsFindResultsLocation.vsFindResults2;

            //Szukana fraza
            find.FindWhat = tbPhrase.Text;

            //Find options
            find.MatchWholeWord = chkWholeWord.IsChecked == true;
            find.MatchCase = chkCase.IsChecked == true;
            if (chkRegExp.IsChecked == true)
                find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxRegExpr;
            else
                find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxLiteral;

            //Look in
            if (rbCurrDoc.IsChecked == true)
            {
                find.Target = vsFindTarget.vsFindTargetCurrentDocument;

                if (dte.ActiveDocument != null && dte.ActiveDocument.ActiveWindow != null)
                {
                    dte.ActiveDocument.ActiveWindow.Activate();
                }
                else if (LastDocWindow != null)
                {
                    LastDocWindow.Activate();
                }
                else
                {
                    tbResult.Text = "No document is active.";
                    tbiLastResult.IsSelected = true;
                    docSelected = false;
                }
            }
            else if (rbOpenDocs.IsChecked == true)
                find.Target = vsFindTarget.vsFindTargetOpenDocuments;
            else if (rbProject.IsChecked == true)
                find.Target = vsFindTarget.vsFindTargetCurrentProject;
            else if (rbSolution.IsChecked == true)
                find.Target = vsFindTarget.vsFindTargetSolution;

             //find.SearchSubfolders = true;
            var x = dte.Find.FindWhat;

            originalFindResult2 = GetFindResults2();
            if (docSelected)
            {
                var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);                
                vsFindResult result = find.Execute();
                findWindow.Visible = false; 
                tbiLastResult.IsSelected = true;
                GetResults();
            }
        }
    }
}