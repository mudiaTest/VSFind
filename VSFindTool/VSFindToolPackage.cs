//------------------------------------------------------------------------------
// <copyright file="VSFindToolPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VSFindTool
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(VSFindToolPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(VSFindToolMainForm))]
    public sealed class VSFindToolPackage : Package
    {
        /// <summary>
        /// VSFindToolPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "c030f256-ba43-4c27-94a8-5adecafb0350";

        /// <summary>
        /// Initializes a new instance of the <see cref="VSFindToolPackage"/> class.
        /// </summary>
        public VSFindToolPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        internal EnvDTE80.DTE2 dte2;
        internal IVsTextManager textManager;
        private DteInitializer dteInitializer;
        public EnvDTE.Window LastDocWindow = null;
        public void m_WindowActivatedEvent(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
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

        public void m_WindowClosingEvent(EnvDTE.Window Window)
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
                m_WindowEvents.WindowActivated += new EnvDTE._dispWindowEvents_WindowActivatedEventHandler(m_WindowActivatedEvent);
                m_WindowEvents.WindowClosing += new EnvDTE._dispWindowEvents_WindowClosingEventHandler(m_WindowClosingEvent);
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
            VSFindToolMainFormCommand.Initialize(this);
			AddWindowEvent();
        }

        #endregion

        private void InitializeDTE()
        {
            IVsShell shellService;

            this.textManager = (IVsTextManager)GetService(typeof(SVsTextManager));

            this.dte2 = this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;

            if (this.dte2 == null) // The IDE is not yet fully initialized
            {
                shellService = this.GetService(typeof(SVsShell)) as IVsShell;
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
