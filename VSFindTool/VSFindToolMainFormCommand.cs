//------------------------------------------------------------------------------
// <copyright file="VSFindToolMainFormCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VSFindTool
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class VSFindToolMainFormCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;
        public const int VSFindToolGetFocus = 0x0101;
        public const int VSFindToolShowResults = 0x0102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("bd0f02b8-4e7f-466e-87d7-c970f5981465");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSFindToolMainFormCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
         
        internal EnvDTE80.DTE2 dte2;
        internal IVsTextManager textManager;

        private VSFindToolMainFormCommand(Package package)
        {
            this.package = package ?? throw new ArgumentNullException("package");
            
            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandID);
                commandService.AddCommand(menuItem);
            }
            
            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandServiceFocus)
            {
                var menuCommandID = new CommandID(CommandSet, VSFindToolGetFocus);
                var menuItem = new MenuCommand(this.FocusToolWindow, menuCommandID);
                commandServiceFocus.AddCommand(menuItem);
            }
            
            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandServiceResults)
            {
                var menuCommandID = new CommandID(CommandSet, VSFindToolShowResults);
                var menuItem = new MenuCommand(this.ResultToolWindow, menuCommandID);
                commandServiceResults.AddCommand(menuItem);
            }

        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static VSFindToolMainFormCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new VSFindToolMainFormCommand(package)
            {
                dte2 = ((VSFindToolPackage)package).dte2,
                textManager = ((VSFindToolPackage)package).textManager
            };
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(VSFindToolMainForm), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());            
        }

        private void FocusToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(VSFindToolMainForm), 0, true);            
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            ((VSFindToolMainFormControl)((VSFindToolMainForm)window).Content).DoFocus();
        }

        private void ResultToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(VSFindToolMainForm), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            ((VSFindToolMainFormControl)((VSFindToolMainForm)window).Content).ShowResults();
        }
    }
}
