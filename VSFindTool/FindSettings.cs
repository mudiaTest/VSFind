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
        internal vsFindAction action;
        internal vsFindResultsLocation location;
        //internal strind searchPath;

        public void ToFind(Find find)
        {
            find.Action = action;
            find.ResultsLocation = location;

            //Search phrase
            find.FindWhat = tbPhrase;

            //Find options
            find.MatchWholeWord = chkWholeWord;
            find.MatchCase = chkCase;
            if (chkRegExp)
                find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxRegExpr;
            else
                find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxLiteral;

            //Look in
            if (rbCurrDoc)
                find.Target = vsFindTarget.vsFindTargetCurrentDocument;
            else if (rbOpenDocs)
                find.Target = vsFindTarget.vsFindTargetOpenDocuments;
            else if (rbProject)
                find.Target = vsFindTarget.vsFindTargetCurrentProject;
            else if (rbSolution)
                find.Target = vsFindTarget.vsFindTargetSolution;
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

            result += "'" + tbPhrase + "'";


            return result;
        }

        public FindSettings GetCopy()
        {
            FindSettings result = new FindSettings()
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
                action = action,
                location = location
            };
            return result;
        }

        public void SetColtrols(VSFindToolMainFormControl form)
        {
            //Search phrase
            form.tbPhrase.Text = tbPhrase;

            //Find options
            form.chkWholeWord.IsChecked = chkWholeWord;
            form.chkForm.IsChecked = chkForm;
            form.chkCase.IsChecked = chkCase;

            //Look in
            form.rbCurrDoc.IsChecked = rbCurrDoc;
            form.rbOpenDocs.IsChecked = rbOpenDocs;
            form.rbProject.IsChecked = rbProject;
            form.rbSolution.IsChecked = rbSolution;
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
            if (chkWholeWord)
                AddExtraBold(AddLabel("W", infoWrapPanel));
            else
                AddLabel("w", infoWrapPanel);
            if (chkForm)
                AddExtraBold(AddLabel("W", infoWrapPanel));
            else
                AddLabel("w", infoWrapPanel);
            if (chkCase)
                AddExtraBold(AddLabel("C", infoWrapPanel));
            else
                AddLabel("c", infoWrapPanel);
            if (chkRegExp)
                AddExtraBold(AddLabel("R", infoWrapPanel));
            else
                AddLabel("r", infoWrapPanel);

            AddExtraBold(AddLabel(" | ", infoWrapPanel));

            if (rbCurrDoc)
                AddLabel("CurDocum", infoWrapPanel);
            else if (rbOpenDocs)
                AddLabel("Opened", infoWrapPanel);
            else if (rbProject)
                AddLabel("Project", infoWrapPanel);
            else if (rbSolution)
                AddLabel("Solution", infoWrapPanel);

            AddExtraBold(AddLabel(" | ", infoWrapPanel));

            AddLabel("`" + tbPhrase + "`", infoWrapPanel);
        }

        public int GetVsFindOptions()
        {
            int result = (Byte)vsFindOptions.vsFindOptionsNone;
            if (chkWholeWord)
                result = result | (Byte)vsFindOptions.vsFindOptionsMatchWholeWord;
            if (chkCase)
                result = result | (Byte)vsFindOptions.vsFindOptionsMatchCase;
            if (chkRegExp)
                result = result | (Byte)vsFindOptions.vsFindOptionsRegularExpression;
            return result;
        }

        public void FillFindData(FindData findData)
        {


            if (chkWholeWord)
                findData.FindOptions |= FindOptions.WholeWord;


            if (chkCase)
                findData.FindOptions |= FindOptions.MatchCase;


            if (chkRegExp)
                findData.FindOptions |= FindOptions.UseRegularExpressions;
        }
    }
}
