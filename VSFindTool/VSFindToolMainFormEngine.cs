using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using System.IO;

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
        public string GetFindResults2Content()
        {
            var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
            var selection = findWindow.Selection as EnvDTE.TextSelection;
            selection.SelectAll();
            return selection.Text;
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
            /*Doesn't work*/
            var i = 0;
        }

        public void MoveResultToTextBox()
        {
            tbResult.Text = GetFindResults2Content();
        }

        public void MoveResultToTreeList()
        {
            //TODO

            ItemCollection treeItemColleaction;
            TreeViewItem treeItem;
            String pathAgg;
            String linePath;
            String LineContent;

            tvResult.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r","\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach(string line in resList)
            {
                treeItemColleaction = tvResult.Items;
                treeItem = null;
                pathAgg = "";

                if (line.Trim() == "" || line.Trim().StartsWith("Matching lines:"))
                    continue;
                linePath = line.Substring(0, line.IndexOf(":", 10)).Trim();
                LineContent = line.Substring(line.IndexOf(":", 10)+1).Trim();

                foreach (string pathPart in linePath.Split('\\').ToList<string>())
                {
                    if (pathAgg == "")
                        pathAgg = pathPart;
                    else
                        pathAgg = pathAgg + "\\" + pathPart;
                    if (Directory.Exists(pathAgg))
                    {
                        treeItem = GetItem(treeItemColleaction, pathPart);
                        if (treeItem == null)
                        {
                            treeItem = new TreeViewItem() { Header = pathPart };
                            treeItemColleaction.Add(treeItem);                            
                        }
                        treeItemColleaction = treeItem.Items;
                    }
                    else if (File.Exists(pathAgg))
                    {
                        treeItemColleaction.Add(new TreeViewItem() { Header = pathPart });
                    }
                    else
                    {
                        //Assert
                        treeItemColleaction.Add(new TreeViewItem() { Header = pathPart + '(' + LineContent + ')' });
                        var i = 0;
                    }

                    //treeItemContainer.Items
                }
                //var  = Path.Get;
                //treeItem = new TreeViewItem();
                //treeItem.Header = "stTmp";
                //tvResult.Items.Add(treeItem);
            }
            TVResultSetExpandAllInLvl(tvResult.Items, true);
        }

         

        private TreeViewItem GetItem(ItemCollection colleation, string pathPart)
        {
            foreach (TreeViewItem item in colleation)
            {
                if (item.Header.ToString() == pathPart)
                    return item;
            }
            return null;
        }

        public void Finish()
        {
            MoveResultToTextBox();
            MoveResultToTreeList();
            List<string> list = tbResult.Text.Split('\n').ToList<string>();
            if (list[list.Count - 2].StartsWith("Matching lines"))
                BringBackFindResult2Value();
            else
                HideFindResult2Window();
        }

        public void m_findEvents_FindDone(EnvDTE.vsFindResult Result, bool Cancelled)
        {
            //This will work for many documents
            Finish();
        }

        public void m_WindowEvent(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
        {
            foreach (EnvDTE.Document doc in dte.Documents)
            {
                foreach (EnvDTE.Window docWindow in doc.Windows)
                {
                    if (docWindow == GotFocus)
                    {
                        LastDocWindow = GotFocus;
                        return;
                    }

                }
            }
        }

        private void ExecSearch()
        {
            Boolean docIsSelected = true;
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

            //Search phrase
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
                    docIsSelected = false;
                }
            }
            else if (rbOpenDocs.IsChecked == true)
                find.Target = vsFindTarget.vsFindTargetOpenDocuments;
            else if (rbProject.IsChecked == true)
                find.Target = vsFindTarget.vsFindTargetCurrentProject;
            else if (rbSolution.IsChecked == true)
                find.Target = vsFindTarget.vsFindTargetSolution;
            /*TODO search in folder*/
            //find.SearchSubfolders = true; 

            /*Check the need for this code*/
            var x = dte.Find.FindWhat;

            //Remember original result
            originalFindResult2 = GetFindResults2Content();
            if (docIsSelected)
            {
                var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
                vsFindResult result = find.Execute();
                findWindow.Visible = false;
                tbiLastResult.IsSelected = true;
                //This will work for one document
                Finish();
            }
        }
    }
}
