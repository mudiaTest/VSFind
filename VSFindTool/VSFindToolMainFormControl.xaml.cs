using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
//using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;

namespace VSFindTool
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    /// <summary>
    /// Interaction logic for VSFindToolMainFormControl.xaml
    /// </summary>
    public partial class VSFindToolMainFormControl : System.Windows.Controls.UserControl
    {
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
            dictTBPreview.Add(last_searchSettings, last_TBPreview);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        //[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]



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
        internal void MoveResultToTreeList(TreeView tvResultTree, FindSettings last_searchSettings, TextBox tbPreview)
        {
            ItemCollection treeItemColleaction;
            string pathAgg;
            TreeViewItem treeItem;
            TreeViewItem leafItem;

            dictTVData[tvResultTree] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName)
            };

            tvResultTree.Items.Clear();

            foreach (ResultLineData resultLineData in resultList)
            {
                treeItemColleaction = tvResultTree.Items;
                treeItem = null;
                pathAgg = "";
                for (int i = 0; i < resultLineData.PathPartsList.Count; i++)
                {
                    if (pathAgg == "")
                        pathAgg = resultLineData.PathPartsList[i];
                    else
                        pathAgg = pathAgg + @"\" + resultLineData.PathPartsList[i];
                    if (Directory.Exists(pathAgg) || File.Exists(pathAgg))
                    {
                        treeItem = GetItem(treeItemColleaction, resultLineData.PathPartsList[i]);
                        if (treeItem == null)
                        {
                            treeItem = new TreeViewItem() {
                                Header = resultLineData.PathPartsList[i],
                                FontWeight = FontWeights.Bold
                            };
                            treeItemColleaction.Add(treeItem);
                        }
                        treeItemColleaction = treeItem.Items;
                    }
                    if (i == resultLineData.PathPartsList.Count - 1)
                    {
                        leafItem = new TreeViewItem()
                        {
                            Header = "(" + resultLineData.lineNumber.ToString() + @"/" + resultLineData.resultOffset.ToString() + ") : " + resultLineData.lineContent.Trim(),
                            FontWeight = FontWeights.Normal
                        };
                        leafItem.MouseDoubleClick += OpenResultDocLine;
                        leafItem.MouseUp += PreviewResultDoc;
                        leafItem.MouseRightButtonUp += ShowResultTreeContextMenu;
                        //leafItem.ContextMenu = (ContextMenu)this.Resources["TVContextMenu"];
                       // leafItem.ContextMenu = new ContextMenu();
                        dictResultLines.Add(leafItem, resultLineData);
                        dictSearchSettings.Add(leafItem, last_searchSettings);                        
                        treeItemColleaction.Add(leafItem);
                    }
                }
            }

            foreach (TreeViewItem tmpItem in tvResultTree.Items)
                JoinNodesWOLeafs(tmpItem);
            SetExpandAllInLvl(tvResultTree.Items, true);
        }

        internal void MoveResultToFlatTreeList(TreeView tvResultFlatTree, FindSettings last_searchSetting, TextBox tbPreview)
        {
            TreeViewItem treeItem;
            TreeViewItem leafItem;
            
            dictTVData[tvResultFlatTree] = new TVData()
            {
                longDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName)
            };

            tvResultFlatTree.Items.Clear();        

            foreach (ResultLineData resultLineData in resultList)
            {
                treeItem = GetItem(tvResultFlatTree.Items, resultLineData.linePath);
                if (treeItem == null)
                {
                    treeItem = new TreeViewItem() { Header = resultLineData.linePath, FontWeight = FontWeights.Bold };
                    tvResultFlatTree.Items.Add(treeItem);
                }
                leafItem = new TreeViewItem()
                {
                    Header = "(" + resultLineData.lineNumber.ToString() + @"/" + resultLineData.resultOffset.ToString() + ") : " + resultLineData.lineContent.Trim(),
                    FontWeight = FontWeights.Normal
                };
                leafItem.MouseDoubleClick += OpenResultDocLine;
                leafItem.MouseUp += PreviewResultDoc;
                leafItem.MouseRightButtonUp += ShowResultTreeContextMenu;
                this.Focusable = false;
                dictResultLines.Add(leafItem, resultLineData);
                dictSearchSettings.Add(leafItem, last_searchSettings);
                treeItem.Items.Add(leafItem);
            }
            SetExpandAllInLvl(tvResultFlatTree.Items, true);
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


        public void OpenResultDocLine(object src, EventArgs args)
        {
            ResultLineData resultLine = dictResultLines[(TreeViewItem)src];
            FindSettings settings = dictSearchSettings[(TreeViewItem)src];
            if (Dte != null)
            {
                EnvDTE.Window docWindow = Dte.ItemOperations.OpenFile(resultLine.linePath, Constants.vsViewKindTextView);
                TextSelection selection = GetSelection(Dte.ActiveDocument);
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
                        selection.MoveToLineAndOffset(resultLine.lineNumber.Value + 1, resultLine.resultOffset + settings.tbPhrase.Length + 1, true);
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
            TextBox tbPreview = dictTBPreview[settings];
            tbPreview.Text = "";
            int lineNumber = 0;
            using (var reader = new StreamReader(resultLine.linePath))
            {
                string line;
                while (lineNumber <= Math.Max(0, resultLine.lineNumber.Value + 2))
                {
                    lineNumber++;
                    line = reader.ReadLine();
                    if (lineNumber >= Math.Max(0, resultLine.lineNumber.Value - 2) && lineNumber <= Math.Max(0, resultLine.lineNumber.Value + 2))
                        tbPreview.AppendText((tbPreview.Text != "" ? Environment.NewLine : "") + line);
                }
            }
        }

        public void OpenInFolder(object src, EventArgs args)
        {
            TreeViewItem item = (TreeViewItem)src;
            item.Header = "ooo";
        }

        public void ShowResultTreeContextMenu(object src, EventArgs args)
        {
            ContextMenu cm;
            MenuItem mi;            
            TreeViewItem item = (TreeViewItem)src;

            if (item.ContextMenu == null)
            {
                cm = new ContextMenu();

                mi = new MenuItem();
                mi.Header = "pos1";
                mi.Click += OpenInFolder;
                cm.Items.Add(mi);

                mi = new MenuItem();
                mi.Header = "pos2";
                cm.Items.Add(mi);

                item.ContextMenu = cm;
                cm.PlacementTarget = item;
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
            }
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
            chkForm.IsEnabled = false;
        }

        private void RbLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            tbLocation.IsEnabled = false;
            btnGetLocation.IsEnabled = false;
            cbFileMask.IsEnabled = false;
            btnAddFileMasks.IsEnabled = false;
            btnDelFileMasks.IsEnabled = false;
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

        private void last_tvResultFlatTree_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }
    }
}