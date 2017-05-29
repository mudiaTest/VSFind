using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Utilities;//Install-Package Microsoft.VisualStudio.Utilities - from paket manager console

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ComponentModelHost;
//using Microsoft.VisualStudio.Tools.Office.Runtime.Interop;

namespace VSFindTool
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(VSFindToolMainForm))]
    [Guid(GuidList.guidVSFindToolPkgString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class VSFindToolPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VSFindToolPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));        	
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(VSFindToolMainForm), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        internal EnvDTE80.DTE2 dte2;
        internal IComponentModel componentModel;
        internal IVsTextManager textManager;
        private DteInitializer dteInitializer;
        internal EnvDTE.Window LastDocWindow = null;
        public void M_WindowActivatedEvent(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
        {
            EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));

            foreach (EnvDTE.Document doc in dte.Documents)
            {
                foreach (EnvDTE.Window docWindow in doc.Windows)
                {
                    if (docWindow == GotFocus)
                    {
                        LastDocWindow = GotFocus;
                        return;
                    }

                }
            }
        }

        public void M_WindowClosingEvent(EnvDTE.Window Window)
        {
            if (LastDocWindow == Window)
              LastDocWindow = null;
        }

        private void AddWindowEvent()
        {
            //EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            if (dte2 != null)
            {
                var m_WindowEvents = dte2.Events.WindowEvents;
                m_WindowEvents.WindowActivated += new EnvDTE._dispWindowEvents_WindowActivatedEventHandler(M_WindowActivatedEvent);
                m_WindowEvents.WindowClosing += new EnvDTE._dispWindowEvents_WindowClosingEventHandler(M_WindowClosingEvent);
            }
        }
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            InitializeDTE();
            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidVSFindToolCmdSet, (int)PkgCmdIDList.cmdidMyTool);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand( menuToolWin );
            }
			AddWindowEvent();
        }
        #endregion

        private void InitializeDTE()
        {
            IVsShell shellService;            

            this.textManager = (IVsTextManager)GetService(typeof(SVsTextManager));

            this.dte2 = this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;

            if (componentModel == null)
            {
                componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            }

            if (this.dte2 == null) // The IDE is not yet fully initialized
            {
                shellService = GetService(typeof(SVsShell)) as IVsShell;
                
                this.dteInitializer = new DteInitializer(shellService, this.InitializeDTE);
            }
            else
            {
                this.dteInitializer = null;
            }
        }
    }



    internal class DteInitializer : IVsShellPropertyEvents
    {
        private IVsShell shellService;
        private uint cookie;
        private Action callback;

        internal DteInitializer(IVsShell shellService, Action callback)
        {
            int hr;

            this.shellService = shellService;
            this.callback = callback;

            // Set an event handler to detect when the IDE is fully initialized
            hr = this.shellService.AdviseShellPropertyChanges(this, out this.cookie);

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
        }

        int IVsShellPropertyEvents.OnShellPropertyChange(int propid, object var)
        {
            int hr;
            bool isZombie;

            if (propid == (int)__VSSPROPID.VSSPROPID_Zombie)
            {
                isZombie = (bool)var;

                if (!isZombie)
                {
                    // Release the event handler to detect when the IDE is fully initialized
                    hr = this.shellService.UnadviseShellPropertyChanges(this.cookie);

                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);

                    this.cookie = 0;

                    this.callback();
                }
            }
            return VSConstants.S_OK;
        }
    }
}
