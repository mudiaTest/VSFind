﻿using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using System.IO;
using System.Diagnostics;
using VSHierarchyAddin;
using Extensibility;

using System.ComponentModel.Composition; //[Import]
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations; //ITextSearchService, ITextStructureNavigatorSelectorService
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TextManager.Interop;

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
        Dictionary<TreeViewItem, ResultLineData> dictResultLines = new Dictionary<TreeViewItem, ResultLineData>();
        Dictionary<TreeViewItem, FindSettings> dictSearchSettings = new Dictionary<TreeViewItem, FindSettings>();
        Dictionary<TreeViewItem, TextBox> dictTBPreview = new Dictionary<TreeViewItem, TextBox>();
        Dictionary<TreeView, TVData> dictTVData = new Dictionary<TreeView, TVData>();


        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }


        Connect c = new Connect();

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
            out ResultLineData resultLine)
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
            resultLine = new ResultLineData(){
                resultLineText = resultLineText,
                linePath = linePath,
                linePathPartsList = linePathPartsList,
                lineContent = lineContent,
                lineInFileNumbe = lineInFileNumber
            };
            return 1;
        }

        public void Test()
        {            
            Array custom = new string[1000];
            c.OnConnection(dte.Application, ext_ConnectMode.ext_cm_AfterStartup, this, ref custom);
            var i = 0;
        }

        public void TestOpenResultDocLine(object src, EventArgs args)
        {
            ResultLineData resultLine = dictResultLines[(TreeViewItem)src];
            FindSettings settings = dictSearchSettings[(TreeViewItem)src];
            if (dte != null)
            {

                /*Guid logicalView = Microsoft.VisualStudio.VSConstants.LOGVIEWID_Debugging;//różne typy edytora? inny np LOGVIEWID_Debugging
                string caption = "TESST";
                uint grfOpenStandard = (uint)__VSOSEFLAGS.OSE_UseOpenWithDialog; //Flagi z Microsoft.VisualStudio.Shell.Interop.__VSOSEFLAGS

                //IVsSolution solution = (IVsSolution)GetService(serviceProvider, typeof(SVsSolution), typeof(IVsSolution));

                //EnvDTE.Project proj;
                //object service = GetService(proj.DTE, typeof(IVsSolution));
                //IVsSolution solution = (IVsSolution)dte.Solution;
                //((VSFindToolPackage)(this.parentToolWindow).Package).
                
                //IVsSolution solution = (IVsSolution)service; 
                //solution.GetProjectOfUniqueName(proj.UniqueName, out hierarchy);

                IVsUIShellOpenDocument.OpenStandardEditor(
                    grfOpenStandard,
                    resultLine.linePath,
                    ref logicalView, 
                    caption, 
                    (IVsUIHierarchy)c.hierarchy[c.projects[0]], 
                    this.Node.ID, 
                    docDataExisting, 
                    serviceProvider,
                    out windowFrame
                );
                */ 
            }
            else
                Debug.Assert(false, "Brak DTE");
            int i = 0;
        }

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
                    selection.MoveToLineAndOffset(Math.Max(0, resultLine.lineInFileNumbe.Value - 2), 1, false);                    
                    selection.MoveToLineAndOffset(Math.Max(0, resultLine.lineInFileNumbe.Value + 2), 1, true);;
                    selection.EndOfLine(true);
                    dictTBPreview[(TreeViewItem)src].Text = selection.Text;
                    selection.GotoLine(resultLine.lineInFileNumbe.Value, false);
                    if (settings.chkRegExp == true)
                        Debug.Assert(false, "Brak obsługi RegExp");
                    else
                    {
                        //in case there are many hits in on line we'll jump to specyfic one
                        for (int j = 0; j < resultLine.textInLineNumer; j++)
                            selection.FindText(settings.tbPhrase, settings.GetVsFindOptions());
                        selection.FindText(settings.tbPhrase, settings.GetVsFindOptions());
                        Debug.Assert(
                            resultLine.lineInFileNumbe == selection.CurrentLine,
                            String.Format("Linia wyniku ({0}) nie jest zgodna z aktualnie wybraną ({1})", resultLine.lineInFileNumbe, selection.CurrentLine)
                        );
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

        /*public void MoveResultToTreeViewModel(TreeView tvResultFlatTree)
        {
            // Get raw family tree data from a database.
            List<ResultLineData> listResultLine = new List<ResultLineData>();           
            ResultLineData resultLine;
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
                            resultLine = new ResultLineData() { header = linePath, linePath = linePath, linePathPartsList = linePathPartsList };
                            listResultLine.Add(resultLine);
                        }
                        resultLine.subItems.Add(new ResultLineData() { header = "(" + lineInFileNumber.ToString() + ") : " + lineContent, lineContent = lineContent, lineInFileNumber = lineInFileNumber });
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

        internal void MoveResultToTreeList(TreeView tvResultTree, FindSettings last_searchSettings, TextBox tbPreview)
        {
            ItemCollection treeItemColleaction;
            TreeViewItem treeItem;
            string pathAgg;
            string linePath;
            string LineContent;
            List<string> linePathPartsList;
            int? lineInFileNumber;
            String pathPart;
            ResultLineData resultLineObject;
            TreeViewItem leafItem;
            String prvLine = "";
            int sameLineCounter = 0;

            dictTVData[tvResultTree] = new TVData() {
                longDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName)
            };

            tvResultTree.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r","\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach(string line in resList)
            {

                treeItemColleaction = tvResultTree.Items;
                treeItem = null;
                pathAgg = "";

                if (prvLine == line)
                    sameLineCounter++;
                else
                    sameLineCounter = 0;

                switch (ParseResultLine(line, out linePath, out linePathPartsList, out LineContent, out lineInFileNumber, out resultLineObject))
                {                    
                    case 0: //Line to skip
                        continue;
                    case 1: //proper line
                        resultLineObject.textInLineNumer = sameLineCounter;
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
                                leafItem.MouseUp += PreviewResultDoc;
                                dictResultLines.Add(leafItem, resultLineObject);
                                dictSearchSettings.Add(leafItem, last_searchSettings);
                                dictTBPreview.Add(leafItem, tbPreview);
                                treeItemColleaction.Add(leafItem);
                            }
                        }
                        prvLine = line;
                        break;
                        default:
                            Debug.Assert(false, "An exception has occured.");
                        break;
                }
                resultLineObject.solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
            }
            foreach (TreeViewItem tmpItem in tvResultTree.Items)
                JoinNodesWOLeafs(tmpItem);
            SetExpandAllInLvl(tvResultTree.Items, true);
        }

        internal void MoveResultToFlatTreeList(TreeView tvResultFlatTree, FindSettings last_searchSetting, TextBox tbPreview)
        {
            TreeViewItem treeItem;
            string linePath;
            string LineContent;
            List<string> linePathPartsList;
            int? lineInFileNumber;
            string paramLine;
            ResultLineData resultLineObject;
            TreeViewItem leafItem;
            String prvLine = "";
            int sameLineCounter = 0;

            dictTVData[tvResultFlatTree] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName)
            };

            tvResultFlatTree.Items.Clear();
            List<string> resList = GetFindResults2Content().Replace("\n\r", "\n").Split('\n').ToList<string>();
            resList.RemoveAt(0);
            foreach (string line in resList)
            {
                if (prvLine == line)
                    sameLineCounter++;
                else
                    sameLineCounter = 0;

                switch (ParseResultLine(line, out linePath, out linePathPartsList, out LineContent, out lineInFileNumber, out resultLineObject))
                {
                    case 0: //Line to skip
                        continue;
                    case 1: //proper line
                        resultLineObject.textInLineNumer = sameLineCounter;
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
                        leafItem.MouseUp += PreviewResultDoc;
                        this.Focusable = false;
                        dictResultLines.Add(leafItem, resultLineObject);
                        dictSearchSettings.Add(leafItem, last_searchSettings);
                        dictTBPreview.Add(leafItem, tbPreview);
                        treeItem.Items.Add(leafItem);
                        prvLine = line;
                        break;
                    default:
                        Debug.Assert(false, "An exception has occured.");
                        break;
                }
                resultLineObject.solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
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
            MoveResultToTreeList(last_tvResultTree, last_searchSettings, last_TBPreview);
            MoveResultToFlatTreeList(last_tvResultFlatTree, last_searchSettings, last_TBPreview);
            SetHeaderShortLong(last_tvResultFlatTree, last_tvResultFlatTree.Items, last_shortDir.IsChecked.Value);
            SetHeaderShortLong(last_tvResultTree, last_tvResultTree.Items, last_shortDir.IsChecked.Value);
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
            /*
            FindData findData = new FindData(currentWord.GetText(), currentWord.Snapshot);
            SnapshotSpan currentWord = this.View.Selection.StreamSelectionSpan.SnapshotSpan;
            if (CurrentWord.HasValue && currentWord == CurrentWord)
            {
                return;
            }
            */ 

            Boolean docIsSelected = true;
            LastDocWindow = ((VSFindTool.VSFindToolPackage)(this.parentToolWindow.Package)).LastDocWindow;
            //tbResult.Text = "";
            /*if (dte != null)
            {
                var m_findEvents = dte.Events.FindEvents;
                m_findEvents.FindDone += new EnvDTE._dispFindEvents_FindDoneEventHandler(m_findEvents_FindDone);
            }*/

            Find find = dte.Find;
            last_searchSettings.action = vsFindAction.vsFindActionFindAll;
            last_searchSettings.location = vsFindResultsLocation.vsFindResults2;

            //Search phrase
            last_searchSettings.tbPhrase = tbPhrase.Text;

            //Find options
            last_searchSettings.chkWholeWord = chkWholeWord.IsChecked == true;
            last_searchSettings.chkCase = chkCase.IsChecked == true;
            last_searchSettings.chkRegExp = chkRegExp.IsChecked == true;

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


            TextSelection selection = ((EnvDTE.TextSelection)dte.ActiveDocument.Selection);
            if (selection != null)
            {
                selection.SelectAll();

                //SnapshotSpan currentWord = selection.StreamSelectionSpan.SnapshotSpan;

                if (textManager == null)
                    return;
                IVsTextView textView = null;
                textManager.GetActiveView(1, null, out textView);
                if (textView == null)
                    return;



                FindData findData = new FindData();// (last_searchSettings.tbPhrase, currentWord.Snapshot);
                //findData.TextStructureNavigator = TextStructureNavigatorSelector;

                last_searchSettings.FillFindData(findData);
                //findData.SearchString = last_searchSettings.tbPhrase;
                System.Collections.ObjectModel.Collection< SnapshotSpan> coll = TextSearchService.FindAll(findData);
                var i = 0;
            }
        }

        private void ExecSearch2()
        {

            //Test();

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
            last_searchSettings.chkRegExp = chkRegExp.IsChecked == true;

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
