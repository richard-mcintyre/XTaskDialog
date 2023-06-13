using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XTaskDialog
{
    public class ProgressDialog
    {
        #region TaskDialogInstance

        class TaskDialogInstance : ITaskDialogInstance
        {
            #region P/Invoke

            [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage")]
            static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage")]
            static extern IntPtr SendMessage2(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);

            private const uint WM_USER = 0x0400;

            #endregion

            #region Messages

            enum Messages : uint
            {
                TDM_NAVIGATE_PAGE = WM_USER + 101,
                TDM_CLICK_BUTTON = WM_USER + 102, // wParam = Button ID
                TDM_SET_MARQUEE_PROGRESS_BAR = WM_USER + 103, // wParam = 0 (nonMarque) wParam != 0 (Marquee)
                TDM_SET_PROGRESS_BAR_STATE = WM_USER + 104, // wParam = new progress state
                TDM_SET_PROGRESS_BAR_RANGE = WM_USER + 105, // lParam = MAKELPARAM(nMinRange, nMaxRange)
                TDM_SET_PROGRESS_BAR_POS = WM_USER + 106, // wParam = new position
                TDM_SET_PROGRESS_BAR_MARQUEE = WM_USER + 107, // wParam = 0 (stop marquee), wParam != 0 (start marquee), lparam = speed (milliseconds between repaints)
                TDM_SET_ELEMENT_TEXT = WM_USER + 108, // wParam = element (TASKDIALOG_ELEMENTS), lParam = new element text (LPCWSTR)
                TDM_CLICK_RADIO_BUTTON = WM_USER + 110, // wParam = Radio Button ID
                TDM_ENABLE_BUTTON = WM_USER + 111, // lParam = 0 (disable), lParam != 0 (enable), wParam = Button ID
                TDM_ENABLE_RADIO_BUTTON = WM_USER + 112, // lParam = 0 (disable), lParam != 0 (enable), wParam = Radio Button ID
                TDM_CLICK_VERIFICATION = WM_USER + 113, // wParam = 0 (unchecked), 1 (checked), lParam = 1 (set key focus)
                TDM_UPDATE_ELEMENT_TEXT = WM_USER + 114, // wParam = element (TASKDIALOG_ELEMENTS), lParam = new element text (LPCWSTR)
                TDM_SET_BUTTON_ELEVATION_REQUIRED_STATE = WM_USER + 115, // wParam = Button ID, lParam = 0 (elevation not required), lParam != 0 (elevation required)
                TDM_UPDATE_ICON = WM_USER + 116  // wParam = icon element (TASKDIALOG_ICON_ELEMENTS), lParam = new icon (hIcon if TDF_USE_HICON_* was set, PCWSTR otherwise)
            }

            #endregion

            #region Construction

            public TaskDialogInstance(IntPtr hwnd)
            {
                _hwnd = hwnd;
            }

            #endregion

            #region Fields

            private readonly IntPtr _hwnd;

            #endregion

            #region Properties

            public IntPtr HWnd => _hwnd;

            #endregion

            #region Methods

            public void StartMarquee() =>
                SendMessage(_hwnd, (uint)Messages.TDM_SET_MARQUEE_PROGRESS_BAR, -1, 0);

            public void StopMarquee() =>
                SendMessage(_hwnd, (uint)Messages.TDM_SET_MARQUEE_PROGRESS_BAR, 0, 0);

            public void SetProgressBarRange(int minRange, int maxRange)
            {
                int range = maxRange;
                range <<= 16;
                range |= minRange;

                SendMessage(_hwnd, (uint)Messages.TDM_SET_PROGRESS_BAR_RANGE, 0, range);
            }

            public void SetProgressBarPosition(int position) =>
                SendMessage(_hwnd, (uint)Messages.TDM_SET_PROGRESS_BAR_POS, position, 0);

            public void SetProgressBarState(TaskDialogProgessBarState state) =>
                SendMessage(_hwnd, (uint)Messages.TDM_SET_PROGRESS_BAR_STATE, (int)state, 0);

            public void UpdateElementText(TaskDialogElements element, string text)
            {
                IntPtr str = IntPtr.Zero;

                try
                {
                    str = Marshal.StringToHGlobalUni(text);
                    SendMessage2(_hwnd, (uint)Messages.TDM_SET_ELEMENT_TEXT, (int)element, str);
                }
                finally
                {
                    if (str != IntPtr.Zero)
                        Marshal.FreeHGlobal(str);
                }
            }

            public void ClickButton(TaskDialogResult button) =>
                SendMessage(_hwnd, (uint)Messages.TDM_CLICK_BUTTON, (int)button, 0);

            #endregion
        }

        #endregion

        #region CallbackData

        private record CallbackData(Task Task, CancellationTokenSource? CancellationTokenSource, ProgressDialogProgressInfo? Progress);

        #endregion  

        #region Construction

        public ProgressDialog(IntPtr hWndParent, string mainInstruction, string content)
        {
            _hWndParent = hWndParent;
            _mainInstruction = mainInstruction;
            _content = content;
        }

        #endregion

        #region Fields

        private readonly IntPtr _hWndParent;
        private readonly string _mainInstruction;
        private readonly string _content;

        #endregion

        #region Properties

        public IntPtr HwndParent => _hWndParent;

        public string MainInstruction => _mainInstruction;

        public string Content => _content;

        public string? WindowTitle { get; set; }

        public bool EnableHyperlinks { get; set; }

        public bool IsMarqueeProgressBar { get; set; } = true;

        #endregion

        #region Methods

        public void Show(Task task, CancellationTokenSource? cancellation, ProgressDialogProgressInfo? progress)
        {
            if (task.IsCompleted)
            {
                ThrowIfTaskFaulted(task);
                return;
            }

            InternalShow(task, cancellation, progress);
        }

        public T Show<T>(Task<T> task, CancellationTokenSource? cancellation, ProgressDialogProgressInfo? progress)
        {
            try
            {
                if (task.IsCompleted)
                    return task.Result;

                // If the task is about to complete, then we dont want to display the progress dialog only to immediately close it
                task.Wait(200);
                if (task.IsCompleted)
                    return task.Result;

                InternalShow(task, cancellation, progress);

                return task.GetAwaiter().GetResult();
            }
            catch (AggregateException e)
            {
                if (task.IsFaulted)
                    return task.GetAwaiter().GetResult();

                throw e.InnerException!;
            }
            catch
            {
                if (task.IsFaulted)
                    return task.GetAwaiter().GetResult();

                throw;
            }
        }

        private static void ThrowIfTaskFaulted(Task task)
        {
            if (task.IsFaulted)
            {
                // Using ExceptionDispatchInfo so the original stack trace is used when we rethrow the exception
                Exception? exception = null;
                if (task.Exception?.InnerException is not null)
                {
                    exception = task.Exception.InnerException;
                }
                else
                {
                    exception = task.Exception;
                }

                ExceptionDispatchInfo.Capture(exception!).Throw();
            }
        }

        private void InternalShow(Task task, CancellationTokenSource? cts, ProgressDialogProgressInfo? progress = null)
        {
            Native.TaskDialogConfig config = new Native.TaskDialogConfig();
            config.cbSize = (uint)Marshal.SizeOf(config);
            config.hwndParent = _hWndParent;
            config.pszWindowTitle = this.WindowTitle ?? GetWindowTitle(_hWndParent);
            config.pszMainInstruction = _mainInstruction;
            config.pszContent = _content;
            config.dwCommonButtons = TaskDialogButton.Cancel;
            config.dwFlags |= TaskDialogFlags.CallbackTimer;
            config.dwFlags |= this.IsMarqueeProgressBar ? TaskDialogFlags.ShowMarqueeProgressBar : TaskDialogFlags.ShowProgressBar;

            if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
                config.dwFlags |= TaskDialogFlags.RtlLayout;

            if (this.EnableHyperlinks)
                config.dwFlags |= TaskDialogFlags.EnableHyperlinks;

            CallbackData callbackData = new CallbackData(task, cts, progress);

            config.pfCallback = new Native.NativeTaskDialogCallback((hwnd, notification, wparam, lparam, data) =>
            {
                return OnCallback(new TaskDialogInstance(hwnd), (TaskDialogNotificationKind)notification, wparam, lparam, callbackData);
            });

            using (new EnableThemingInScope(true))
            {
                Native.TaskDialogIndirect(ref config, out _, out _, out _);
            }

            if (task.IsCanceled)
                throw new OperationCanceledException(cts?.Token ?? default);

            ThrowIfTaskFaulted(task);
        }

        private int OnCallback(ITaskDialogInstance dlg, TaskDialogNotificationKind notification, nint wParam, nint lParam, CallbackData data)
        {
            if (data.Progress != null)
            {
                if (data.Progress.MainInstruction is not null)
                    dlg.UpdateElementText(TaskDialogElements.MainInstruction, data.Progress.MainInstruction);

                if (data.Progress.Content is not null)
                    dlg.UpdateElementText(TaskDialogElements.Content, data.Progress.Content);

                if (data.Progress.ProgressBar is not null)
                {
                    dlg.SetProgressBarRange(data.Progress.ProgressBar.MinRange, data.Progress.ProgressBar.MaxRange);
                    dlg.SetProgressBarPosition(data.Progress.ProgressBar.Position);
                }
            }

            switch (notification)
            {
                case TaskDialogNotificationKind.Created:
                    {
                        if (this.IsMarqueeProgressBar)
                            dlg.StartMarquee();
                    }
                    break;


                case TaskDialogNotificationKind.ButtonClicked:
                    {
                        TaskDialogResult buttonClicked = (TaskDialogResult)wParam;
                        if (buttonClicked == TaskDialogResult.Cancel)
                        {
                            if (data.Task.IsCanceled)
                            {
                                return Native.S_OK;
                            }
                            else
                            {
                                if (data.CancellationTokenSource is not null)
                                    data.CancellationTokenSource.Cancel();

                                return Native.S_FALSE;   // Dont close until the background function has cancelled
                            }
                        }
                    }
                    break;

                case TaskDialogNotificationKind.Timer:
                    {
                        if (data.Task != null && data.Task.IsCompleted)
                        {
                            if (data.Task.IsCanceled)
                                dlg.ClickButton(TaskDialogResult.Cancel);
                            else // The task may have thrown an exception
                                dlg.ClickButton(TaskDialogResult.OK);
                        }
                        else if (this.IsMarqueeProgressBar)
                        {
                            dlg.SetProgressBarPosition(0);
                        }                        
                    }
                    break;

                case TaskDialogNotificationKind.HyperlinkClicked:
                    {
                        string hyperlink = Marshal.PtrToStringAuto(lParam)!;
                        this.HyperlinkClicked?.Invoke(this, new HyperlinkClickedArgs(hyperlink));
                    }
                    break;
            }

            return Native.S_OK;
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                int length = Native.GetWindowTextLength(hWnd);
                StringBuilder sb = new StringBuilder(length + 1);
                Native.GetWindowText(hWnd, sb, sb.Capacity);

                return sb.ToString();
            }

            return String.Empty;
        }

        #endregion

        #region Events

        public event EventHandler<HyperlinkClickedArgs>? HyperlinkClicked;

        #endregion
    }
}
