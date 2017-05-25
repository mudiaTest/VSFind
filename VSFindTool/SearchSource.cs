using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSFindTool
{
    class SearchSource
    {
        internal EnvDTE.Window window = null;
        internal EnvDTE.Document document = null;
        internal string Path = "";
        internal string windowContent = "";
    }
}
