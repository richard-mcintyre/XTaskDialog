using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTaskDialog
{
    public interface ITaskDialogInstance
    {
        /// <summary>
        /// Handle of the task dialog
        /// </summary>
        IntPtr HWnd { get; }

        /// <summary>
        /// Starts the marquee progress bar
        /// </summary>
        void StartMarquee();

        /// <summary>
        /// Stops the marquee progress bar
        /// </summary>
        void StopMarquee();

        /// <summary>
        /// Sets the progress bar range
        /// </summary>
        void SetProgressBarRange(int minRange, int maxRange);

        /// <summary>
        /// Sets the progress bars current position
        /// </summary>
        void SetProgressBarPosition(int position);

        /// <summary>
        /// Sets the progress bars state
        /// </summary>
        void SetProgressBarState(TaskDialogProgessBarState state);

        /// <summary>
        /// Updates the text of an element in the dialog
        /// </summary>
        void UpdateElementText(TaskDialogElements element, string text);

        /// <summary>
        /// Simulates the user clicking the specified button
        /// </summary>
        void ClickButton(TaskDialogResult button);
    }
}
