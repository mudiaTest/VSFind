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
    using System.Runtime.CompilerServices;

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
        Dictionary<TreeViewItem, ResultItem> dictResultItems = new Dictionary<TreeViewItem, ResultItem>();
        
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



        internal void ClearLastResults()
        {
            List<TreeViewItem> keyTVList = new List<TreeViewItem>();

            //dictResultItems
            //dictSearchSettings
            foreach (KeyValuePair<TreeViewItem, ResultItem> pair in dictResultItems)
            {
                if (pair.Value.belongsToLastResults)
                    keyTVList.Add(pair.Key);
            }
            foreach (TreeViewItem key in keyTVList)
            {
                dictResultItems.Remove(key);
                dictSearchSettings.Remove(key);
            }

            List<MenuItem> keyMIList = new List<MenuItem>();

            //dictContextMenu
            foreach (KeyValuePair<MenuItem, TreeViewItem> pair in dictContextMenu)
            {
                if (keyTVList.IndexOf(pair.Value) >= 0)
                    keyMIList.Add(pair.Key);
            }
            foreach (MenuItem key in keyMIList)
            {
                dictContextMenu.Remove(key);
            }

            //dictTVData
            dictTVData.Remove(last_tvResultFlatTree);
            dictTVData.Remove(last_tvResultTree);
        }



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



        public void Finish(bool resultToTV = false)
        {
            lock (threadLock)
            {                
                FillResultSummary(last_LabelInfo, GetResultSummary(lastSearchSettings));
                if (resultToTV)
                {
                    MoveResultToTreeList(last_tvResultTree, lastSearchSettings, last_TBPreview, lastResultList, false);
                    MoveResultToFlatTreeList(last_tvResultFlatTree, lastSearchSettings, last_TBPreview, lastResultList, false);
                    //RebuildTVTreeList(last_tvResultTree);
                }
                JoinNodesWOLeafs(last_tvResultTree);
                //SetHeaderShortLong(last_tvResultFlatTree, last_tvResultFlatTree.Items, last_shortDir.IsChecked.Value);
                //SetHeaderShortLong(last_tvResultTree, last_tvResultTree.Items, last_shortDir.IsChecked.Value);
                searchedCandidates.Clear();
                UnlockSearching();
            }
        }


        private void ExecSearch()
        {            
            var progressInfo = new Progress<string>();
            progressInfo.ProgressChanged += (o, info) =>
            {
                last_LabelInfo.Content = info;
            };

            var progressResult = new Progress<List<ResultItem>>();
            progressResult.ProgressChanged += (o, list) =>
            {
                MoveResultToTreeList(last_tvResultTree, lastSearchSettings, last_TBPreview, list);
                MoveResultToFlatTreeList(last_tvResultFlatTree, lastSearchSettings, last_TBPreview, list);
                //RebuildTVTreeList(last_tvResultTree);
                foreach (ResultItem item in list)
                    lastResultList.Add(item);
                list.Clear();
            };

            dictResultSummary.Remove(lastSearchSettings);
            
            //Location
            if (lastSearchSettings.rbLocation)
            {
                if (!Directory.Exists(lastSearchSettings.tbLocation))
                {
                    System.Windows.Forms.MessageBox.Show(String.Format("Podania ścieżka '{0}' jest pusta lub nie wskazuje na istniejący katalog.", lastSearchSettings.tbLocation));
                    UnlockSearching();
                    return;
                }
                FindInLocationAsync(progressInfo, progressResult, lastSearchSettings, "");
                tbiLastResult.Focus();
            }

            //Last results
            else if (lastSearchSettings.rbLastResults)
            {
                List<Candidate> lastResultCandidates = GetCandidatesByPath(lastResultList);
                lastResultList.Clear();
                //ShowCandidates(lastResultCandidates);
                FindInLastResultsAsync(progressInfo, progressResult, lastResultCandidates, lastSearchSettings);
                tbiLastResult.Focus();
            }

            else if (Dte.Solution.FullName != "")
            {                
                //Current doc
                if (lastSearchSettings.rbCurrDoc)
                {
                    List<Candidate> candidates = GetItemCandidates(GetActiveProject(), true);
                    //ShowCandidates(candidates);
                    FindInCurrentDocumentAsync(progressInfo, progressResult, candidates);
                    tbiLastResult.Focus();
                }
                //Open docs
                else if (lastSearchSettings.rbOpenDocs)
                {
                    List<Candidate> candidates = GetItemCandidates(GetActiveProject(), true);
                    FindInOpenedDocumentsAsync(progressInfo, progressResult, candidates, lastSearchSettings);
                    tbiLastResult.Focus();
                }

                //Project
                else if (lastSearchSettings.rbProject)
                {
                    FindInProjectAsync(progressInfo, progressResult, GetActiveProject(), lastSearchSettings, GetSolutionName());
                }

                //Solution
                else if (lastSearchSettings.rbSolution)
                {
                    FindInProjectsAsync(progressInfo, progressResult, lastSearchSettings, GetSolutionName());
                }
                tbiLastResult.Focus();                
            }

            else
            {
                System.Windows.Forms.MessageBox.Show("The solution is no opened.");
                UnlockSearching();
                return;
            }
        }

        private void StartSearch()
        {
            LockSearching();
            try
            {
                LastDocWindow = ((VSFindTool.VSFindToolPackage)(this.parentToolWindow.Package)).LastDocWindow;

                ClearTV(last_tvResultFlatTree, last_tvResultTree);
                ClearLastResults();

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

        

        //Find in group of sources
        private async void FindInCurrentDocumentAsync(IProgress<string> progressInfo, IProgress<List<ResultItem>> progressResult, List<Candidate> candidates)
        {
            List<ResultItem> tmpResultList;
            Candidate candidate = GetCandidate(candidates, LastDocWindow.ProjectItem);
            Debug.Assert(candidate != null, String.Format("There is no candiodate for LastDocWindow {0}: '{1}'", LastDocWindow.Caption, LastDocWindow.ProjectItem.Document.FullName));
            await Task.Factory.StartNew(() =>
            {
                tmpResultList = new List<ResultItem>();
                FindInCandidate(candidate, GetSolutionName(), tmpResultList);


                //search in form/recource/oteher files
                if (lastSearchSettings.chkForm)
                    foreach (Candidate subCandidate in candidate.subItems)
                        FindInCandidate(subCandidate, GetSolutionName(), tmpResultList);
                //For test purpose only
                //Task.Delay(500).Wait();
                if (progressInfo != null)
                    progressInfo.Report(String.Format("Searching{0} item: {1}/{2}", "", 1, 1));
                if (progressResult != null)
                    ((IProgress<List<ResultItem>>)progressResult).Report(tmpResultList);
            }).ContinueWith(task => { GetResultSummary(lastSearchSettings).searchedFiles = 1; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void FindInProjectsAsync(IProgress<string> progressInfo, IProgress<List<ResultItem>> progressResult, FindSettings settings, string solutionDir)
        {
            string projectInfo;
            int loop = 0;
            int count = 0;
            GetResultSummary(settings).searchedFiles = 0;
            await Task.Factory.StartNew(() =>
            {
                foreach (Project project in Dte.Solution.Projects)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    loop++;
                    projectInfo = String.Format(" project: {0}/{1}", loop, Dte.Solution.Projects.Count);
                    count = FindInProject(progressInfo, progressResult,  project, lastSearchSettings, solutionDir, projectInfo);
                }
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles += count; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void FindInLastResultsAsync(Progress<string> progressInfo, IProgress<List<ResultItem>> progressResult, List<Candidate> lastResultCandidates, FindSettings settings)
        {
            int loop = 0;
            List<ResultItem> tmpResultList;
            await Task.Factory.StartNew(() =>
            {
                foreach (Candidate candidate in lastResultCandidates)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    tmpResultList = new List<ResultItem>();
                    FindInCandidate(candidate, GetSolutionName(),  tmpResultList);
                    //For test purpose only
                    //Task.Delay(500).Wait();
                    loop++;
                    if (progressInfo != null)
                        ((IProgress<string>)progressInfo).Report(String.Format("Searching {0}/{1}", loop, lastResultCandidates.Count));
                    if (progressResult != null)
                        ((IProgress<List<ResultItem>>)progressResult).Report(tmpResultList);
                }
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles = loop; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void FindInProjectAsync(IProgress<string> progressInfo, IProgress<List<ResultItem>> progressResult, Project project, FindSettings settings, string solutionDir, string projectInfo = "")
        {
            GetResultSummary(settings).searchedFiles = 0;
            int count = 0;
            await Task.Run(() =>
            {
                count = FindInProject(progressInfo, progressResult, project, settings,  solutionDir, projectInfo);
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles += count; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
            return; //Task as resutl and return null is ok. If We 
        }

        private async void FindInLocationAsync(IProgress<string> progressInfo, IProgress<List<ResultItem>> progressResult, FindSettings settings, string solutionDir)
        {
            int loop = 0;
            List<ResultItem> tmpResultList;
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
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    tmpResultList = new List<ResultItem>();
                    FindInFile(candidate.filePath, lastSearchSettings, tmpResultList, solutionDir);
                    //For test purpose only
                    //Task.Delay(500).Wait();
                    loop++;
                    if (progressInfo != null)
                        progressInfo.Report(String.Format("Searching {0}/{1}", loop, candidates.Count));
                    if (progressResult != null)
                        ((IProgress<List<ResultItem>>)progressResult).Report(tmpResultList);
                }
            }, TaskCreationOptions.LongRunning).ContinueWith(task => { GetResultSummary(settings).searchedFiles = loop; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void FindInOpenedDocumentsAsync(Progress<string> progressInfo, IProgress<List<ResultItem>> progressResult, List<Candidate> candidates, FindSettings settings)
        {
            int loop = 0;
            List<ResultItem> tmpResultList;
            await Task.Factory.StartNew(() =>
            {
                List<Window> windowsWithItems = GetWindowsWithItems();
                foreach (EnvDTE.Window window in windowsWithItems)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    tmpResultList = new List<ResultItem>();
                    if (window != null)
                    {
                        Candidate candidate = GetCandidate(candidates, window.ProjectItem);
                        Debug.Assert(candidate != null, String.Format("There is no candiodate for window {0}: '{1}'", window.Caption, window.ProjectItem.Document.FullName));

                        FindInCandidate(candidate, GetSolutionName(), tmpResultList);

                        FindInProjectItem(window.ProjectItem, lastSearchSettings, tmpResultList, errList, GetSolutionName(), candidate.filePath);
                        //search in form/recource/oteher files
                        if (lastSearchSettings.chkForm)
                            foreach (Candidate subCandidate in candidate.subItems)
                                FindInCandidate(subCandidate, GetSolutionName(), tmpResultList);
                    }
                    else
                    {
                        Debug.Assert(false, "You shouldnt be here.");
                    }
                    //For test purpose only
                    //Task.Delay(500).Wait();
                    loop++;
                    if (progressInfo != null)
                        ((IProgress<string>)progressInfo).Report(String.Format("Searching {0}/{1}", loop, windowsWithItems.Count));
                    if (progressResult != null)
                        ((IProgress<List<ResultItem>>)progressResult).Report(tmpResultList);
                }
            }).ContinueWith(task => { GetResultSummary(settings).searchedFiles = loop; Finish(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private int FindInProject(IProgress<string> progressInfo, IProgress<List<ResultItem>> progressResult, Project project, FindSettings settings, string solutionDir, string projectInfo = "")
        {            
            int loop = 0;
            List<ResultItem> tmpResultList;
            List<Candidate> candidates = GetItemCandidates(project, true);
            //ShowCandidates(candidates);
            foreach (Candidate candidate in candidates)
            {
                tmpResultList = new List<ResultItem>();
                FindInCandidate(candidate, solutionDir, tmpResultList);
                Debug.Assert(candidate.item != null || candidate.filePath != "", "Wrong candidate 'candidate.item != null || candidate.filePath != '''");
                //search in form/recource/oteher files
                if (lastSearchSettings.chkForm)
                    foreach (Candidate subCandidate in candidate.subItems)
                        FindInCandidate(subCandidate, solutionDir, tmpResultList);
                //For test purpose only
                //Task.Delay(500).Wait();
                loop++;
                if (progressInfo != null)
                    ((IProgress<string>)progressInfo).Report(String.Format("Searching{0} item: {1}/{2}", projectInfo, loop, candidates.Count));
                if (progressResult != null)
                    ((IProgress<List<ResultItem>>)progressResult).Report(tmpResultList);                
            }
            return loop; //Task as reutl and return null is ok. If We 
        }



        //Find the phrase in one source
        private void FindInCandidate(Candidate candidate, string solutionDir, List<ResultItem> resultList)
        {
            if (!searchedCandidates.Exists(e => e.filePath == candidate.filePath))
            {
                if (candidate.item != null)
                    FindInProjectItem(candidate.item, lastSearchSettings, resultList, errList, solutionDir, candidate.filePath);
                else if (candidate.filePath != "")
                    FindInFile(candidate.filePath, lastSearchSettings, resultList, solutionDir);
                searchedCandidates.Add(candidate);
            }
        }

        private void FindInDocument(Document document, FindSettings settings, List<ResultItem> resultList, List<ErrData> errList, string solutionDir, string bulkPath)
        {
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == bulkPath.ToLower()))
            {
                int lineIndex = 1;
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
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == bulkPath.ToLower()))
            {
                int lineIndex = 1;
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
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == bulkPath.ToLower()))
            {
                int lineIndex = 1;
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
            if (!searchedCandidates.Exists(e => e.filePath.ToLower() == path.ToLower()))
            {
                StreamReader reader = new StreamReader(path, Encoding.Default);
                try
                {
                    string line = "";
                    int lineIndex = 1;
                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
                        LineToResultList(line, settings, resultList, solutionDir, path, lineIndex);
                        lineIndex++;
                    }
                }
                finally
                {
                    reader.Close();
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
                if (cancellationToken.IsCancellationRequested)
                    return;
                resultItem = new ResultItem()
                {
                    linePath = path,
                    lineContent = line,
                    lineNumber = lineIndex,
                    resultIndex = indexInLine,
                    resultOffset = match.Index,
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
            Project currentProject = null;
            int projCount = 0;
            foreach (Project project in Dte.Solution.Projects)
            {
                if (project.FileName != "")
                {
                    currentProject = project;
                    projCount++;
                }
            }
   
            if (projCount == 1)
                return currentProject;
            else
            {
                currentProject = null;
                if (Dte.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0)
                    currentProject = activeSolutionProjects.GetValue(0) as Project;
                else
                    Debug.Assert(false, "Brak aktywnego projektu.");
                return currentProject;
            }
        }

        private string GetSolutionName()
        {
            return Dte.Solution != null ? System.IO.Path.GetDirectoryName(Dte.Solution.FullName) : "";
        }

        private string GetSolutionFullName()
        {
            if (Dte.Solution.FullName != "")
                return System.IO.Path.GetDirectoryName(Dte.Solution.FullName);
            else
                return "";
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
                candidate = new Candidate()
                {
                    filePath = File.Exists(itemPath) ? itemPath : ""
                };
                if (doc != null)
                {
                    candidate.item = item;
                    candidate.document = doc;
                }

                //if candidate have document or filePath
                if (candidate.filePath != "" || candidate.item != null)
                {
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
                        if (subCandidate.item == item)
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

        //Get selection of document
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

        //Get selection from document. Raise if document or selection doesn't exist
        internal TextSelection GetDocumentSelectionExcept(string path)
        {
            EnvDTE.Document document = GetDocumentByPath(path);
            if(document != null)
            {
                TextSelection selection = GetSelection(document);
                if(selection != null)
                {
                    return selection;
                }
                else
                {
                    Debug.Assert(false, "Brak selection w otwartym dokumencie dla '{0}'" + path);
                }
            }
            else
            {
                Debug.Assert(false, "Brak otwartego dokumentu dla '{0}'" + path);
            }
            return null;
        }

        //Return selection for document for result
        internal TextSelection GetDocumentSelection(ResultItem resultLine)
        {
            TextSelection selection = null;
            if (Dte != null)
            {
                EnvDTE.Window docWindow = Dte.ItemOperations.OpenFile(resultLine.linePath, Constants.vsViewKindTextView);
                selection = GetSelection(Dte.ActiveDocument);
            }
            else
                Debug.Assert(false, "Brak DTE");
            return selection;
        }

        //Return selection for document and select whole line for result
        internal TextSelection GetDocumentSelectionAndGoToLine(ResultItem resultLine)
        {
            TextSelection selection = GetDocumentSelectionExcept(resultLine.linePath);
            selection.GotoLine(resultLine.lineNumber.Value, true);
            return selection;
        }

        //Return selection for document and select string for result
        internal TextSelection GetDocumentSelectionAndSelect(ResultItem resultLine)
        {
            TextSelection selection = GetDocumentSelectionExcept(resultLine.linePath);
            selection.MoveToLineAndOffset(resultLine.lineNumber.Value, resultLine.resultOffset + 1 );
            selection.MoveToLineAndOffset(resultLine.lineNumber.Value, resultLine.resultOffset + 1 +resultLine.ResultLength, true);
            return selection;
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

        //Get Candidate with fields filled for dodument or files
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

        //Get document if exists in VS or null
        private Document GetDocumentByPath(string path)
        {
            foreach (EnvDTE.Document document in Dte.Documents)
                if(document.FullName.ToLower() == path.ToLower())
                    return document;
            return null;
        }




        //Replace
        internal bool GetStrReplaceForm(out string strReplace)
        {
            ReplaceForm replaceForm = new ReplaceForm();
            bool? doReplace = replaceForm.ShowDialog();
            strReplace = replaceForm.tbStrReplace.Text;
            return doReplace.Value;
        }

        internal void ReplaceInSource(string strToReplace, ResultItem resultItem)
        {
            //list of ALL LastResults
            List<ResultItem> resultList = dictResultItems.Select(c => c.Value).ToList<ResultItem>();

            if (GetDocumentByPath(resultItem.linePath) != null)
            {
                RaplaceInDocument(strToReplace, resultItem, resultList);
            }
            else
            {
                ReplaceInFile(resultItem.linePath, resultItem.linePath + "__", resultItem, GetPathRelatesResults(resultList, resultItem.linePath), strToReplace);
            }
        }

        internal void RaplaceInDocument(string strToReplace, ResultItem resultItem, List<ResultItem> resultList)
        {
            TextSelection selection = GetDocumentSelectionAndGoToLine(resultItem);
            if (selection.Text.Substring(resultItem.resultOffset, resultItem.resultContent.Length) != resultItem.resultContent)
            {
                System.Windows.Forms.MessageBox.Show(String.Format("Source doesn't match the phrase. Phrase '{0}', found '{1}'.", resultItem.resultContent, selection.Text));
                return;
            }
            //update selected text in docyment
            selection.Text = strToReplace;
            //update offset in related results
            UpdateResults(resultItem, strToReplace, resultList);
        }

        internal void ReplaceInFiles(string strToReplace, List<ResultItem> resultItemList)
        {
            List<ResultItem> list;
            List<string> paths = new List<string>();
            foreach (ResultItem item in resultItemList)
                if (!paths.Contains(item.linePath))
                    paths.Add(item.linePath);
            foreach (string path in paths)
            {
                list = GetPathRelatesResults(resultItemList, path);
                ReplaceInFile(path, path + "__", list, list, strToReplace);
            }
        }

        internal void UpdateResults(ResultItem resultItem, string strToReplace, List<ResultItem> resultItemList)
        {
            List<ResultItem> changedResults = new List<ResultItem>();
            //update offset in all related (by path and line number) results
            foreach (ResultItem item in resultItemList)
            {
                if (!changedResults.Contains(item) &&
                    item.linePath == resultItem.linePath &&
                    item.lineNumber == resultItem.lineNumber &&
                    item.resultOffset > resultItem.resultOffset)
                {
                    item.resultOffset = item.resultOffset + (strToReplace.Length - resultItem.resultContent.Length);
                    changedResults.Add(item);
                }
            }
            //set result as updated
            resultItem.replaced = true;
        }

        internal List<ResultItem> GetPathRelatesResults(List<ResultItem> resultItemList, string path)
        {
            return resultItemList.Where(i => i.linePath.ToLower() == path.ToLower()).ToList<ResultItem>();
        }

        internal void ReplaceInFile(string path, string newPath, ResultItem resultToChange, List<ResultItem> allResultsForFile, string strToReplace)
        {
            List<ResultItem> list = new List<ResultItem>() { resultToChange };
            ReplaceInFile(path, newPath, list, allResultsForFile, strToReplace);
        }

        internal void ReplaceInFile(string path, string newPath, List<ResultItem> resultsToChangeForFile, List<ResultItem> allResultsForFile, string strToReplace)
        {
            //create temporary file
            using (StreamReader reader = new StreamReader(path))
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(newPath))
                    {
                        try
                        {
                            int lineNumber = 1;
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                foreach (ResultItem item in resultsToChangeForFile)
                                {
                                    if (!item.replaced && item.lineNumber == lineNumber)
                                    {
                                        line = line.Substring(0, item.resultOffset) + strToReplace + line.Substring(item.resultOffset + item.ResultLength);
                                        UpdateResults(item, strToReplace, allResultsForFile);
                                    }
                                }
                                writer.WriteLine(line);
                                lineNumber++;
                            }
                        }
                        catch
                        {
                            Console.WriteLine(String.Format("Couldn't write '{0}'", newPath));
                        }
                        finally
                        {
                            writer.Close();
                        }
                    }
                }
                catch
                {
                    Console.WriteLine(String.Format("Couldn't read '{0}'", path));
                }
                finally
                {
                    reader.Close();
                }
            }

            //replace old file with the new one
            File.Delete(path);
            File.Move(newPath, path);
        }




        //Save results to file
        internal void SaveResultsToFile(Button botton)
        {
            List<string> lines = new List<string>();
            TreeView tv = saveDict[botton];
            foreach (TreeViewItem item in tv.Items)
            {
                SaveResultsToFile(lines, item, 0);
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog()
            {
                FileName = "Result", // Default file name
                DefaultExt = ".txt", // Default file extension
                Filter = "Text documents (.txt)|*.txt", // Filter files by extension
                                                        // Show save file dialog box
            };
            Nullable<bool> result = dlg.ShowDialog();
            // Process save file dialog box results
            if (result == true)
            {
                string filename = dlg.FileName;
                StreamWriter sw = new StreamWriter(filename);
                foreach (string line in lines)
                    sw.WriteLine(line);
                sw.Close();
            }
        }

        internal void SaveResultsToFile(List<string> lines, TreeViewItem item, int level)
        {
            string line = "";
            for (int i = 0; i < level; i++)
                line += "\t";

            if (dictResultItems.ContainsKey(item))
            {
                ResultItem result = dictResultItems[item];
                line += String.Format("{0}/{1} '{2}': \"{3}\"", result.lineNumber, result.resultOffset, result.resultContent, result.lineContent.Trim());
            }
            else
                line += item.Header;
            lines.Add(line);
            foreach (TreeViewItem subItem in item.Items)
            {
                SaveResultsToFile(lines, subItem, level + 1);
            }
        }
    }
}