﻿using EnvDTE;
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

namespace VSFindTool
{
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for VSFindToolMainFormControl.
    /// </summary>
    public partial class VSFindToolMainFormControl : UserControl
    {
        private object threadLock = new object();

        public delegate void FinishDelegate();

        List<ResultLineData> resultList = new List<ResultLineData>();
        List<ErrData> errList = new List<ErrData>();
        FindSettings last_searchSettings = new FindSettings();
        Dictionary<TreeViewItem, ResultLineData> dictResultLines = new Dictionary<TreeViewItem, ResultLineData>();
        Dictionary<TreeViewItem, FindSettings> dictSearchSettings = new Dictionary<TreeViewItem, FindSettings>();
        Dictionary<FindSettings, TextBox> dictTBPreview = new Dictionary<FindSettings, TextBox>();
        Dictionary<TreeView, TVData> dictTVData = new Dictionary<TreeView, TVData>();
        Dictionary<FindSettings, ResultSummary> dictResultSummary = new Dictionary<FindSettings, ResultSummary>();


        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactory { get; set; }


        Connect c = new Connect();


        public string GetFindResults2Content()
        {
            var findWindow1 = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults1);
            var findWindow = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
            findWindow.Activate();
            var selection = findWindow.Selection as EnvDTE.TextSelection;
            selection.SelectAll();
            var selection1 = findWindow1.Selection as EnvDTE.TextSelection;
            selection1.SelectAll();
            string result1 = selection1.Text;
            string result = selection.Text;
            return result;
        }

        internal string GetDocumentContent(Document document, List<ErrData> errList)
        {
            if (document.Selection == null)
            {
                errList.Add(new ErrData()
                {
                    path = document.FullName,
                    caption = document.ActiveWindow != null ? document.ActiveWindow.Caption : "",
                    info = "No Selection object found."
                });
                return "";
            }
            EnvDTE.TextSelection selection = document.Selection as EnvDTE.TextSelection;
            int line = selection.ActivePoint.Line;
            int lineCharOffset = selection.ActivePoint.LineCharOffset;
            selection.SelectAll();
            string result = selection.Text;
            selection.MoveToLineAndOffset(line, lineCharOffset);
            return result;
        }

        public void HideFindResult2Window()
        {
            var findWindow = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
            findWindow.Visible = false;
        }

        public void BringBackFindResult2Value()
        {
            var findWindow = Dte.Windows.Item(EnvDTE.Constants.vsWindowKindFindResults2);
            var selection = findWindow.Selection as EnvDTE.TextSelection;
            selection.StartOfDocument();
            selection.Delete(2);
            /*Doesn't work*/
        }

        public void MoveResultToTextBox()
        {
            //tbResult.Text = GetFindResults2Content();
        }


