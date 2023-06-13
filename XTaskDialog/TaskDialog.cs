using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XTaskDialog
{
    public class TaskDialog
    {
        #region Construction

        public TaskDialog(IntPtr hWndParent, string mainInstruction, string content, TaskDialogButton buttons = TaskDialogButton.OK)
        {
            _hWndParent = hWndParent;
            _mainInstruction = mainInstruction;
            _content = content;
            this.Buttons = buttons;
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

        public TaskDialogButton Buttons { get; set; } = TaskDialogButton.OK;

        public IEnumerable<TaskDialogCustomButton> CustomButtons { get; set; } = Enumerable.Empty<TaskDialogCustomButton>();

        public IEnumerable<TaskDialogRadioButton> RadioButtons { get; set; } = Enumerable.Empty<TaskDialogRadioButton>();

        public TaskDialogButton DefaultButton { get; set; }

        public TaskDialogIcon Icon { get; set; } = TaskDialogIcon.None;

        public string? VerificationText { get; set; }

        public bool IsVerificationChecked { get; set; }

        public string? FooterText { get; set; }

        public TaskDialogIcon FooterIcon { get; set; }

        public string? ExpandedInformation { get; set; }

        public string? ExpandedText { get; set; }

        public string? CollapsedText { get; set; }

        public bool EnableHyperlinks { get; set; }

        public bool AllowDialogCancellation { get; set; }

        public bool ExpandFooterArea { get; set; }

        public bool ExpandedByDefault { get; set; }

        public bool PositionRelativeToWindow { get; set; }

        public bool UseCommandLinks { get; set; }

        public bool UseCommandLinksNoIcon { get; set; }

        #endregion

        #region Methods

        public static TaskDialogResult Show(IntPtr hWndParent, string mainInstruction, string content, TaskDialogButton buttons = TaskDialogButton.OK)
        {
            TaskDialog dlg = new TaskDialog(hWndParent, mainInstruction, content, buttons);
            return dlg.Show(out _, out _);
        }

        public TaskDialogResult Show() => Show(out _, out _);

        public TaskDialogResult Show(out int radioButtonSelected) => Show(out radioButtonSelected, out _);

        public TaskDialogResult Show(out bool verificationFlagChecked) => Show(out _, out verificationFlagChecked);

        public TaskDialogResult Show(out int radioButtonSelected, out bool verificationFlagChecked)
        {
            Native.TaskDialogConfig config = new Native.TaskDialogConfig();

            try
            {
                config.cbSize = (uint)Marshal.SizeOf(config);
                config.hwndParent = _hWndParent;
                config.pszWindowTitle = this.WindowTitle ?? GetWindowTitle(_hWndParent);
                config.pszMainInstruction = _mainInstruction;
                config.pszContent = _content;
                config.dwCommonButtons = this.Buttons;
                config.nDefaultButton = (int)this.DefaultButton;

                if (this.Icon != TaskDialogIcon.None)
                    config.hMainIcon = new Native.TASKDIALOGCONFIG_ICON_UNION((int)this.Icon);

                if (String.IsNullOrWhiteSpace(this.VerificationText) == false)
                {
                    config.pszVerificationText = this.VerificationText;
                    if (this.IsVerificationChecked)
                        config.dwFlags |= TaskDialogFlags.VerificationFlagChecked;
                }

                if (String.IsNullOrWhiteSpace(this.FooterText) == false)
                {
                    config.pszFooter = this.FooterText;
                    if (this.FooterIcon != TaskDialogIcon.None)
                        config.hFooterIcon = new Native.TASKDIALOGCONFIG_ICON_UNION((int)this.FooterIcon);
                }

                if (String.IsNullOrWhiteSpace(this.ExpandedInformation) == false)
                {
                    config.pszExpandedInformation = this.ExpandedInformation;

                    if (String.IsNullOrWhiteSpace(this.ExpandedText) == false)
                        config.pszExpandedControlText = this.ExpandedText;

                    if (String.IsNullOrWhiteSpace(this.CollapsedText) == false)
                        config.pszCollapsedControlText = this.CollapsedText;
                }

                if (this.CustomButtons != null && this.CustomButtons.Any())
                {
                    IntPtr initialPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Native.TASKDIALOG_BUTTON)) * this.CustomButtons.Count());
                    IntPtr currentPtr = initialPtr;
                    foreach (TaskDialogCustomButton btn in this.CustomButtons)
                    {
                        Native.TASKDIALOG_BUTTON nativeCustomButton = new Native.TASKDIALOG_BUTTON()
                        {
                            nButtonID = btn.ButtonId,
                            pszButtonText = btn.Caption
                        };

                        Marshal.StructureToPtr(nativeCustomButton, currentPtr, false);
                        currentPtr += Marshal.SizeOf(nativeCustomButton);
                    }

                    config.cButtons = (uint)this.CustomButtons.Count();
                    config.pButtons = initialPtr;
                }

                if (this.RadioButtons != null && this.RadioButtons.Any())
                {
                    IntPtr initialPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Native.TASKDIALOG_BUTTON)) * this.RadioButtons.Count());
                    IntPtr currentPtr = initialPtr;
                    foreach (TaskDialogRadioButton btn in this.RadioButtons)
                    {
                        Native.TASKDIALOG_BUTTON nativeRadioButton = new Native.TASKDIALOG_BUTTON()
                        {
                            nButtonID = btn.ButtonId,
                            pszButtonText = btn.Caption
                        };

                        Marshal.StructureToPtr(nativeRadioButton, currentPtr, false);
                        currentPtr += Marshal.SizeOf(nativeRadioButton);
                    }

                    config.cRadioButtons = (uint)this.RadioButtons.Count();
                    config.pRadioButtons = initialPtr;
                }

                if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
                    config.dwFlags |= TaskDialogFlags.RtlLayout;

                if (this.EnableHyperlinks)
                    config.dwFlags |= TaskDialogFlags.EnableHyperlinks;

                if (this.AllowDialogCancellation)
                    config.dwFlags |= TaskDialogFlags.AllowDialogCancellation;

                if (this.ExpandFooterArea)
                    config.dwFlags |= TaskDialogFlags.ExpandFooterArea;

                if (this.ExpandedByDefault)
                    config.dwFlags |= TaskDialogFlags.ExpandedByDefault;

                if (this.PositionRelativeToWindow)
                    config.dwFlags |= TaskDialogFlags.PositionRelativeToWindow;

                if (this.UseCommandLinks)
                    config.dwFlags |= TaskDialogFlags.UseCommandLinks;

                if (this.UseCommandLinksNoIcon)
                    config.dwFlags |= TaskDialogFlags.UseCommandLinksNoIcon;

                config.pfCallback = new Native.NativeTaskDialogCallback((hwnd, notification, wparam, lparam, data) =>
                    OnCallback(hwnd, (TaskDialogNotificationKind)notification, wparam, lparam));

                using (new EnableThemingInScope(true))
                {
                    Native.TaskDialogIndirect(ref config, out int buttonPressed, out radioButtonSelected, out verificationFlagChecked);
                    return (TaskDialogResult)buttonPressed;
                }
            }
            finally
            {
                if (config.pButtons != IntPtr.Zero)
                    Marshal.FreeHGlobal(config.pButtons);

                if (config.pRadioButtons != IntPtr.Zero)
                    Marshal.FreeHGlobal(config.pRadioButtons);
            }
        }

        protected int OnCallback(IntPtr hWnd, TaskDialogNotificationKind notification, nint wParam, nint lParam)
        {
            switch (notification)
            {
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
