using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations; //ITextSearchService, ITextStructureNavigatorSelectorService
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition; //[Import]
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VSHierarchyAddin;
using System.Threading;
using System.Threading.Tasks;

namespace VSFindTool
{
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for VSFindToolMainFormControl.
    /// </summary>
    public partial class VSFindToolMainFormControl : UserControl
    {
        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactory { get; set; }

        Connect c = new Connect();

        private object threadLock = new object();

        public delegate void FinishDelegate();

        List<ResultItem> lastResultList = new List<ResultItem>();
        List<Candidate> searchedCandidates = new List<Candidate>();
        List<ErrData> errList = new List<ErrData>();
        FindSettings lastSearchSettings = new FindSettings();

        // Result object for TVItem
        Dictionary<TreeViewItem, ResultItem> dictResultLines = new Dictionary<TreeViewItem, ResultItem>();
        
        // Settings for TVItem
        Dictionary<TreeViewItem, FindSettings> dictSearchSettings = new Dictionary<TreeViewItem, FindSettings>();
            
        // Preview TextBox for Settings
        Dictionary<FindSettings, TextBox> dictTBPreview = new Dictionary<FindSettings, TextBox>();
            
        // Result summary for Settings
        Dictionary<FindSettings, ResultSummary> dictResultSummary = new Dictionary<FindSettings, ResultSummary>();
       
        // Short / long path for TreeView
        Dictionary<TreeView, TVData> dictTVData = new Dictionary<TreeView, TVData>();
        
        // TVItem for Context menu
        Dictionary<MenuItem, TreeViewItem> dictContextMenu = new Dictionary<MenuItem, TreeViewItem>();



        CancellationTokenSource tokenSource;
        CancellationToken cancellationToken;



        /* public void HideFindResult2Window()
         {
             var findWindow = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
             findWindow.Visible = false;
         }*/



        private ResultSummary GetResultSummary(FindSettings settings)
        {
            if (!dictResultSummary.ContainsKey(settings))
                dictResultSummary.Add(settings, new ResultSummary());
            return dictResultSummary[settings];
        }


        private TreeViewItem GetTVItemByFilePath(ItemCollection colleation, string pathPart)
        {
            foreach (TreeViewItem item in colleation)
            {
                if (item.Header.ToString() == pathPart)
                    return item;
            }
            return null;
        }



        private bool ReplaceInDocument(Document document, FindSettings settings, ResultItem result)
        {
            TextSelection selection = GetSelection(document);
            selection.MoveToLineAndOffset(result.resultOffset, result.resultOffset);
            selection.SelectLine();
            if (selection.Text != result.lineContent)
            {
                Debug.Assert(false, "The line has changed: {1} in '{0}': '{2}'", result.linePath, result.resultOffset, result.lineContent);
            }

            return false;

        }

        public void Finish()
        {
            lock (threadLock)
            {                
                FillResultSummary(last_LabelInfo, GetResultSummary(lastSearchSettings));
                MoveResultToTreeList(last_tvResultTree, lastSearchSettings, last_TBPreview);
                MoveResultToFlatTreeList(last_tvResultFlatTree, lastSearchSettings, last_TBPreview);
                SetHeaderShortLong(last_tvResultFlatTree, last_tvResultFlatTree.Items, last_shortDir.IsChecked.Value);
                SetHeaderShortLong(last_tvResultTree, last_tvResultTree.Items, last_shortDir.IsChecked.Value);
                searchedCandidates.Clear();
                UnlockSearching();
            }
        }


