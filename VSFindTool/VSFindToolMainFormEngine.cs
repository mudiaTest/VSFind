using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using System.IO;
using System.Diagnostics;

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
            //tbResult.Text = GetFindResults2Content();
        }

        public int ParseResultLine(string resultLine, out string linePath, out List<string> linePathPartsList, out string lineContent, out int? lineInFileNumber)
        {
            String pathPart = "";
            linePath = "";
            lineContent = "";
            linePathPartsList = null;
            lineInFileNumber = null;
            if (resultLine.Trim() == "" || resultLine.Trim().StartsWith("Matching lines:"))
                return 0;
            linePath = resultLine.Substring(0, resultLine.IndexOf(":", 10)).Trim();
            lineContent = resultLine.Substring(resultLine.IndexOf(":", 10) + 1).Trim();
            linePathPartsList = linePath.Split('\\').ToList<string>();

            for (int i = 0; i < linePathPartsList.Count; i++)
            {
                pathPart = linePathPartsList[i];
                if (i == linePathPartsList.Count - 1)
                {
                    lineInFileNumber = Int32.Parse(pathPart.Substring(pathPart.LastIndexOf("(") + 1, pathPart.LastIndexOf(")") - pathPart.LastIndexOf("(") - 1));
                    linePathPartsList[i] = pathPart.Substring(0, pathPart.LastIndexOf("("));
                    linePath = linePath.Substring(0, linePath.LastIndexOf("("));
                }
            }
            return 1;
        }

        public void MoveResultToTreeViewModel(TreeView tvResultFlatTree)
        {
            // Get raw family tree data from a database.
            List<ResultLine> listResultLine = new List<ResultLine>();           
            ResultLine resultLine;
            string linePath;
            string lineContent;
            List<string> linePathPartsList;
            int? lineInFileNumber;

            tvResultFlatTree.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r", "\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach (string line in resList)
            {
                switch (ParseResultLine(line, out linePath, out linePathPartsList, out lineContent, out lineInFileNumber))
                {
                    case 0: //Line to skip
                        continue;
                    case 1: //proper line
                        resultLine = listResultLine.Find( item => item.linePath == linePath );
                        if (resultLine == null)
                        {
                            resultLine = new ResultLine() { header = linePath, linePath = linePath, linePathPartsList = linePathPartsList };
                            listResultLine.Add(resultLine);
                        }
                        resultLine.subItems.Add(new ResultLine() { header = "(" + lineInFileNumber.ToString() + ") : " + lineContent, lineContent = lineContent, lineInFileNumber = lineInFileNumber });
                        break;
                    default:
                        Debug.Assert(false, "An exception has occured.");
                        break;
                }
            }
            // Create UI-friendly wrappers around the 
            // raw data objects (i.e. the view-model).
            resultTree = new ResultTreeViewModel(listResultLine[0]);

            // Let the UI bind to the view-model.
            tvResultFlatTree.DataContext = resultTree;
        }

        public void MoveResultToTreeList(TreeView tvResultTree)
        {
            ItemCollection treeItemColleaction;
            TreeViewItem treeItem;
            string pathAgg;
            string linePath;
            string LineContent;
            List<string> linePathPartsList;
            int? lineInFileNumber;
            String pathPart;

            tvResultTree.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r","\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach(string line in resList)
            {

                treeItemColleaction = tvResultTree.Items;
                treeItem = null;
                pathAgg = "";

                switch (ParseResultLine(line, out linePath, out linePathPartsList, out LineContent, out lineInFileNumber))
                { 
                case 0: //Line to skip
                    continue;
                case 1: //proper line
                    for (int i = 0; i < linePathPartsList.Count; i++)
                    {
                        pathPart = linePathPartsList[i];
                        if (pathAgg == "")
                            pathAgg = pathPart;
                        else
                            pathAgg = pathAgg + "\\" + pathPart;
                        if (Directory.Exists(pathAgg) || File.Exists(pathAgg))
                        {
                            treeItem = GetItem(treeItemColleaction, pathPart);
                            if (treeItem == null)
                            {
                                treeItem = new TreeViewItem() { Header = pathPart, FontWeight = FontWeights.Bold };
                                treeItemColleaction.Add(treeItem);
                            }
                            treeItemColleaction = treeItem.Items;
                        }
                        if (i == linePathPartsList.Count - 1)
                            treeItemColleaction.Add(new TreeViewItem() { Header = "(" + lineInFileNumber.ToString() + ") : " + LineContent, FontWeight = FontWeights.Normal });
                    }
                    break;
                    default:
                        Debug.Assert(false, "An exception has occured.");
                    break;
                }
            }
            foreach (TreeViewItem tmpItem in tvResultTree.Items)
                JoinNodesWOLeafs(tmpItem);
            SetExpandAllInLvl(tvResultTree.Items, true);
        }

        public void MoveResultToFlatTreeList(TreeView tvResultFlatTree)
        {
            TreeViewItem treeItem;
            string linePath;
            string LineContent;
            List<string> linePathPartsList;
            int? lineInFileNumber;

            tvResultFlatTree.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r", "\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach (string line in resList)
            {
                switch (ParseResultLine(line, out linePath, out linePathPartsList, out LineContent, out lineInFileNumber))
                {
                    case 0: //Line to skip
                        continue;
                    case 1: //proper line
                        treeItem = GetItem(tvResultFlatTree.Items, linePath);
                        if (treeItem == null)
                        {
                            treeItem = new TreeViewItem() { Header = linePath, FontWeight = FontWeights.ExtraBold };
                            tvResultFlatTree.Items.Add(treeItem);
                        }
                        treeItem.Items.Add(new TreeViewItem() { Header = "(" + lineInFileNumber.ToString() + ") : " + LineContent, FontWeight = FontWeights.Normal });
                        break;
                    default:
                        Debug.Assert(false, "An exception has occured.");
                        break;
                }
            }
            SetExpandAllInLvl(tvResultFlatTree.Items, true);
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
            //MoveResultToTextBox();
            MoveResultToTreeList(last_tvResultTree);
            MoveResultToFlatTreeList(last_tvResultFlatTree);
            //MoveResultToTreeViewModel();

            /*List<string> list = tbResult.Text.Split('\n').ToList<string>();
            if (list[list.Count - 2].StartsWith("Matching lines"))
                BringBackFindResult2Value();
            else
                HideFindResult2Window();*/
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

        private string GetSnapshotTag(int number)
        {
            return "snap" + number.ToString();
        }

        private string GetSnapshotTag(string number)
        {
            return "snap" + number;
        }

        private void ExecSearch()
        {
            Boolean docIsSelected = true;
            LastDocWindow = ((VSFindTool.VSFindToolPackage)(this.parentToolWindow.Package)).LastDocWindow;
            //tbResult.Text = "";
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
                    //tbResult.Text = "No document is active.";
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
