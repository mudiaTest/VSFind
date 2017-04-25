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
        FindSettings last_searchSettings = new FindSettings();
        Dictionary<TreeViewItem, ResultLine> dictResultLines = new Dictionary<TreeViewItem, ResultLine>();

        public string GetFindResults2Content()
        {
            var findWindow1 = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults1);
            var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);            
            findWindow.Activate();
            var selection = findWindow.Selection as EnvDTE.TextSelection;
            selection.SelectAll();
            var selection1 = findWindow1.Selection as EnvDTE.TextSelection;
            selection1.SelectAll();
            string result1 = selection1.Text;
            string result = selection.Text;
            return result;
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

        internal int ParseResultLine(
            string resultLineText, 
            out string linePath, 
            out List<string> linePathPartsList,
            out string lineContent, 
            out int? lineInFileNumber,
            out ResultLine resultLine)
        {
            String pathPart = "";
            linePath = "";
            lineContent = "";
            linePathPartsList = null;
            lineInFileNumber = null;
            resultLine = null;
            if (resultLineText.Trim() == "" || resultLineText.Trim().StartsWith("Matching lines:") || resultLineText.Trim().StartsWith("Find all:"))
                return 0;
            linePath = resultLineText.Substring(0, resultLineText.IndexOf(":", 10)).Trim();
            lineContent = resultLineText.Substring(resultLineText.IndexOf(":", 10) + 1).Trim();
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
            resultLine = new ResultLine(){
                resultLineText = resultLineText,
                linePath = linePath,
                linePathPartsList = linePathPartsList,
                lineContent = lineContent,
                lineInFileNumbe = lineInFileNumber
            };
            return 1;
        }

        public void OpenResultDocLine(object src, EventArgs args)
        {            
            ResultLine resultLine = dictResultLines[(TreeViewItem)src];
            int i = 0;
        }

        /*public void MoveResultToTreeViewModel(TreeView tvResultFlatTree)
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
        }*/

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
            ResultLine resultLine;
            TreeViewItem leafItem;

            tvResultTree.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r","\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach(string line in resList)
            {

                treeItemColleaction = tvResultTree.Items;
                treeItem = null;
                pathAgg = "";

                switch (ParseResultLine(line, out linePath, out linePathPartsList, out LineContent, out lineInFileNumber, out resultLine))
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
                            pathAgg = pathAgg + @"\" + pathPart;
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
                            {
                                leafItem = new TreeViewItem() {
                                    Header = "(" + lineInFileNumber.ToString() + ") : " + LineContent,
                                    FontWeight = FontWeights.Normal
                                };
                                leafItem.MouseDoubleClick += OpenResultDocLine;
                                dictResultLines.Add(leafItem, resultLine);
                                treeItemColleaction.Add(leafItem);
                            }
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
            string paramLine;
            ResultLine resultLine;
            TreeViewItem leafItem;

            tvResultFlatTree.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r", "\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach (string line in resList)
            {
                switch (ParseResultLine(line, out linePath, out linePathPartsList, out LineContent, out lineInFileNumber, out resultLine))
                {
                    case 0: //Line to skip
                        continue;
                    case 1: //proper line
                        treeItem = GetItem(tvResultFlatTree.Items, linePath);
                        if (treeItem == null)
                        {
                            treeItem = new TreeViewItem() { Header = linePath, FontWeight = FontWeights.Bold };
                            tvResultFlatTree.Items.Add(treeItem);
                        }
                        leafItem = new TreeViewItem()
                        {
                            Header = "(" + lineInFileNumber.ToString() + ") : " + LineContent,
                            FontWeight = FontWeights.Normal
                        };
                        leafItem.MouseDoubleClick += OpenResultDocLine;
                        dictResultLines.Add(leafItem, resultLine);
                        treeItem.Items.Add(leafItem);                      
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
            last_searchSettings.FillWraperPanel(last_infoWrapPanel);
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
            last_searchSettings.action = vsFindAction.vsFindActionFindAll;
            last_searchSettings.location = vsFindResultsLocation.vsFindResults2;

            //Search phrase
            last_searchSettings.tbPhrase = tbPhrase.Text;

            //Find options
            last_searchSettings.chkWholeWord = chkWholeWord.IsChecked == true;
            last_searchSettings.chkCase = chkCase.IsChecked == true;

            //Look in
            last_searchSettings.rbCurrDoc = rbCurrDoc.IsChecked == true;
            last_searchSettings.rbOpenDocs = rbOpenDocs.IsChecked == true;
            last_searchSettings.rbProject = rbProject.IsChecked == true;
            last_searchSettings.rbSolution = rbSolution.IsChecked == true;
            //Select last active document
            if (rbCurrDoc.IsChecked == true)
            {
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
                    tbiLastResult.IsSelected = true;
                    docIsSelected = false;
                }
            }
            /*TODO search in folder*/
            //find.SearchSubfolders = true; 

            /*Check the need for this code*/
            var x = dte.Find.FindWhat;

            //Remember original result
            //originalFindResult2 = GetFindResults2Content();
            if (docIsSelected)
            {
                var findWindow = dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
                last_searchSettings.ToFind(find);
                vsFindResult result = find.Execute();
                findWindow.Visible = false;
                tbiLastResult.IsSelected = true;
                //This will work for one document
                Finish();
            }
        }
    }
}
