using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using EnvDTE;
namespace VSFindTool
{
    class Candidate
    {
        internal string path = "";
        internal string FileName
        {
            get 
            {
                return Path.GetFileName(path);
            }
        }
    }
}