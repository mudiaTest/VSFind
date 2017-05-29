using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VSFindTool
{
    class ResultLineData
    {
        //May not correspon to actual file when the document is dirty!
        internal string linePath; //path to file with the line
        internal string lineContent; //complete line with found result, not trimmed.
        internal int? lineNumber; //number of line with result in file/document.  
        internal int resultIndex; //index of result in line: 0, 1, 2...
        internal int resultOffset; //offset of result in line;
        internal int resultLength; //length of found result
        internal string resultContent; //value of found result

        private List<string> _pathPartsList = null;
        internal List<string> PathPartsList
        {
            get{
                if (_pathPartsList == null)
                    _pathPartsList = linePath.Split('\\').ToList<string>();//Path.GetDirectoryName(linePath).Split('\\').ToList<string>();
                return _pathPartsList;
            }
        }

        internal ResultLineData GetCopy()
        {
            return new ResultLineData()
            {
                linePath = linePath,
                lineContent = lineContent,
                lineNumber = lineNumber,
                resultOffset = resultOffset,
                resultLength = resultLength,
                resultContent = resultContent
            };
        }
    }

    class TVData
    {
        internal string shortDir = @"[...]";
        internal string longDir;
    }
}
