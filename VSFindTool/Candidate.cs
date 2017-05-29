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
        //internal int type; //1:code file, 2:form file file, 3: other file (ie resource)
        internal List<Candidate> subItems = new List<Candidate>();

        internal ProjectItem item = null;
        internal Document document = null;
        internal string DocumentPath
        {
            get
            {
                if (document != null)
                    return document.FullName;
                else
                    return "";
            }
        }
        internal string filePath = "";
        internal string FileName
        {
            get 
            {
                return Path.GetFileName(filePath);
            }
        }
    }
}