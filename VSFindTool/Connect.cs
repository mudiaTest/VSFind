using System;
using Extensibility; /*Wymaga Reference: Extensions->extensibility*/
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;

namespace VSHierarchyAddin
{
    public class Connect : IDTExtensibility2
    {
        private const int S_OK = 0;

        private DTE2 _applicationObject;
        private AddIn _addInInstance;

        public Dictionary<int, EnvDTE.Project> projects = new Dictionary<int, EnvDTE.Project>();
        public Dictionary<EnvDTE.Project, IVsHierarchy> hierarchy = new Dictionary<EnvDTE.Project, IVsHierarchy>();
        //public Dictionary<EnvDTE.ProjectItem, IVsHierarchy> projectItes = new Dictionary<EnvDTE.ProjectItem, IVsHierarchy>();

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = null;// (AddIn)addInInst;

            switch (connectMode)
            {
                case ext_ConnectMode.ext_cm_Startup:

                    // Do nothing; OnStartupComplete will be called
                    break;

                case ext_ConnectMode.ext_cm_AfterStartup:

                    InitializeAddIn();
                    break;
            }
        }

        public void Start(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            switch (connectMode)
            {
                case ext_ConnectMode.ext_cm_Startup:

                    // Do nothing; OnStartupComplete will be called
                    break;

                case ext_ConnectMode.ext_cm_AfterStartup:

                    InitializeAddIn();
                    break;
            }
        }

        public void OnStartupComplete(ref Array custom)
        {
            InitializeAddIn();
        }

        private void InitializeAddIn()
        {
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;
            IVsSolution solutionService;

            try
            {
                serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_applicationObject;

                solutionService = (IVsSolution)GetService(serviceProvider, typeof(SVsSolution), typeof(IVsSolution));

                foreach (EnvDTE.Project project in _applicationObject.Solution.Projects)
                {
                    projects.Add(projects.Count, project);
                    ProcessProject(solutionService, project);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public IVsHierarchy ProcessProject(IVsSolution solutionService, EnvDTE.Project project)
        {
            IVsHierarchy projectHierarchy = null;

            if (solutionService.GetProjectOfUniqueName(project.UniqueName, out projectHierarchy) == S_OK)
            {
                if (projectHierarchy != null)
                {
                    hierarchy.Add(project, projectHierarchy);
                    ProcessProjectItems(solutionService, projectHierarchy, project.ProjectItems);
                }
            }
            return projectHierarchy;
        }

        private void ProcessProjectItems(IVsSolution solutionService, IVsHierarchy projectHierarchy, EnvDTE.ProjectItems projectItems)
        {
            if (projectItems != null)
            {
                foreach (EnvDTE.ProjectItem projectItem in projectItems)
                {
                    if (projectItem.SubProject != null)
                    {
                        ProcessProject(solutionService, projectItem.SubProject);
                    }
                    else
                    {
                        ProcessProjectItem(projectHierarchy, projectItem);

                        // Enter in recursion
                        ProcessProjectItems(solutionService, projectHierarchy, projectItem.ProjectItems);
                    }
                }
            }
        }

        private void ProcessProjectItem(IVsHierarchy projectHierarchy, EnvDTE.ProjectItem projectItem)
        {
            string fileFullName = null;
            uint itemId;

            try
            {
                fileFullName = projectItem.get_FileNames(0);
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(fileFullName))
            {
                if (projectHierarchy.ParseCanonicalName(fileFullName, out itemId) == S_OK)
                {
                    MessageBox.Show("File: " + fileFullName + "\r\n" + "Item Id: 0x" + itemId.ToString("X"));
                }
            }
        }

        private object GetService(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider,
           System.Type serviceType, System.Type interfaceType)
        {
            object service = null;
            IntPtr servicePointer;
            int hr = 0;
            Guid serviceGuid;
            Guid interfaceGuid;

            serviceGuid = serviceType.GUID;
            interfaceGuid = interfaceType.GUID;

            hr = serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid, out servicePointer);
            if (hr != S_OK)
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            }
            else if (servicePointer != IntPtr.Zero)
            {
                service = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(servicePointer);
                System.Runtime.InteropServices.Marshal.Release(servicePointer);
            }
            return service;
        }

        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }

    }
}