using System;
using System.Collections.Generic;
using System.Linq;

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
                    _pathPartsList = linePath.Split('\\').ToList<string>();
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

        internal (string, string, string) GetSplitLine(string line, bool trimStart)
        {
            line = line.TrimEnd();
            string pre = line.Substring(0, resultOffset);
            string res = line.Substring(resultOffset, ResultLength);            
            int offset2 = resultOffset + ResultLength;
            string post = line.Substring(offset2, Math.Min(line.Length - offset2, 300));
            if (trimStart)
                return (pre.TrimStart(), res, post);
            else
                return (pre, res, post);
        }
    }

    class TVData
    {
        internal string shortDir = @"[...]";
        internal string longDir;
    }
}