        private ResultSummary GetResultSummary(FindSettings settings)
        {
            if (!dictResultSummary.ContainsKey(settings))
                dictResultSummary.Add(settings, new ResultSummary());
            return dictResultSummary[settings];
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
            lock (threadLock)
            {
                FillWraperPanel(last_searchSettings, last_infoWrapPanel);
                FillResultSummary(last_LabelInfo, GetResultSummary(last_searchSettings));
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
        }

        private void ExecSearch()
        {
            var progress = new Progress<string>();
            progress.ProgressChanged += (o, info) =>
            {
                last_LabelInfo.Content = info;
            };

            dictResultSummary.Remove(last_searchSettings);

            if (Dte.Solution.FullName != "")
            {
                
                string solutionDir = System.IO.Path.GetDirectoryName(Dte.Solution.FullName);
                if (last_searchSettings.rbCurrDoc)
                {
                    FindInDocument(LastDocWindow.Document, last_searchSettings, resultList, errList, solutionDir, LastDocWindow.Document.FullName);
                    Finish();
                }
                else if (last_searchSettings.rbOpenDocs)
                {
                    List<Candidate> candidates = GetCandidates(GetActiveProject());
                    foreach (SearchSource searchSource in GetOpenDocuments())
                    {
                        if (searchSource.document != null) {
                            Candidate candidate = candidates.FirstOrDefault<Candidate>((e => e.path.ToLower() == searchSource.document.FullName.ToLower()));
                            if (candidate != null)
                                FindInDocument(searchSource.document, last_searchSettings, resultList, errList, solutionDir, candidate.path);
                        }
                        else if(searchSource.windowContent != "")
                        {
                            Debug.Assert(false, "You shouldnt be here.");
                            FindInSelection(searchSource.windowContent, last_searchSettings, resultList, errList, solutionDir, "dummy");
                        }
                        else
                        {
                            Debug.Assert(false, "You shouldnt be here also.");
                        }
                    }
                    Finish();
                }
                else if (last_searchSettings.rbProject)
                {
                    FindInProject(progress, Finish, GetActiveProject(), last_searchSettings, resultList, solutionDir);
                }
                else if (last_searchSettings.rbSolution)
                {
                    FindInProjects(progress, Finish, last_searchSettings, resultList, solutionDir);
                }
                else if (last_searchSettings.rbLocation)
                {
                    FindInLocation(progress, Finish, last_searchSettings, resultList, solutionDir);
                }
                tbiLastResult.Focus();
            }
        }

        private void StartSearch()
        {
            LastDocWindow = ((VSFindTool.VSFindToolPackage)(this.parentToolWindow.Package)).LastDocWindow;

            //Search phrase
            last_searchSettings.tbPhrase = tbPhrase.Text;

            //Find options
            last_searchSettings.chkWholeWord = chkWholeWord.IsChecked == true;
            last_searchSettings.chkForm = chkForm.IsChecked == true;
            last_searchSettings.chkCase = chkCase.IsChecked == true;
            last_searchSettings.chkRegExp = chkRegExp.IsChecked == true;

            //Look in
            last_searchSettings.rbCurrDoc = rbCurrDoc.IsChecked == true;
            last_searchSettings.rbOpenDocs = rbOpenDocs.IsChecked == true;
            last_searchSettings.rbProject = rbProject.IsChecked == true;
            last_searchSettings.rbSolution = rbSolution.IsChecked == true;
            last_searchSettings.rbLocation = rbLocation.IsChecked == true;
            last_searchSettings.tbLocation = tbLocation.Text;
            last_searchSettings.tbfileFilter = cbFileMask.Text;
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
                else
                {
                    tbiLastResult.IsSelected = true;
                }
            }
            resultList.Clear();
            ExecSearch();
        }


        public void ShowStatus(string info)
        {
            last_LabelInfo.Content = info;
        }



        private async void FindInProjects(IProgress<string> progress, FinishDelegate finish, FindSettings settings, List<ResultLineData> resultList, string solutionDir)
        {
            foreach (Project project in Dte.Solution.Projects)
            {
                await FindInProject(progress, null, project, last_searchSettings, resultList, solutionDir);
                //await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(1));
            }
            finish();
        }



