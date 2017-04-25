using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input; //Dla ICommand
using System.Linq;
using System.ComponentModel;

namespace VSFindTool
{
    public class ResultLine
    {
        readonly List<ResultLine> _subItems = new List<ResultLine>();
        public IList<ResultLine> subItems
        {
            get { return _subItems; }
        }

        public string linePath;
        public List<string> linePathPartsList = new List<string>();
        public string lineContent;
        public int? lineInFileNumber;
        public string header;
    }

    public class ResultLineViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ReadOnlyCollection<ResultLineViewModel> _subItems;
        readonly ResultLineViewModel _parentItem;
        readonly ResultLine _resultLine;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructors

        public ResultLineViewModel(ResultLine resultLine)
            : this(resultLine, null)
        {
        }

        private ResultLineViewModel(ResultLine resultLine, ResultLineViewModel parentItem)
        {
            _resultLine = resultLine;
            _parentItem = parentItem;

            _subItems = new ReadOnlyCollection<ResultLineViewModel>(
                    (from child in _resultLine.subItems
                     select new ResultLineViewModel(child, this))
                     .ToList<ResultLineViewModel>());
        }

        #endregion // Constructors

        #region ResultLine Properties

        public ReadOnlyCollection<ResultLineViewModel> subItems
        {
            get { return _subItems; }
        }


        public string linePath
        {
            get { return _resultLine.linePath; }
        }
        public IList<string> linePathPartsList
        {
            get { return _resultLine.linePathPartsList; }
        }
        public string lineContent
        {
            get { return _resultLine.lineContent; }
        }
        public int? lineInFileNumber
        {
            get { return _resultLine.lineInFileNumber; }
        }
        public string header
        {
            get { return _resultLine.header; }
        }


        #endregion // ResultLine Properties

        #region Presentation Members

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parentItem != null)
                    _parentItem.IsExpanded = true;
            }
        }

        #endregion // IsExpanded

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion // IsSelected

        #region NameContainsText
        /*
        public bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.Name))
                return false;

            return this.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }
        */

        #endregion // NameContainsText

        #region parentItem

        public ResultLineViewModel parentItem
        {
            get { return _parentItem; }
        }

        #endregion // parentItem

        #endregion // Presentation Members        

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }

    public class ResultTreeViewModel
    {
        #region Data

        readonly ReadOnlyCollection<ResultLineViewModel> rootCollection;
        readonly ResultLineViewModel _rootItem;
        readonly ICommand _searchCommand;

        IEnumerator<ResultLineViewModel> _matchingPeopleEnumerator;
        string _searchText = String.Empty;

        #endregion // Data

        #region Constructor

        public ResultTreeViewModel(ResultLine rootLine)
        {
            _rootItem = new ResultLineViewModel(rootLine);

            rootCollection = new ReadOnlyCollection<ResultLineViewModel>(
                new ResultLineViewModel[]
                {
                    _rootItem
                });

            //_searchCommand = new SearchFamilyTreeCommand(this);
        }

        #endregion // Constructor

        #region Properties

        #region FirstGeneration

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ReadOnlyCollection<ResultLineViewModel> FirstGeneration
        {
            get { return rootCollection; }
        }

        #endregion // FirstGeneration

        #region SearchCommand

        /// <summary>
        /// Returns the command used to execute a search in the family tree.
        /// </summary>
        /*public ICommand SearchCommand
        {
            get { return _searchCommand; }
        }

        private class SearchFamilyTreeCommand : ICommand
        {
            readonly ResultTreeViewModel _familyTree;

            public SearchFamilyTreeCommand(ResultTreeViewModel familyTree)
            {
                _familyTree = familyTree;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            event EventHandler ICommand.CanExecuteChanged
            {
                // I intentionally left these empty because
                // this command never raises the event, and
                // not using the WeakEvent pattern here can
                // cause memory leaks.  WeakEvent pattern is
                // not simple to implement, so why bother.
                add { }
                remove { }
            }

            public void Execute(object parameter)
            {
                _familyTree.PerformSearch();
            }
        }*/

        #endregion // SearchCommand

        #region SearchText

        /// <summary>
        /// Gets/sets a fragment of the name to search for.
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (value == _searchText)
                    return;

                _searchText = value;

                _matchingPeopleEnumerator = null;
            }
        }

        #endregion // SearchText

        #endregion // Properties

        #region Search Logic

        /*void PerformSearch()
        {
            if (_matchingPeopleEnumerator == null || !_matchingPeopleEnumerator.MoveNext())
                this.VerifyMatchingPeopleEnumerator();

            var person = _matchingPeopleEnumerator.Current;

            if (person == null)
                return;

            // Ensure that this person is in view.
            if (person.parentItem != null)
                person.parentItem.IsExpanded = true;

            person.IsSelected = true;
        }*/

        /*void VerifyMatchingPeopleEnumerator()
        {
            var matches = this.FindMatches(_searchText, root);
            _matchingPeopleEnumerator = matches.GetEnumerator();

            if (!_matchingPeopleEnumerator.MoveNext())
            {
                MessageBox.Show(
                    "No matching names were found.",
                    "Try Again",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
            }
        }*/

        /*IEnumerable<ResultLineViewModel> FindMatches(string searchText, ResultLineViewModel person)
        {
            if (person.NameContainsText(searchText))
                yield return person;

            foreach (ResultLineViewModel child in person.subItems)
                foreach (ResultLineViewModel match in this.FindMatches(searchText, child))
                    yield return match;
        }*/

        #endregion // Search Logic
    }
}

