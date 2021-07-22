﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using HWND = System.IntPtr;

namespace CustomizacaoMoradias.Forms
{
    /// <summary>
    /// Wrapper class for window handles
    /// </summary>
    public class WindowHandleSearch
      : IWin32Window, System.Windows.Forms.IWin32Window
    {
        #region Static methods
        /// <summary>
        /// Revit main window handle
        /// </summary>
        static public WindowHandleSearch MainWindowHandle
        {
            get
            {
                // Get handle of main window
                var revitProcess = Process.GetCurrentProcess();
                return new WindowHandleSearch(
                  GetMainWindow(revitProcess.Id));
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor - From WinForms window handle
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        public WindowHandleSearch(IntPtr hwnd)
        {
            // Assert valid window handle
            Debug.Assert(IntPtr.Zero != hwnd,
              "Null window handle");

            Handle = hwnd;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Window handle
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Set this window handle as the owner 
        /// of the given window
        /// </summary>
        /// <param name="childWindow">Child window 
        /// whose parent will be set to be this window 
        /// handle</param>
        public void SetAsOwner(Window childWindow)
        {
            new WindowInteropHelper(childWindow) { Owner = Handle };
        }

        // User32.dll calls used to get the Main Window for a Process Id (PID)
        private delegate bool EnumWindowsProc(
          HWND hWnd, int lParam);

        [DllImport("user32.DLL")]
        private static extern bool EnumWindows(
          EnumWindowsProc enumFunc, int lParam);

        [DllImport("user32.dll", ExactSpelling = true,
          CharSet = CharSet.Auto)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(
          IntPtr hWnd, out uint processId);

        [DllImport("user32.DLL")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.DLL")]
        private static extern bool IsWindowVisible(HWND hWnd);

        [DllImport("user32.DLL")]
        private static extern int GetWindowTextLength(HWND hWnd);

        [DllImport("user32.DLL")]
        private static extern int GetWindowText(HWND hWnd,
          StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Returns the main Window Handle for the 
        /// Process Id (pid) passed in.
        /// IF the Main Window is not found then a 
        /// handle value of Zreo is returned, no handle.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static IntPtr GetMainWindow(int pid)
        {
            HWND shellWindow = GetShellWindow();
            List<HWND> windowsForPid = new List<IntPtr>();

            try
            {
                EnumWindows(
                // EnumWindowsProc Function, does the work 
                // on each window.
                delegate (HWND hWnd, int lParam)
                {
                    if (hWnd == shellWindow) return true;
                    if (!IsWindowVisible(hWnd)) return true;

                    uint windowPid = 0;
                    GetWindowThreadProcessId(hWnd, out windowPid);

                    // if window is from Pid of interest, 
                    // see if it's the Main Window
                    if (windowPid == pid)
                    {
                        // By default Main Window has a 
                        // Parent Window of Zero, no parent.
                        HWND parentHwnd = GetParent(hWnd);
                        if (parentHwnd == IntPtr.Zero)
                            windowsForPid.Add(hWnd);
                    }

                    return true;
                }
                // lParam, nothing, null...
                , 0);
            }
            catch (Exception) { }

            return DetermineMainWindow(windowsForPid);
        }

        /// <summary>
        /// Finds Revit's Main Window from the list of 
        /// window handles passed in.
        /// If the Main Window for Revit is not found 
        /// then a Null (IntPtr.Zero) handle is returnd.
        /// </summary>
        /// <param name="handles"></param>
        /// <returns></returns>
        private static IntPtr DetermineMainWindow(
          List<HWND> handles)
        {
            // Safty conditions, bail if not met.
            if (handles == null || handles.Count <= 0)
                return IntPtr.Zero;

            // default Null handel
            HWND mainWindow = IntPtr.Zero;

            // only one window so return it, 
            // must be the Main Window??
            if (handles.Count == 1)
            {
                mainWindow = handles[0];
            }
            // more than one window
            else
            {
                // more than one candidate for Main Window 
                // so find the Main Window by its Title, it 
                // will contain "Autodesk Revit"
                foreach (var hWnd in handles)
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length == 0) continue;

                    StringBuilder builder = new StringBuilder(
                      length);

                    GetWindowText(hWnd, builder, length + 1);

                    // Depending on the Title of the Main Window 
                    // to have "Autodesk Revit" in it.
                    if (builder.ToString().ToLower().Contains(
                      "autodesk revit"))
                    {
                        mainWindow = hWnd;
                        break; // found Main Window stop and return it.
                    }
                }
            }
            return mainWindow;
        }
        #endregion
    }
}
