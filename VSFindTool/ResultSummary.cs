using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSFindTool
{
    class ResultSummary
    {
        internal int searchedFiles = 0;
        internal int foundResults = 0;

        internal void Clear()
        {
            searchedFiles = 0;
            foundResults = 0;
        }

        public ResultSummary GetCopy()
        {
            return new ResultSummary()
            {
                searchedFiles = searchedFiles,
                foundResults = foundResults
            };
        }
    }
}