        private async System.Threading.Tasks.Task FindInProject(IProgress<string> progress, FinishDelegate finish, Project project, FindSettings settings, List<ResultLineData> resultList, string solutionDir)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                bool asDocument = false;
                int loop = 0;
                List<Candidate> candidates = GetCandidates(project);
                foreach (Candidate candidate in candidates)
                {
                    loop++;
                    if (progress != null)
                        progress.Report(String.Format("Searching {0}/{1}", loop, candidates.Count));
                    //await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(0));
                    asDocument = false;
                    foreach (EnvDTE.Window window in Dte.Windows)
                    {
                        if (window.Document != null)
                        {
                            if (window.Document.FullName == candidate.path)
                            {
                                FindInDocument(window.Document, last_searchSettings, resultList, errList, solutionDir, candidate.path);
                                asDocument = true;
                                break;
                            }
                        }
                    }
                    if (!asDocument)
                        FindInFile(candidate.path, last_searchSettings, resultList, solutionDir);
                }
            });
            if (finish != null)
                finish();
            return; //Task as reutl and return null is ok. If We 
        }

        private void FindInProjectItem(ProjectItem projectItem, FindSettings settings, List<ResultLineData> resultList, string solutionDir, string parentPath)
        {
            string potentialSubPath = Path.Combine(parentPath, projectItem.Name);
            if (projectItem.FileCodeModel != null)
            {
                if (projectItem.Document != null)
                    FindInDocument(projectItem.Document, last_searchSettings, resultList, errList, solutionDir, Path.Combine(parentPath, projectItem.Name));
                else
                    FindInFile(Path.Combine(parentPath, projectItem.Name), last_searchSettings, resultList, solutionDir);
            }
            else if (Directory.Exists(potentialSubPath))
            {
                foreach (ProjectItem subItem in projectItem.ProjectItems)
                {
                    FindInProjectItem(subItem, settings, resultList, solutionDir, potentialSubPath);
                }
            }
        }

        private void FindInDocument(Document document, FindSettings settings, List<ResultLineData> resultList, List<ErrData> errList, string solutionDir, string bulkPath)
        {
            int lineIndex = 0;
            GetResultSummary(settings).searchedFiles++;
            List<string> resList = GetDocumentContent(document, errList).Replace("\n\r", "\n").Split('\n').ToList<string>();
            foreach (string line in resList)
            {
                LineToResultList(line, settings, resultList, solutionDir, bulkPath, lineIndex);
                lineIndex++;
            }
        }

        private void FindInSelection(string selection, FindSettings settings, List<ResultLineData> resultList, List<ErrData> errList, string solutionDir, string bulkPath)
        {
            int lineIndex = 0;
            GetResultSummary(settings).searchedFiles++;

            List<string> resList = selection.Replace("\r\n", "\n").Split('\n').ToList();
            //List <string> resList = GetDocumentContent(document, errList).Replace("\n\r", "\n").Split('\n').ToList<string>();

            foreach (string line in resList)
            {
                LineToResultList(line, settings, resultList, solutionDir, bulkPath, lineIndex);
                lineIndex++;
            }
        }

        private void FindInFile(string path, FindSettings settings, List<ResultLineData> resultList, string solutionDir)
        {
            StreamReader stream = new StreamReader(path, Encoding.Default);
            string line = stream.ReadLine();
            int lineIndex = 0;
            GetResultSummary(settings).searchedFiles++;
            while (!stream.EndOfStream)
            {
                LineToResultList(line, settings, resultList, solutionDir, path, lineIndex);
                line = stream.ReadLine();
                lineIndex++;
            }
        }

        private void LineToResultList(string line, FindSettings settings, List<ResultLineData> resultList, string solutionDir, string path, int lineIndex)
        {
            ResultLineData resultLineData;
            int indexInLine = 0;

            string prefix = "";
            string sufix = "";
            string phrase = settings.tbPhrase;
            MatchCollection lineResults;

            if (!settings.chkRegExp)
            {
                phrase = Regex.Escape(phrase);
            }
            if (settings.chkWholeWord)
            {
                if (!phrase.StartsWith(@"\b"))
                    prefix = @"\b"; // prefix = "(^|[\\s,\\.,\\,])";
                if (!phrase.EndsWith(@"\b"))
                    sufix = @"\b"; //sufix = "($|[\\s,\\.,\\,])";
            }
            if (settings.chkCase)
                lineResults = Regex.Matches(line, prefix + phrase + sufix);
            else
                lineResults = Regex.Matches(line, prefix + phrase + sufix, RegexOptions.IgnoreCase);

            foreach (Match lineResult in lineResults)
            {
                resultLineData = new ResultLineData()
                {
                    solutionDir = "",
                    resultLineText = "",
                    linePath = path,
                    lineContent = line.Trim(),
                    lineInFileNumbe = lineIndex,
                    textInLineNumer = lineResult.Index,
                    textLength = lineResult.Length,
                    foundResult = lineResult.Value
                };
                lock (threadLock)
                {
                    resultList.Add(resultLineData);
                    GetResultSummary(settings).foundResults++;
                }
                indexInLine++;
            }

            /*while (indexInLine != -1)
            {
                if (settings.chkCase)
                    indexInLine = line.IndexOf(settings.tbPhrase, indexInLine);
                else
                    indexInLine = line.ToUpper().IndexOf(settings.tbPhrase.ToUpper(), indexInLine);

                

                if (indexInLine != -1)
                {
                    resultLineData = new ResultLineData()
                    {
                        solutionDir = "",
                        resultLineText = "",
                        linePath = path,
                        lineContent = line.Trim(),
                        lineInFileNumbe = lineIndex,
                        textInLineNumer = indexInLine
                    };
                    lock (threadLock)
                    {
                        resultList.Add(resultLineData);
                    }
                    indexInLine++;
                }
            }*/
        }


        private async System.Threading.Tasks.Task FindInLocation(IProgress<string> progress, FinishDelegate finish, FindSettings settings, List<ResultLineData> resultList, string solutionDir)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                bool asDocument = false;
                int loop = 0;
                List<Candidate> candidates = new List<Candidate>();

                List<string> filterList = new List<string>();
                foreach(string filter in settings.tbfileFilter.Split(';').ToList())
                {
                    filterList.Add(filter.Replace(".", "\\.").Replace("*", ".*")+"$");
                }

                GetCandidatesFromLocation(settings.tbLocation, candidates, filterList);
                foreach (Candidate candidate in candidates)
                {
                    loop++;
                    if (progress != null)
                        progress.Report(String.Format("Searching {0}/{1}", loop, candidates.Count));
                    //await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(0));
                    FindInFile(candidate.path, last_searchSettings, resultList, solutionDir);
                }
            });
            if (finish != null)
                finish();
            return;
        }


        private List<SearchSource> GetOpenDocuments()
        {
            SearchSource searchSource;
            List<SearchSource> result = new List<SearchSource>();
            EnvDTE.TextSelection selection = null;
            foreach (EnvDTE.Window window in Dte.Windows)
            {
                if (window.Document != null)
                {
                    searchSource = new SearchSource();
                    if (window.Caption == Path.GetFileName(window.Document.FullName))
                    {
                        searchSource.document = window.Document;
                        if (!result.Exists(e => e.document.FullName == window.Document.FullName))
                            result.Add(searchSource);
                    }
                    else
                    {
                        /*
                        selection = null;
                        try
                        {
                            selection = window.Selection as EnvDTE.TextSelection;
                        }
                        catch
                        {
                            selection = null;
                        }
                        if (selection != null)
                        {
                            selection.SelectAll();
                            searchSource.window = window;
                            searchSource.windowContent = selection.Text;
                        }
                        result.Add(searchSource);*/
                    }

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


        private List<Candidate> GetCandidates(Project project)
        {
            List<Candidate> result = new List<Candidate>();
            foreach (ProjectItem item in project.ProjectItems)
            {
                GetCandidates(item, result, Path.GetDirectoryName(project.FullName));
            }
            return result;
        }

        private void GetCandidatesFromLocation(string parentDirPath, List<Candidate> result, List<string> fileFilters)
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
                    result.Add(new Candidate() { path = filePath });
            }
            foreach (string dirPath in Directory.GetDirectories(parentDirPath))
            {
                GetCandidatesFromLocation(dirPath, result, fileFilters);
            }

        }


        private void GetCandidates(ProjectItem item, List<Candidate> result, string parentPath)
        {
            string itemPath = Path.Combine(parentPath, item.Name);
            if (item.FileCodeModel != null)
            {

                if (File.Exists(itemPath))
                {
                    if (!result.Exists(e => e.path == itemPath))
                        result.Add(new Candidate()
                        {
                            path = itemPath
                        });
                    foreach (ProjectItem subItem in item.ProjectItems)
                    {
                        GetCandidates(subItem, result, parentPath);
                    }
                }
            }
            else
            {
                string potentialSubPath = itemPath;
                if (Directory.Exists(potentialSubPath))
                    foreach (ProjectItem subItem in item.ProjectItems)
                    {
                        GetCandidates(subItem, result, potentialSubPath);
                    }
            }
        }
    }
}