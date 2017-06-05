//------------------------------------------------------------------------------
// <copyright file="VSFindToolMainFormControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Media;

namespace VSFindTool
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    /// <summary>
    /// Interaction logic for VSFindToolMainFormControl.
    /// </summary>
    public partial class VSFindToolMainFormControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VSFindToolMainFormControl"/> class.
        /// </summary>
        public VSFindToolMainForm parentToolWindow;
        public EnvDTE.Window LastDocWindow;
        //internal string OuterSelectedText;
        EnvDTE80.DTE2 Dte
        {
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

        IVsTextManager TextManager
        {
            get
            {
                return ((VSFindToolPackage)parentToolWindow.Package).textManager;
            }
        }


        public VSFindToolMainFormControl()
        {
            InitializeComponent();
            Init();
        }

        internal void Init()
        {
            last_shortDir.IsChecked = true;
            FileMask.FillCB(cbFileMask);
            cbFileMask.SelectedIndex = 0;
            dictTBPreview.Add(lastSearchSettings, last_TBPreview);
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]



        /*Shortcut actions*/
        internal void DoFocus()
        {
            EnvDTE.Window window = ((VSFindTool.VSFindToolPackage)(this.parentToolWindow.Package)).LastDocWindow;
            if (window != null)
            {
                EnvDTE.TextSelection selection = GetSelection(window);
                if (selection != null && selection.Text != "")
                    tbPhrase.Text = selection.Text;
            }
            tbiSearch.IsSelected = true; //use instwad of "tbcMain.SelectedIndex = 0;"
            System.Windows.Input.FocusManager.SetFocusedElement(tbiSearch, tbPhrase); //use instead of "tbPhrase.Focus();"

        }

        internal void ShowResults()
        {
            tbiLastResult.IsSelected = true;
        }



        /*Filling and actions of result TreeViews*/
        internal void MoveResultToTreeList(TreeView tvResultTree, FindSettings last_searchSettings, TextBox tbPreview, List<ResultItem> resultList, bool partialMode = true)
        {
            List<ResultItem> tmp = new List<ResultItem>();
            foreach (ResultItem resultItem in resultList)
            {
                tmp.Add(resultItem.GetCopy());
            }

            ItemCollection treeItemColleaction;
            string pathAgg;
            string content = "";
            TreeViewItem treeItem;
            TreeViewItem leafItem;

            dictTVData[tvResultTree] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName)
            };

            if (!partialMode)
                tvResultTree.Items.Clear();

            foreach (ResultItem resultItem in resultList)
            {
                treeItemColleaction = tvResultTree.Items;
                treeItem = null;
                pathAgg = "";
                for (int i = 0; i < resultItem.PathPartsList.Count; i++)
                {
                    if (pathAgg == "")
                        pathAgg = resultItem.PathPartsList[i];
                    else
                        pathAgg = pathAgg + @"\" + resultItem.PathPartsList[i];
                    if (Directory.Exists(pathAgg) || File.Exists(pathAgg))
                    {
                        treeItem = GetTVItemByFilePath(treeItemColleaction, resultItem.PathPartsList[i]);
                        if (treeItem == null)
                        {
                            treeItem = new TreeViewItem() {
                                Header = resultItem.PathPartsList[i],
                                FontWeight = FontWeights.Bold
                            };
                            treeItemColleaction.Add(treeItem);
                        }
                        treeItemColleaction = treeItem.Items;
                    }
                    if (i == resultItem.PathPartsList.Count - 1)
                    {
                        content = resultItem.lineContent.Trim();
                        leafItem = new TreeViewItem()
                        {                          
                            Header = "(" + resultItem.lineNumber.ToString() + @"/" + resultItem.resultOffset.ToString() + ") : " + content.Substring(0, Math.Min(resultItem.lineContent.Trim().Length - 1, 300)),
                            FontWeight = FontWeights.Normal
                        };
                        resultItem.belongsToLastResults = true;
                        leafItem.MouseDoubleClick += OpenResultDocLine;
                        leafItem.MouseUp += PreviewResultDoc;
                        leafItem.MouseRightButtonUp += ShowResultTreeContextMenu;
                        //leafItem.ContextMenu = (ContextMenu)this.Resources["TVContextMenu"];
                       // leafItem.ContextMenu = new ContextMenu();
                        dictResultItems.Add(leafItem, resultItem);
                        dictSearchSettings.Add(leafItem, last_searchSettings);                        
                        treeItemColleaction.Add(leafItem);
                    }
                }
            }

            if (!partialMode)
                foreach (TreeViewItem tmpItem in tvResultTree.Items)
                    JoinNodesWOLeafs(tmpItem);
            SetExpandAllInLvl(tvResultTree.Items, true);
        }

        internal void MoveResultToFlatTreeList(TreeView tvResultFlatTree, FindSettings last_searchSetting, TextBox tbPreview, List<ResultItem> resultList, bool partialMode = true)
        {
            List<ResultItem> tmp = new List<ResultItem>();
            string content = "";
            foreach (ResultItem resultItem in resultList)
            {
                tmp.Add(resultItem.GetCopy());
            }

            TreeViewItem treeItem;
            TreeViewItem leafItem;
            
            dictTVData[tvResultFlatTree] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName)
            };

            if (!partialMode)
                tvResultFlatTree.Items.Clear();        

            foreach (ResultItem resultItem in resultList)
            {
                treeItem = GetTVItemByFilePath(tvResultFlatTree.Items, resultItem.linePath);
                content = resultItem.lineContent.Trim();
                if (treeItem == null)
                {
                    treeItem = new TreeViewItem() { Header = resultItem.linePath, FontWeight = FontWeights.Bold };
                    tvResultFlatTree.Items.Add(treeItem);
                }
                leafItem = new TreeViewItem()
                {
                    Header = "(" + resultItem.lineNumber.ToString() + @"/" + resultItem.resultOffset.ToString() + ") : " + content.Substring(0, Math.Min(resultItem.lineContent.Trim().Length - 1, 300)),
                    FontWeight = FontWeights.Normal
                };

                resultItem.belongsToLastResults = true;
                leafItem.MouseDoubleClick += OpenResultDocLine;
                leafItem.MouseUp += PreviewResultDoc;
                leafItem.MouseRightButtonUp += ShowResultTreeContextMenu;
                this.Focusable = false;
                dictResultItems.Add(leafItem, resultItem);
                dictSearchSettings.Add(leafItem, lastSearchSettings);
                treeItem.Items.Add(leafItem);
            }
            SetExpandAllInLvl(tvResultFlatTree.Items, true);
        }

        internal void FillTVIList(Dictionary<string, TreeViewItem> tviList, ItemCollection items)
        {
            foreach (TreeViewItem item in items)
            {
                tviList.Add(dictResultItems[item].linePath, item);
                FillTVIList(tviList, item.Items);
            }
        }

        internal void RebuildTVTreeList(TreeView tvResultTree)
        {
           /* Dictionary<string, TreeViewItem> tviList = new Dictionary<string, TreeViewItem>();
            ItemCollection items = tvResultTree.Items;
            TreeViewItem treeItem;
            FillTVIList(tviList, tvResultTree.Items);
            items.Clear();
            foreach (KeyValuePair<string, TreeViewItem> pair in tviList)
            {
                if ()
            }*/
        }

        internal void ClearTV(TreeView tvResultFlatTree, TreeView tvResultTree)
        {
            tvResultFlatTree.Items.Clear();
            tvResultTree.Items.Clear();
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
                foreach (TreeViewItem item in collection)
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

        public void JoinNodesWOLeafs(TreeView tree)
        {
            foreach (TreeViewItem tmpItem in tree.Items)
                JoinNodesWOLeafs(tmpItem);
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


        internal TextSelection OpenDocGetSelection(ResultItem resultLine, FindSettings settings, EventArgs args, bool focus = true)
        {
            TextSelection selection = null;
            if (Dte != null)
            {
                EnvDTE.Window docWindow = Dte.ItemOperations.OpenFile(resultLine.linePath, Constants.vsViewKindTextView);
                selection = GetSelection(Dte.ActiveDocument);
                if (selection != null)
                {
                    selection.SelectAll();
                    int lastLine = selection.CurrentLine;
                    selection.MoveToLineAndOffset(Math.Max(1, resultLine.lineNumber.Value - 2), 1, false);
                    selection.MoveToLineAndOffset(Math.Min(lastLine, resultLine.lineNumber.Value + 4), 1, true);
                    selection.EndOfLine(true);
                    dictTBPreview[settings].Text = selection.Text;

                    selection.GotoLine(resultLine.lineNumber.Value, false);
                    if (settings.chkRegExp == true)
                        Debug.Assert(false, "Brak obsługi RegExp");
                    else
                    {
                        selection.MoveToLineAndOffset(resultLine.lineNumber.Value + 1, resultLine.resultOffset + 1, false);
                        selection.MoveToLineAndOffset(resultLine.lineNumber.Value + 1, resultLine.resultOffset + resultLine.resultLength + 1, true);
                    }
                    //Add action to set focus no doc window after finishing all action in queue (currenty there should be only double click event) 
                    if (focus)
                    {
                        Action showAction = () => docWindow.Activate();
                        this.Dispatcher.BeginInvoke(showAction);
                    }
                }
            }
            else
                Debug.Assert(false, "Brak DTE");
            return selection;
        }

        internal void OpenResultDocLine(object src, EventArgs args)
        {
            ResultItem resultLine = dictResultItems[(TreeViewItem)src];
            FindSettings settings = dictSearchSettings[(TreeViewItem)src];
            OpenDocGetSelection(resultLine, settings, args, true);
        }

        public void PreviewResultDoc(object src, EventArgs args)
        {
            ResultItem resultLine = dictResultItems[(TreeViewItem)src];
            FindSettings settings = dictSearchSettings[(TreeViewItem)src];
            TextBox tbPreview = dictTBPreview[settings];
            tbPreview.Text = "";
            int lineNumber = 0;

            EnvDTE.Document document = GetDocumentByPath(resultLine.linePath);
            if (document != null)
            {
                EnvDTE.TextSelection selection = GetSelection(document);
                if (selection != null)
                {
                    selection.EndOfDocument();
                    int docLength = selection.CurrentLine;
                    selection.GotoLine(Math.Max(0, resultLine.lineNumber.Value - 1), false);
                    selection.LineDown(true, Math.Min(5, docLength - resultLine.lineNumber.Value + 3));
                    tbPreview.Text = selection.Text;
                    return;
                }
            }
                        
            using (var reader = new StreamReader(resultLine.linePath))
            {
                string line;
                while (lineNumber <= Math.Max(0, resultLine.lineNumber.Value + 2))
                {
                    if (reader.EndOfStream)
                        return;
                    lineNumber++;
                    line = reader.ReadLine();
                    if (lineNumber >= Math.Max(0, resultLine.lineNumber.Value - 2) && lineNumber <= Math.Max(0, resultLine.lineNumber.Value + 2))
                        tbPreview.AppendText((tbPreview.Text != "" ? Environment.NewLine : "") + line);
                }
            }            
        }

        public void OpenInFolder(object src, EventArgs args)
        {
            string app = "explorer.exe";
            string selectFile = "/select, \"" + dictResultItems[dictContextMenu[(MenuItem)src]].linePath + "\"";
            System.Diagnostics.Process.Start(app, selectFile);
        }

        public void ReplaceSpecyfic(object src, EventArgs args)
        {
            List<ResultItem> changedResults = new List<ResultItem>();
            String strReplace = "ttt";
            ResultItem resultItem = dictResultItems[dictContextMenu[(MenuItem)src]];
            FindSettings settings = lastSearchSettings;
            if (resultItem.replaced)
            {
                System.Windows.Forms.MessageBox.Show("Result has already been changed.");
                return;
            }            
            TextSelection selection = OpenDocGetSelection(resultItem, settings, args, false);
            selection.Text = strReplace;
            foreach (KeyValuePair<TreeViewItem, ResultItem> pair in dictResultItems)
            {
                if (!changedResults.Contains(pair.Value) &&
                    pair.Value.linePath == resultItem.linePath && 
                    pair.Value.lineNumber == resultItem.lineNumber &&
                    pair.Value.resultOffset > resultItem.resultOffset)
                {
                    pair.Value.resultOffset = pair.Value.resultOffset + (strReplace.Length - resultItem.resultContent.Length);
                    changedResults.Add(pair.Value);
                }
            }
            resultItem.replaced = true;
        }

        public void ShowResultTreeContextMenu(object src, EventArgs args)
        {
            ContextMenu cm;
            MenuItem mi;            
            TreeViewItem item = (TreeViewItem)src;

            if (item.ContextMenu == null)
            {
                cm = new ContextMenu();

                mi = new MenuItem(){ Header = "Open in containing folder" };
                mi.Click += OpenInFolder;
                cm.Items.Add(mi);

                mi = new MenuItem() { Header = "Replace" };
                mi.Click += ReplaceSpecyfic;
                cm.Items.Add(mi);

                item.ContextMenu = cm;
                cm.PlacementTarget = item;

                dictContextMenu.Add(mi, item);
            }
            cm = item.ContextMenu;
            cm.IsOpen = true;
        }



        /*Fill settings and summary*/
        internal void FillResultSummary(Label lbl, ResultSummary resultSummary)
        {
            lbl.Content = "Searched files: " + resultSummary.searchedFiles.ToString() + "; Found results: " + resultSummary.foundResults.ToString();
        }

        private Label AddLabel(string text, WrapPanel infoWrapPanel)
        {
            Label lbl = new Label() { Content = text, Padding = new Thickness(2, 0, 1, 0), Margin = new Thickness(0, 2, 0, 0) };
            infoWrapPanel.Children.Add(lbl);
            return lbl;
        }

        private Label AddBold(Label lbl)
        {
            lbl.FontWeight = FontWeights.Bold;
            return lbl;
        }

        private Label AddExtraBold(Label lbl)
        {
            lbl.FontWeight = FontWeights.ExtraBold;
            return lbl;
        }

        internal void FillWraperPanel(FindSettings settings, WrapPanel infoWrapPanel)
        {
            infoWrapPanel.Children.Clear();

            AddLabel("`" + settings.tbPhrase + "`", infoWrapPanel);
            //separator
            AddExtraBold(AddLabel(" | ", infoWrapPanel));

            //WholeWord
            if (settings.chkWholeWord)
                AddExtraBold(AddLabel("W", infoWrapPanel));
            else
                AddLabel("w", infoWrapPanel);

            //Form
            if (settings.chkForm)
                AddExtraBold(AddLabel("F", infoWrapPanel));
            else
                AddLabel("f", infoWrapPanel);

            //CharCase
            if (settings.chkCase)
                AddExtraBold(AddLabel("C", infoWrapPanel));
            else
                AddLabel("c", infoWrapPanel);

            //RegExp
            if (settings.chkRegExp)
                AddExtraBold(AddLabel("R", infoWrapPanel));
            else
                AddLabel("r", infoWrapPanel);

            //separator
            AddExtraBold(AddLabel(" | ", infoWrapPanel));

            if (settings.rbCurrDoc)
                AddLabel("CurDocum", infoWrapPanel);
            else if (settings.rbOpenDocs)
                AddLabel("Opened", infoWrapPanel);
            else if (settings.rbProject)
                AddLabel("Project", infoWrapPanel);
            else if (settings.rbSolution)
                AddLabel("Solution", infoWrapPanel);
            else if (settings.rbLocation)
            {
                AddLabel(settings.tbLocation, infoWrapPanel);
                AddExtraBold(AddLabel(" | ", infoWrapPanel));
                AddLabel(settings.tbfileFilter, infoWrapPanel);
                AddExtraBold(AddLabel(" | ", infoWrapPanel));
                AddLabel(settings.chkSubDir.ToString(), infoWrapPanel);
            }
            else if (settings.rbLastResults)            
                AddLabel("LastRes", infoWrapPanel);            
        }



        /*Fill info panel*/
        public void ShowStatus(string info)
        {
            last_LabelInfo.Content = info;
        }



        /*Set settings*/
        public void SetTbFileFilter(string value)
        {
            cbFileMask.SelectedValue = 0;
            foreach (FileMaskItem item in cbFileMask.Items)
            {
                if (item.Value == value)
                {
                    cbFileMask.SelectedItem = item;
                    break;
                }
            }
        }



        /*(Un)Locking possibility of searching*/
        private void LockSearching()
        {
            btnFind.IsEnabled = false;
            btnAbort.IsEnabled = true;
            btnAbort2.IsEnabled = true;
        }

        private void UnlockSearching()
        {
            btnFind.IsEnabled = true;
            btnAbort.IsEnabled = false;
            btnAbort2.IsEnabled = false;
            if (cancellationToken.IsCancellationRequested)
                System.Windows.Forms.MessageBox.Show("Search aborted."); 
        }


        /*Events*/
        private void Tb_Checked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(1, GridUnitType.Star);
            last_rowFlat.Height = new GridLength(0);
            last_tbFlatTree.Foreground = Brushes.Red;
        }

        private void Tb_Unchecked(object sender, RoutedEventArgs e)
        {
            last_rowTree.Height = new GridLength(0);
            last_rowFlat.Height = new GridLength(1, GridUnitType.Star);
            last_tbFlatTree.ClearValue(ToggleButton.ForegroundProperty);
        }

        private void BtnAddSnapshot_Click(object sender, RoutedEventArgs e)
        {
            AddSmapshotTab(last_LabelInfo.Content == null ? "" : last_LabelInfo.Content.ToString());
            //TODO dodać na zakładkę nowe obiekty
            //todo dodać skrót wlaczający tool na pierwszą zakładkę
        }

        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            tokenSource = new CancellationTokenSource();
            cancellationToken = tokenSource.Token;
            StartSearch();
        }

        private void BtnUnExpAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpandAllInLvl(last_tvResultFlatTree.Items, false);
            SetExpandAllInLvl(last_tvResultTree.Items, false);
        }

        private void BtnExpAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpandAllInLvl(last_tvResultFlatTree.Items, true);
            SetExpandAllInLvl(last_tvResultTree.Items, true);
        }

        private void RbLocation_Click(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = rbLocation.IsChecked == true;
            btnGetLocation.IsEnabled = rbLocation.IsChecked == true;
        }

        private void BtnGetLocation_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (tbLocation.Text != "" && Directory.Exists(tbLocation.Text))
                dlg.SelectedPath = tbLocation.Text;
            if (System.Windows.Forms.DialogResult.OK == dlg.ShowDialog())
                tbLocation.Text = dlg.SelectedPath;
        }

        private void RbLocation_Checked(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = true;
            btnGetLocation.IsEnabled = true;
            cbFileMask.IsEnabled = true;
            btnAddFileMasks.IsEnabled = true;
            btnDelFileMasks.IsEnabled = true;
            chkSubDir.IsEnabled = true;
            chkForm.IsEnabled = false;
        }

        private void RbLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = false;
            btnGetLocation.IsEnabled = false;
            cbFileMask.IsEnabled = false;
            btnAddFileMasks.IsEnabled = false;
            btnDelFileMasks.IsEnabled = false;
            chkSubDir.IsEnabled = false;
            chkForm.IsEnabled = true;
        }

        private void Last_shortDir_Checked(object sender, RoutedEventArgs e)
        {
            SetHeaderShortLong(last_tvResultFlatTree, last_tvResultFlatTree.Items, true);
            SetHeaderShortLong(last_tvResultTree, last_tvResultTree.Items, true);
            last_shortDir.Foreground = Brushes.Red;
        }

        private void Last_shortDir_Unchecked(object sender, RoutedEventArgs e)
        {
            SetHeaderShortLong(last_tvResultFlatTree, last_tvResultFlatTree.Items, false);
            SetHeaderShortLong(last_tvResultTree, last_tvResultTree.Items, false);
            last_shortDir.ClearValue(ToggleButton.ForegroundProperty);
        }

        private void TbPhrase_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                StartSearch();
        }

        private void BtnAddFileMasks_Click(object sender, RoutedEventArgs e)
        {
            FileMask.AddToRegistry(cbFileMask.Text);
            FileMask.FillCB(cbFileMask);
            cbFileMask.SelectedIndex = cbFileMask.Items.Count;
        }

        private void BtnDelFileMasks_Click(object sender, RoutedEventArgs e)
        { 
            FileMask.DelFromRegistry( ((FileMaskItem)cbFileMask.SelectedItem).Key );
            FileMask.FillCB(cbFileMask);
        }

        private void Last_tvResultFlatTree_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        private void RbLastResults_Checked(object sender, RoutedEventArgs e)
        {
            chkForm.IsEnabled = false;
        }

        private void RbLastResults_Unchecked(object sender, RoutedEventArgs e)
        {
            chkForm.IsEnabled = true;
        }

        private void BtnAbort_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }

        private void BtnAbort2_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }


        /*Show debug info*/
        private void ShowCandidates(List<Candidate> candidates)
        {
            string text = "";
            foreach (Candidate candidate in candidates)
            {
                text = text + ShowCandidate(candidate, "");
                foreach (Candidate subCandidate in candidate.subItems)
                {
                    text = text + ShowCandidate(subCandidate, "->");
                }
            }
            System.Windows.Forms.MessageBox.Show(text);
        }

        private string ShowCandidate(Candidate candidate, string prefix)
        {
            string text = prefix;

            if (candidate.item != null)
                text = text + "item:TAK ";
            else
                text = text + "item:NIE ";

            if (candidate.document != null)
                text = text + "document:TAK ";
            else
                text = text + "document:NIE ";

            if (candidate.filePath != "")
                text = text + "filePath:TAK; ";
            else
                text = text + "filePath:NIE ";

            //if (candidate.document != null)
            text = text + "\n" + "docPath: " + candidate.DocumentPath;

            //if (candidate.filePath != "")
            text = text + "\n" + "filePath: " + candidate.filePath;
            return text + "\n\n";
        }
    }
}