        private void ExecSearch()
        {            
            var progress = new Progress<string>();
            progress.ProgressChanged += (o, info) =>
            {
                last_LabelInfo.Content = info;
            };

            dictResultSummary.Remove(lastSearchSettings);
            
            //Location
            if (lastSearchSettings.rbLocation)
            {
                if (!Directory.Exists(lastSearchSettings.tbLocation))
                {
                    System.Windows.Forms.MessageBox.Show(String.Format("Podania ścieżka '{0}' jest pusta lub nie wskazuje na istniejący katalog.", lastSearchSettings.tbLocation));
                    return;
                }
                FindInLocationAsync(progress, lastSearchSettings, lastResultList, "");
                tbiLastResult.Focus();
            }

            //Last results
            else if (lastSearchSettings.rbLastResults)
            {
                List<Candidate> lastResultCandidates = GetCandidatesByPath(lastResultList);
                lastResultList.Clear();
                //ShowCandidates(lastResultCandidates);
                FindInLastResultsAsync(progress, lastResultCandidates, lastSearchSettings);
                tbiLastResult.Focus();
            }

            else if (Dte.Solution.FullName != "")
            {                
                //Current doc
                if (lastSearchSettings.rbCurrDoc)
                {
                    List<Candidate> candidates = GetItemCandidates(GetActiveProject(), true);
                    //ShowCandidates(candidates);
                    FindInCurrentDocumentAsync(progress, candidates);
                    tbiLastResult.Focus();
                }
                //Open docs
                else if (lastSearchSettings.rbOpenDocs)
                {
                    List<Candidate> candidates = GetItemCandidates(GetActiveProject(), true);
                    FindInOpenedDocumentsAsync(progress, candidates, lastSearchSettings);
                    tbiLastResult.Focus();
                }

                //Project
                else if (lastSearchSettings.rbProject)
                {
                    FindInProjectAsync(progress, GetActiveProject(), lastSearchSettings, lastResultList, GetSolutionName());
                }

                //Solution
                else if (lastSearchSettings.rbSolution)
                {
                    FindInProjectsAsync(progress, lastSearchSettings, lastResultList, GetSolutionName());
                }
                tbiLastResult.Focus();                
            }

            else
            {
                System.Windows.Forms.MessageBox.Show("The solution is no opened.");
                return;
            }
        }

        private void StartSearch()
        {
            LockSearching();
            try
            {
                LastDocWindow = ((VSFindTool.VSFindToolPackage)(this.parentToolWindow.Package)).LastDocWindow;

                //Remember settings
                lastSearchSettings.Form2Settings(this);

                //Select last active document
                if (rbCurrDoc.IsChecked == true)
                {
                    if (Dte.ActiveDocument != null && Dte.ActiveDocument.ActiveWindow != null)
                    {
                        Dte.ActiveDocument.ActiveWindow.Activate();
                    }
                    else if (LastDocWindow != null)
                    {
                        LastDocWindow.Activate();
                    }
                }
                //for rbLastResults we need lastResultList to gather candidates, co it'll be cleared later
                if (!lastSearchSettings.rbLastResults)
                    lastResultList.Clear();
                FillWraperPanel(lastSearchSettings, last_infoWrapPanel);
                ExecSearch();
            }
            catch
            {
                UnlockSearching();
                throw;
            }
        }

        

