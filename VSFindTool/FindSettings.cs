using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using System.Windows.Controls;
using System.Windows;
using Microsoft.VisualStudio.Text.Operations;

namespace VSFindTool
{
    class FindSettings
    {
        internal string tbPhrase;
        internal bool chkWholeWord;
        internal bool chkForm;
        internal bool chkCase;
        internal bool chkRegExp;
        internal bool rbCurrDoc;
        internal bool rbOpenDocs;
        internal bool rbProject;
        internal bool rbSolution;
        internal bool rbLocation;
        internal bool rbLastResults;
        internal string tbLocation;
        internal string tbfileFilter;

        internal string GetPrefix()
        {
            if (chkWholeWord && tbPhrase.StartsWith(@"\b"))
                return  @"\b"; // prefix = "(^|[\\s,\\.,\\,])";
            else
                return "";
        }

        internal string GetSufix()
        {
            if (chkWholeWord && tbPhrase.EndsWith(@"\b"))
                return @"\b"; // prefix = "(^|[\\s,\\.,\\,])";
            else
                return "";
        }
        
        public string ToLabelString()
        {
            string result = "";

            if (chkWholeWord)
                result += "W ";
            else
                result += "w ";

            if (chkCase)
                result += "C ";
            else
                result += "c ";

            if (chkForm)
                result += "F";
            else
                result += "f";

            if (chkRegExp)
                result += "R ";
            else
                result += "r ";

            if (rbCurrDoc)
                result += " [CurDocum] ";
            else if (rbOpenDocs)
                result += " [Opened] ";
            else if (rbProject)
                result += " [Project] ";
            else if (rbSolution)
                result += " [Solution] ";
            else if (rbLocation)
                result += " [" + tbLocation + " / " + tbfileFilter + "] ";
            else if (rbLastResults)
                result += " [LastRes]";

            result += "'" + tbPhrase + "'";


            return result;
        }

        public FindSettings GetCopy()
        {
            return new FindSettings()
            {
                tbPhrase = tbPhrase,
                chkWholeWord = chkWholeWord,
                chkForm = chkForm,
                chkCase = chkCase,
                chkRegExp = chkRegExp,
                rbCurrDoc = rbCurrDoc,
                rbOpenDocs = rbOpenDocs,
                rbProject = rbProject,
                rbSolution = rbSolution,
                rbLocation = rbLocation,
                rbLastResults = rbLastResults,
                tbLocation = tbLocation,
                tbfileFilter = tbfileFilter
            };
        }

        public void Settings2Form(VSFindToolMainFormControl form)
        {
            //Search phrase
            form.tbPhrase.Text = tbPhrase;

            //Find options
            form.chkWholeWord.IsChecked = chkWholeWord;
            form.chkForm.IsChecked = chkForm;
            form.chkCase.IsChecked = chkCase;
            form.chkRegExp.IsChecked = chkRegExp;

            //Look in
            form.rbCurrDoc.IsChecked = rbCurrDoc;
            form.rbOpenDocs.IsChecked = rbOpenDocs;
            form.rbProject.IsChecked = rbProject;
            form.rbSolution.IsChecked = rbSolution;
            form.rbLocation.IsChecked = rbLocation;
            form.rbLastResults.IsChecked = rbLastResults;
            form.tbLocation.Text = tbLocation;
            form.SetTbFileFilter(tbfileFilter);
        } 
        
        public void Form2Settings(VSFindToolMainFormControl form)
        {
            //Search phrase
            tbPhrase = form.tbPhrase.Text;

            //Find options
            chkWholeWord = form.chkWholeWord.IsChecked == true;
            chkForm = form.chkForm.IsChecked == true;
            chkCase = form.chkCase.IsChecked == true;
            chkRegExp = form.chkRegExp.IsChecked == true;

            //Look in
            rbCurrDoc = form.rbCurrDoc.IsChecked == true;
            rbOpenDocs = form.rbOpenDocs.IsChecked == true;
            rbProject = form.rbProject.IsChecked == true;
            rbSolution = form.rbSolution.IsChecked == true;
            rbLocation = form.rbLocation.IsChecked == true;
            rbLastResults = form.rbLastResults.IsChecked == true;
            tbfileFilter = form.cbFileMask.Text;
        }
    }
}
