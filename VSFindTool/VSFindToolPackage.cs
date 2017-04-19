//------------------------------------------------------------------------------
// <copyright file="VSFindToolPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

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
        public const string PackageGuidString = "12f7a4f9-c3ad-468f-8c37-c35b41d4ed2f";

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
            EnvDTE.DTE dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            if (dte != null)
            {
                var m_WindowEvents = dte.Events.WindowEvents;
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
            VSFindToolMainFormCommand.Initialize(this);
			AddWindowEvent();
        }

        #endregion
    }
}
