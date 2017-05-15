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

        private Label AddLabel(string text, WrapPanel infoWrapPanel)
        {
            Label lbl = new Label() { Content = text, Padding = new Thickness(2, 0, 1, 0), Margin = new Thickness(0, 2, 0, 0) };
            infoWrapPanel.Children.Add(lbl);
            return lbl;
        }

        private Label AddBold(Label lbl)
        {
            lbl.FontWeight = FontWeights.Bold;
            return lbl;
        }

        private Label AddExtraBold(Label lbl)
        {
            lbl.FontWeight = FontWeights.ExtraBold;
            return lbl;
        }

        public void FillWraperPanel(WrapPanel infoWrapPanel)
        {
            infoWrapPanel.Children.Clear();

            //WholeWord
            if (chkWholeWord)
                AddExtraBold(AddLabel("W", infoWrapPanel));
            else
                AddLabel("w", infoWrapPanel);

            //Form
            if (chkForm)
                AddExtraBold(AddLabel("F", infoWrapPanel));
            else
                AddLabel("f", infoWrapPanel);

            //CharCase
            if (chkCase)
                AddExtraBold(AddLabel("C", infoWrapPanel));
            else
                AddLabel("c", infoWrapPanel);

            //RegExp
            if (chkRegExp)
                AddExtraBold(AddLabel("R", infoWrapPanel));
            else
                AddLabel("r", infoWrapPanel);

            //separator
            AddExtraBold(AddLabel(" | ", infoWrapPanel));

            if (rbCurrDoc)
                AddLabel("CurDocum", infoWrapPanel);
            else if (rbOpenDocs)
                AddLabel("Opened", infoWrapPanel);
            else if (rbProject)
                AddLabel("Project", infoWrapPanel);
            else if (rbSolution)
                AddLabel("Solution", infoWrapPanel);
            else if (rbLocation)
                AddLabel(tbLocation + " / " + tbfileFilter, infoWrapPanel);

            //separator
            AddExtraBold(AddLabel(" | ", infoWrapPanel));

            AddLabel("`" + tbPhrase + "`", infoWrapPanel);
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
            form.SetTbfileFilter(tbfileFilter);
        }
    }
}
