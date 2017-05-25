﻿using System;
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
        internal string tbLocation;
        internal string tbfileFilter;
        
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
                tbLocation = tbLocation,
                tbfileFilter = tbfileFilter
            };
        }

        public void SetColtrols(VSFindToolMainFormControl form)
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
            form.tbLocation.Text = tbLocation;
            form.SetTbFileFilter(tbfileFilter);
        }        
    }
}
