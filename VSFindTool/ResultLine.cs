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
        internal string solutionDir;
        internal string resultLineText;
        internal string linePath;
        //internal List<string> linePathPartsList;
        internal string lineContent;
        internal int? lineInFileNumbe;
        internal int textInLineNumer = 0;
        private List<string> _pathPartsList = null;
        internal List<string> pathPartsList
        {
            get{
                if (_pathPartsList == null)
                    _pathPartsList = linePath.Split('\\').ToList<string>();//Path.GetDirectoryName(linePath).Split('\\').ToList<string>();
                return _pathPartsList;
            }
        }
    }

    class TVData
    {
        internal string shortDir = @"[...]";
        internal string longDir;
    }
}
