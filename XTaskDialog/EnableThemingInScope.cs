using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace XTaskDialog
{
    /// <devdoc>
    /// This class is intended to use with the C# 'using' statement in
    /// to activate an activation context for turning on visual theming at
    /// the beginning of a scope, and have it automatically deactivated
    /// when the scope is exited.
    /// </devdoc>
    /// <remarks>Based on https://www.betaarchive.com/wiki/index.php?title=Microsoft_KB_Archive/830033</remarks>
    internal class EnableThemingInScope : IDisposable
    {
        #region PInvoke

        // All the pinvoke goo...
        [DllImport("Kernel32.dll")]
        private extern static IntPtr CreateActCtx(ref ACTCTX actctx);
        [DllImport("Kernel32.dll")]
        private extern static bool ActivateActCtx(IntPtr hActCtx, out IntPtr lpCookie);
        [DllImport("Kernel32.dll")]
        private extern static bool DeactivateActCtx(uint dwFlags, IntPtr lpCookie);

        private const int ACTCTX_FLAG_ASSEMBLY_DIRECTORY_VALID = 0x004;

        private struct ACTCTX
        {
            public int cbSize;
            public uint dwFlags;
            public string lpSource;
            public ushort wProcessorArchitecture;
            public ushort wLangId;
            public string lpAssemblyDirectory;
            public string lpResourceName;
            public string lpApplicationName;
        }

        #endregion

        #region Construction

        public EnableThemingInScope(bool enable)
        {
            cookie = IntPtr.Zero;
            if (enable/* && OSFeature.Feature.IsPresent(OSFeature.Themes)*/)
            {
                if (EnsureActivateContextCreated())
                {
                    if (!ActivateActCtx(hActCtx, out cookie))
                    {
                        // Be sure cookie always zero if activation failed
                        cookie = IntPtr.Zero;
                    }
                }
            }
        }

        ~EnableThemingInScope()
        {
            Dispose(false);
        }

        #endregion

        #region Fields

        private IntPtr cookie;
        private static ACTCTX enableThemingActivationContext;
        private static IntPtr hActCtx;
        private static bool contextCreationSucceeded;

        #endregion

        #region Methods

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (cookie != IntPtr.Zero)
            {
                if (DeactivateActCtx(0, cookie))
                {
                    // deactivation succeeded...
                    cookie = IntPtr.Zero;
                }
            }
        }

        private bool EnsureActivateContextCreated()
        {
            lock (typeof(EnableThemingInScope))
            {
                if (!contextCreationSucceeded)
                {
                    const string resourceName = "XTaskDialog.Resources.XPThemes.manifest";

                    string tempFileName = Path.GetTempFileName();
                    using (FileStream fs = new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete | FileShare.ReadWrite))
                    {
                        using (Stream manifestStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!)
                        {
                            manifestStream.CopyTo(fs);
                        }
                    }

                    enableThemingActivationContext = new ACTCTX();
                    enableThemingActivationContext.cbSize = Marshal.SizeOf(typeof(ACTCTX));
                    enableThemingActivationContext.lpSource = tempFileName;

                    // Set the lpAssemblyDirectory to the install
                    // directory to prevent Win32 Side by Side from
                    // looking for comctl32 in the application
                    // directory, which could cause a bogus dll to be
                    // placed there and open a security hole.
                    enableThemingActivationContext.lpAssemblyDirectory = typeof(Object).Assembly.Location;
                    enableThemingActivationContext.dwFlags = ACTCTX_FLAG_ASSEMBLY_DIRECTORY_VALID;

                    // Note this will fail gracefully if file specified
                    // by manifestLoc doesn't exist.
                    hActCtx = CreateActCtx(ref enableThemingActivationContext);
                    contextCreationSucceeded = (hActCtx != new IntPtr(-1));

                    try
                    {
                        File.Delete(tempFileName);
                    }
                    catch { }
                }

                // If we return false, we'll try again on the next call into
                // EnsureActivateContextCreated(), which is fine.
                return contextCreationSucceeded;
            }
        }

        #endregion
    }
}
