using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTaskDialog
{
    public class ProgressDialogProgressInfo
    {
        public string? MainInstruction { get; set; }

        public string? Content { get; set; }

        public ProgressBarInfo? ProgressBar { get; set; }

        public void SetFrom(ProgressDialogProgressInfo other)
        {
            this.MainInstruction = other.MainInstruction;
            this.Content = other.Content;
            this.ProgressBar = other.ProgressBar;
        }
    }

    public record ProgressBarInfo(int MinRange, int MaxRange, int Position);
}