        private async void FindInCurrentDocumentAsync(IProgress<string> progress, List<Candidate> candidates)
        {
            Candidate candidate = GetCandidate(candidates, LastDocWindow.ProjectItem);
            Debug.Assert(candidate != null, String.Format("There is no candiodate for LastDocWindow {0}: '{1}'", LastDocWindow.Caption, LastDocWindow.ProjectItem.Document.FullName));
            await Task.Factory.StartNew(() =>
            {
                FindInCandidate(candidate, GetSolutionName());

                //search in form/recource/oteher files
                if (lastSearchSettings.chkForm)
                    foreach (Candidate subCandidate in candidate.subItems)
                        FindInCandidate(subCandidate, GetSolutionName());
                //For test purpose only
                //Task.Delay(500).Wait();
                if (progress != null)
                    progress.Report(String.Format("Searching{0} item: {1}/{2}", "", 1, 1));
            }).ContinueWith(task => { GetResultSummary(lastSearchSettings).searchedFiles = 1; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void FindInProjectsAsync(IProgress<string> progress, FindSettings settings, List<ResultItem> resultList, string solutionDir)
        {
            string projectInfo;
            int loop = 0;
            int count = 0;
            GetResultSummary(settings).searchedFiles = 0;
            await Task.Factory.StartNew(() =>
            {
                foreach (Project project in Dte.Solution.Projects)
                {
                    loop++;
                    projectInfo = String.Format(" project: {0}/{1}", loop, Dte.Solution.Projects.Count);
                    count = FindInProject(progress, project, lastSearchSettings, resultList, solutionDir, projectInfo);
                }
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles += count; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void FindInLastResultsAsync(Progress<string> progress, List<Candidate> lastResultCandidates, FindSettings settings)
        {
            int loop = 0;
            await Task.Factory.StartNew(() =>
            {
                foreach (Candidate candidate in lastResultCandidates)
                {
                    FindInCandidate(candidate, GetSolutionName());
                    //For test purpose only
                    //Task.Delay(500).Wait();
                    loop++;
                    if (progress != null)
                        ((IProgress<string>)progress).Report(String.Format("Searching {0}/{1}", loop, lastResultCandidates.Count));
                }
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles = loop; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task FindInProjectAsync(IProgress<string> progress, Project project, FindSettings settings, List<ResultItem> resultList, string solutionDir, string projectInfo = "")
        {
            GetResultSummary(settings).searchedFiles = 0;
            int count = 0;
            await Task.Run(() =>
            {
                count = FindInProject(progress, project, settings, resultList, solutionDir, projectInfo);
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles += count; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
            return; //Task as reutl and return null is ok. If We 
        }

        private async void FindInLocationAsync(IProgress<string> progress, FindSettings settings, List<ResultItem> resultList, string solutionDir)
        {
            int loop = 0;
            await Task.Factory.StartNew(() =>
            {
                Debug.Assert(Directory.Exists(settings.tbLocation),
                             String.Format("Podania ścieżka '{0}' jest pusta lub nie wskazuje na istniejący katalog.",
                                            settings.tbLocation)
                             );

                List<Candidate> candidates = new List<Candidate>();

                List<string> filterList = new List<string>();
                foreach (string filter in settings.tbfileFilter.Split(';').ToList())
                {
                    filterList.Add(filter.Replace(".", "\\.").Replace("*", ".*") + "$");
                }

                GetCandidatesFromLocation(settings.tbLocation, candidates, filterList, settings.chkSubDir);
                foreach (Candidate candidate in candidates)
                {
                    loop++;
                    if (progress != null)
                        progress.Report(String.Format("Searching {0}/{1}", loop, candidates.Count));
                    //For test purpose only
                    //Task.Delay(500).Wait();
                    FindInFile(candidate.filePath, lastSearchSettings, resultList, solutionDir);
                }
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles = loop; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void FindInOpenedDocumentsAsync(Progress<string> progress, List<Candidate> candidates, FindSettings settings)
        {
            int loop = 0;
            await Task.Factory.StartNew(() =>
            {
                List<Window> windowsWithItems = GetWindowsWithItems();
                foreach (EnvDTE.Window window in windowsWithItems)
                {
                    if (window != null)
                    {
                        Candidate candidate = GetCandidate(candidates, window.ProjectItem);
                        Debug.Assert(candidate != null, String.Format("There is no candiodate for window {0}: '{1}'", window.Caption, window.ProjectItem.Document.FullName));

                        FindInCandidate(candidate, GetSolutionName());

                        FindInProjectItem(window.ProjectItem, lastSearchSettings, lastResultList, errList, GetSolutionName(), candidate.filePath);
                        //search in form/recource/oteher files
                        if (lastSearchSettings.chkForm)
                            foreach (Candidate subCandidate in candidate.subItems)
                                FindInCandidate(subCandidate, GetSolutionName());
                    }
                    else
                    {
                        Debug.Assert(false, "You shouldnt be here.");
                    }
                    //For test purpose only
                    //Task.Delay(500).Wait();
                    loop++;
                    if (progress != null)
                        ((IProgress<string>)progress).Report(String.Format("Searching {0}/{1}", loop, windowsWithItems.Count));
                }
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles = loop; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private int FindInProject(IProgress<string> progress, Project project, FindSettings settings, List<ResultItem> resultList, string solutionDir, string projectInfo = "")
        {            
            int loop = 0;
            List<Candidate> candidates = GetItemCandidates(project, true);
            //ShowCandidates(candidates);
            foreach (Candidate candidate in candidates)
            {
                FindInCandidate(candidate, solutionDir);

                Debug.Assert(candidate.item != null || candidate.filePath != "", "Wrong candidate 'candidate.item != null || candidate.filePath != '''");
                //search in form/recource/oteher files
                if (lastSearchSettings.chkForm)
                    foreach (Candidate subCandidate in candidate.subItems)
                        FindInCandidate(subCandidate, solutionDir);
                //For test purpose only
                //Task.Delay(500).Wait();
                loop++;
                if (progress != null)
                    ((IProgress<string>)progress).Report(String.Format("Searching{0} item: {1}/{2}", projectInfo, loop, candidates.Count));
            }
            return loop; //Task as reutl and return null is ok. If We 
        }


        private void FindInCandidate(Candidate candidate, string solutionDir)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (!searchedCandidates.Exists(e => e.filePath == candidate.filePath))
            {
                if (candidate.item != null)
                    FindInProjectItem(candidate.item, lastSearchSettings, lastResultList, errList, solutionDir, candidate.filePath);
                else if (candidate.filePath != "")
                    FindInFile(candidate.filePath, lastSearchSettings, lastResultList, solutionDir);
                searchedCandidates.Add(candidate);
            }
        }

        private void FindInDocument(Document document, FindSettings settings, List<ResultItem> resultList, List<ErrData> errList, string solutionDir, string bulkPath)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == bulkPath.ToLower()))
            {
                int lineIndex = 0;
                //GetResultSummary(settings).searchedFiles++;
                List<string> resList = GetDocumentContent(document, errList).Replace("\n\r", "\n").Split('\n').ToList<string>();
                foreach (string line in resList)
                {
                    LineToResultList(line, settings, resultList, solutionDir, bulkPath, lineIndex);
                    lineIndex++;
                }
            }
        }

        private void FindInProjectItem(ProjectItem item, FindSettings settings, List<ResultItem> resultList, List<ErrData> errList, string solutionDir, string bulkPath)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == bulkPath.ToLower()))
            {
                int lineIndex = 0;
                //GetResultSummary(settings).searchedFiles++;
                List<string> resList = GetItemContent(item, errList).Replace("\n\r", "\n").Split('\n').ToList<string>();
                foreach (string line in resList)
                {
                    LineToResultList(line, settings, resultList, solutionDir, bulkPath, lineIndex);
                    lineIndex++;
                }
            }
        }

        private void FindInSelection(string selection, FindSettings settings, List<ResultItem> resultList, List<ErrData> errList, string solutionDir, string bulkPath)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == bulkPath.ToLower()))
            {
                int lineIndex = 0;
                //GetResultSummary(settings).searchedFiles++;

                List<string> resList = selection.Replace("\r\n", "\n").Split('\n').ToList();

                foreach (string line in resList)
                {
                    LineToResultList(line, settings, resultList, solutionDir, bulkPath, lineIndex);
                    lineIndex++;
                }
            }
        }

        private void FindInFile(string path, FindSettings settings, List<ResultItem> resultList, string solutionDir)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == path.ToLower()))
            {
                StreamReader stream = new StreamReader(path, Encoding.Default);
                string line = stream.ReadLine();
                int lineIndex = 0;
                //GetResultSummary(settings).searchedFiles++;
                while (!stream.EndOfStream)
                {
                    LineToResultList(line, settings, resultList, solutionDir, path, lineIndex);
                    line = stream.ReadLine();
                    lineIndex++;
                }
            }
        }

        private MatchCollection GetMatchesInline(string line, FindSettings settings)
        {
            string phrase = settings.tbPhrase;
            string prefix = settings.GetPrefix();
            string sufix = settings.GetSufix();
            if (!settings.chkRegExp)
            {
                phrase = Regex.Escape(phrase);
            }

            if (settings.chkCase)
                return Regex.Matches(line, prefix + phrase + sufix);
            else
                return Regex.Matches(line, prefix + phrase + sufix, RegexOptions.IgnoreCase);
        }

        private void LineToResultList(string line, FindSettings settings, List<ResultItem> resultList, string solutionDir, string path, int lineIndex)
        {
            ResultItem resultItem;
            int indexInLine = 0;

            foreach (Match match in GetMatchesInline(line, settings))
            {
                resultItem = new ResultItem()
                {
                    linePath = path,
                    lineContent = line,
                    lineNumber = lineIndex,
                    resultIndex = indexInLine,
                    resultOffset = match.Index,
                    resultLength = match.Length,
                    resultContent = match.Value
                };
                lock (threadLock)
                {
                    resultList.Add(resultItem);
                    GetResultSummary(settings).foundResults++;
                }
                indexInLine++;
            }
        }

 


        private List<EnvDTE.Window> GetWindowsWithItems()
        {
            List<EnvDTE.Window> result = new List<EnvDTE.Window>();
            foreach (EnvDTE.Window window in Dte.Windows)
            {
                if (window.ProjectItem != null)
                {

                    if (!result.Exists(e => e.ProjectItem.Name == window.ProjectItem.Name))
                        result.Add(window);
                }
            }
            return result;
        }

        private Project GetActiveProject()
        {
            if (Dte.Solution.Projects.Count == 1)
            {
                foreach (Project project in Dte.Solution.Projects)
                    return project;
            }
            else
            {
                Array activeSolutionProjects = Dte.ActiveSolutionProjects as Array;
                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                    return activeSolutionProjects.GetValue(0) as Project;
                else
                    Debug.Assert(false, "Brak aktywnego projektu.");
            }
            return null;
        }

        private string GetSolutionName()
        {
            return Dte.Solution != null ? System.IO.Path.GetDirectoryName(Dte.Solution.FullName) : "";
        }



        //Get Candidates from project
        private List<Candidate> GetItemCandidates(Project project, bool allowNoDoc)
        {
            List<Candidate> result = new List<Candidate>();
            foreach (ProjectItem item in project.ProjectItems)
            {
                GetItemCandidates(item, result, Path.GetDirectoryName(project.FullName), allowNoDoc);
            }

            //Checking if none of candidates have subitems
            foreach(Candidate candidate in result)
            {
                if (candidate.subItems.Count > 0)
                {
                    foreach(Candidate subCandidate in candidate.subItems)
                    {
                        Debug.Assert(subCandidate.subItems.Count == 0, String.Format("There are subItems for candidate '{0}' .", subCandidate.filePath));
                    }
                }
            }

            //Checking if none candidate have different paths for file and document
            foreach (Candidate candidate in result)
            {
                if (candidate.filePath != "" && 
                    candidate.DocumentPath != "")
                {
                    Debug.Assert(candidate.filePath.ToLower() == candidate.DocumentPath.ToLower(), String.Format("Candidate has different paths '{0}' ; '{1}' .", candidate.filePath, candidate.DocumentPath));
                }
            }

            return result;
        }

        //Fill result with Candidates from item (may have subitems)
        //Candidate will have filled document if it was opened (sometimes there may NOT be document)
        //If there is no document we'll search the file
        private void GetItemCandidates(ProjectItem item, List<Candidate> result, string parentPath, bool allowNoDoc)
        {
            string itemPath = Path.Combine(parentPath, item.Name);
            Candidate candidate = null;
            Candidate existingCandidate = null;
            Candidate candidateAsParent = null;

            Document doc;
            string documentPath;

            //Some items doesn't have Documents (ie Properties) and throw exceptions even when we try to chech if they are equal to null
            try
            {
                doc = item.Document;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                doc = null;
            }


            //Trying to find existing candidate with the same filePath or documentPath : null
            documentPath = doc != null ? doc.FullName : "";
            existingCandidate = result.FirstOrDefault(e => (
                                                            (e.filePath == itemPath && itemPath != "") ||
                                                            (e.DocumentPath == documentPath && documentPath != "")
                                                            )
                                                        );

            //If the ProjectItem has docyment or we allow candidates w/o documents
            if (allowNoDoc || (doc != null))
            {
                candidate = new Candidate();
                candidate.filePath = File.Exists(itemPath) ? itemPath : "";
                if (doc != null)
                {
                    candidate.item = item;
                    candidate.document = doc;
                }

                //if candidate have docyment or filePath
                if (candidate.filePath != "" || candidate.item != null)
                {

                    /*existingCandidate = result.FirstOrDefault(e => (
                                                                        (e.filePath == candidate.filePath && candidate.filePath != "") ||
                                                                        (e.DocumentPath == candidate.DocumentPath && candidate.DocumentPath != "")
                                                                    )
                                                              );*/
                    if (existingCandidate == null)
                        result.Add(candidate);
                    else
                        Console.WriteLine("Ommited 'GetItemCandidates / (File.Exists(itemPath))': '" + itemPath + "'");
                }
                else
                {
                    Console.WriteLine("Ommited 'GetItemCandidates / (candidate.filePath != '' || candidate.item != null)': '" + itemPath + "'");
                }
            }
            else
            {
                Console.WriteLine("Ommited 'GetItemCandidates / (doc != null)': '" + itemPath + "'");
            }

            if (existingCandidate != null)
                candidateAsParent = existingCandidate;
            else
                candidateAsParent = candidate;

            Debug.Assert(Directory.Exists(itemPath) || File.Exists(itemPath), String.Format("Item '{0}' is nither dir nor file.", item.Name));

            foreach (ProjectItem subItem in item.ProjectItems)
            {
                //If item is directory, subitems will be independent
                if (Directory.Exists(itemPath))
                    GetItemCandidates(subItem, result, itemPath, allowNoDoc);
                //If item is file, subitems wolud be form, rosurrce and oteher non-code types
                else if (File.Exists(itemPath))
                {
                    Debug.Assert(candidateAsParent != null, String.Format("There is no candidateAsParent for item '{0}' .", item.Name));
                    GetItemCandidates(subItem, candidateAsParent.subItems, parentPath, allowNoDoc);
                }
            }
        }

        //Fill result with Candidates from Directory and subdirectories if they match fileFilters
        private void GetCandidatesFromLocation(string parentDirPath, List<Candidate> result, List<string> fileFilters, bool includeSubDirs)
        {
            foreach(string filePath in Directory.GetFiles(parentDirPath))
            {
                bool blAdd = false;
                foreach(string filter in fileFilters)
                {
                    if (Regex.IsMatch(Path.GetFileName(filePath), filter, RegexOptions.IgnoreCase))
                    {
                        blAdd = true;
                        break;
                    }
                }
               
                if (blAdd)
                    result.Add(new Candidate() { filePath = filePath });
            }
            if (includeSubDirs)
                foreach (string dirPath in Directory.GetDirectories(parentDirPath))
                {
                    GetCandidatesFromLocation(dirPath, result, fileFilters, includeSubDirs);
                }
        }

        //Get candidate from list for selected project item
        private Candidate GetCandidate(List<Candidate> candidates, ProjectItem item)
        {
            Candidate candidate = candidates.FirstOrDefault<Candidate>( (e => e.item == item) );
            if (candidate == null)
            {
                foreach (Candidate tmpCandidate in candidates)
                {
                    foreach (Candidate subCandidate in tmpCandidate.subItems)
                    {
                        if (subCandidate.item == item /*subCandidate.filePath.ToLower() == window.ProjectItem.Document.FullName.ToLower()*/)
                        {
                            candidate = subCandidate;
                            break;
                        }
                    }
                    if (candidate != null)
                    {
                        break;
                    }
                }
            }
            else
            {
                //int i = 0;
            }
            return candidate;
        }

        //Get full content of document of project item
        internal string GetItemContent(ProjectItem item, List<ErrData> errList)
        {
            return GetDocumentContent(item.Document, errList, item.Name);
        }

        //Get full content of document
        internal string GetDocumentContent(Document document, List<ErrData> errList, string errInfo = "")
        {
            if (document.Selection == null)
            {
                errList.Add(new ErrData()
                {
                    path = document.FullName,
                    caption = errInfo != "" ? errInfo : (document.ActiveWindow != null ? document.ActiveWindow.Caption : ""),
                    info = "No Selection object found."
                });
                return "";
            }
            EnvDTE.TextSelection selection = GetSelection(document);
            int line = selection.ActivePoint.Line;
            int lineCharOffset = selection.ActivePoint.LineCharOffset;
            selection.SelectAll();
            string result = selection.Text;
            selection.MoveToLineAndOffset(line, lineCharOffset);
            return result;
        }



        //Get selection of document of project item
        private EnvDTE.TextSelection GetSelection(ProjectItem item)
        {
            if (item.Document != null)
            {
                return GetSelection(item.Document);
            }
            else
            {
                return null;
            }
        }

        //Get selection of documen
        private EnvDTE.TextSelection GetSelection(Document document)
        {
            if (document.Selection != null)
            {
                return document.Selection as EnvDTE.TextSelection;
            }
            else
            {
                return null;
            }
        }

        //Get selection of document of window
        private EnvDTE.TextSelection GetSelection(EnvDTE.Window window)
        {
            if (window.Selection != null)
            {
                return window.Selection as EnvDTE.TextSelection;
            }
            else
            {
                return null;
            }
        }



        /*private Document GetDocumentByPath(string path)
        {
            Document result = null;
            foreach (EnvDTE.Window window in Dte.Windows)
            {
                if (window.ProjectItem != null)
                {
                    if (window.ProjectItem.Document != null)
                    {
                        if (window.ProjectItem.Document.FullName.ToLower() == path.ToLower())
                        {
                            result = window.ProjectItem.Document;
                        }
                    }
                }
            }
            return result;
        }*/

        //Get candidates from result list
        private List<Candidate> GetCandidatesByPath(List<ResultItem> resList)
        {
            List<Candidate> result = new List<Candidate>();
            List<Candidate> candidatesWithDocuments = new List<Candidate>();
            List<Candidate> candidatesInProject = new List<Candidate>();
            foreach (Project project in Dte.Solution.Projects)
            {
                candidatesInProject = GetItemCandidates(project, true);
                candidatesWithDocuments.AddRange(candidatesInProject);
            }

            Candidate candidate;
            foreach (ResultItem resultItem in resList)
            {
                candidate = GetCandidateByPath(candidatesWithDocuments, resultItem.linePath);
                if (candidate == null)
                    candidate = new Candidate() { filePath = resultItem.linePath };
                result.Add(candidate);
            }
            return result;
        }

        private Candidate GetCandidateByPath(List<Candidate> candidates, string path)
        {
            Candidate result = candidates.FirstOrDefault(e => e.filePath == path);
            if (result == null)
                foreach (Candidate candidate in candidates)
                {
                    result = GetCandidateByPath(candidate.subItems , path);
                    if (result != null)
                        break;
                }
            return result;
        }

        //
        /*private string GetProjectItemContent(ProjectItem item)
        {
            string result = "";
            EnvDTE.TextSelection selection = null;
            try
            {
                selection = GetSelection(item);
            }
            catch
            {
                selection = null;
            }
            if (selection != null)
            {
                selection.SelectAll();
                return result;
            }
            return "";
        }*/
    }
}