using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XTaskDialog
{
    internal class Native
    {
        public const int S_FALSE = 0x0001;
        public const int S_OK = 0x0000;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, EntryPoint = "TaskDialog")]
        public static extern int TaskDialog(IntPtr hWndParent, IntPtr hInstance, string pszWindowTitle, string pszMainInstruction, string pszContent, int dwCommonButtons, IntPtr pszIcon, out int pnButton);

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        public static extern IntPtr TaskDialogIndirect
           ([In] ref TaskDialogConfig pTaskConfig,
           [Out, Optional] out int pnButton,
           [Out, Optional] out int pnRadioButton,
           [Out, Optional] out bool pfVerificationFlagChecked);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct TaskDialogConfig
        {
            public uint cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public TaskDialogFlags dwFlags;
            public TaskDialogButton dwCommonButtons;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszWindowTitle;
            public TASKDIALOGCONFIG_ICON_UNION hMainIcon;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMainInstruction;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszContent;
            public uint cButtons;
            public IntPtr pButtons;
            public int nDefaultButton;
            public uint cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszVerificationText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedInformation;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedControlText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCollapsedControlText;
            public TASKDIALOGCONFIG_ICON_UNION hFooterIcon;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszFooter;
            public NativeTaskDialogCallback pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
        public struct TASKDIALOGCONFIG_ICON_UNION
        {
            internal TASKDIALOGCONFIG_ICON_UNION(int i)
            {
                spacer = IntPtr.Zero;
                pszIcon = 0;
                hMainIcon = i;
            }

            [FieldOffset(0)]
            internal int hMainIcon;
            [FieldOffset(0)]
            internal int pszIcon;
            [FieldOffset(0)]
            internal IntPtr spacer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public struct TASKDIALOG_BUTTON
        {
            public int nButtonID;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszButtonText;
        }

        public delegate int NativeTaskDialogCallback(IntPtr hwnd, uint uNotification, IntPtr wParam, IntPtr lParam, IntPtr dwRefData);
    }

}
