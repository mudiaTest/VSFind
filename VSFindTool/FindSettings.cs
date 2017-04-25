﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using System.Windows.Controls;
using System.Windows;

namespace VSFindTool
{
    class FindSettings
    {
        internal string tbPhrase;
        internal bool chkWholeWord;
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

            if(chkWholeWord)
                result += "W ";
            else
                result += "w ";

            if (chkCase)
                result += "C ";
            else
                result += "c ";

            if (chkRegExp)
                result += "R ";
            else
                result += "r ";

            if (rbCurrDoc)
                result +=  " [CurDocum] ";
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
            FindSettings result = new FindSettings();
            result.tbPhrase = tbPhrase;
            result.chkWholeWord = chkWholeWord;
            result.chkCase = chkCase;
            result.chkRegExp = chkRegExp;
            result.rbCurrDoc = rbCurrDoc;
            result.rbOpenDocs = rbOpenDocs;
            result.rbProject = rbProject;
            result.rbSolution = rbSolution;
            result.action = action;
            result.location = location;
            return result;
        }

        public void SetColtrols(VSFindToolMainFormControl form)
        {
            //Search phrase
            form.tbPhrase.Text = tbPhrase;

            //Find options
            form.chkWholeWord.IsChecked = chkWholeWord;
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
    }
}
