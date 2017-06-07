using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VSFindTool
{
    class ResultItem
    {
        //May not correspon to actual file when the document is dirty!
        internal string linePath; //path to file with the line
        internal string lineContent; //complete line with found result, not trimmed.
        internal int? lineNumber; //number of line with result in file/document. First line IS 1 NOT 0
        internal int resultIndex; //index of result in line: 0, 1, 2...
        internal int resultOffset; //number of first sing of resylt in line, where first sign in line is on position 0. Selection Move...Offset moves to BEFOTE this number, so it must be osed with "resultOffset+1"
        internal string resultContent; 
        internal bool replaced = false; //if found result has been replaced
        internal bool belongsToLastResults = false; //does result belog to Last...TreeView

        private List<string> _pathPartsList = null;

        internal int ResultLength //value of found result
        {
            get
            {
                return resultContent.Length;
            }
        }

        internal List<string> PathPartsList
        {
            get{
                if (_pathPartsList == null)
                    _pathPartsList = linePath.Split('\\').ToList<string>();//Path.GetDirectoryName(linePath).Split('\\').ToList<string>();
                return _pathPartsList;
            }
        }

        internal ResultItem GetCopy()
        {
            return new ResultItem()
            {
                linePath = linePath,
                lineContent = lineContent,
                lineNumber = lineNumber,
                resultOffset = resultOffset,
                resultContent = resultContent,
                replaced = replaced
            };
        }
    }

    class TVData
    {
        internal string shortDir = @"[...]";
        internal string longDir;
    }
}
