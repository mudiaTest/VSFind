﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSFindTool
{
    class ResultLineData
    {
        internal string solutionDir;
        internal string resultLineText;
        internal string linePath;
        internal List<string> linePathPartsList;
        internal string lineContent;
        internal int? lineInFileNumbe;
        internal int textInLineNumer = 0;
    }

    class TVData
    {
        internal string shortDir = @"[...]";
        internal string longDir;
    }
}
