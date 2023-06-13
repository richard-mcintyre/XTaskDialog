using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTaskDialog
{
    public enum TaskDialogNotificationKind
    {
        Created = 0,
        Navigated = 1,
        ButtonClicked = 2,                  // wParam = Button ID
        HyperlinkClicked = 3,               // lParam = (LPCWSTR)pszHREF
        Timer = 4,                          // wParam = Milliseconds since dialog created or timer reset
        Destroyed = 5,
        RadioButtonClicked = 6,             // wParam = Radio Button ID
        DialogConstructed = 7,
        VerificationClicked = 8,            // wParam = 1 if checkbox checked, 0 if not, lParam is unused and always 0
        Help = 9,
        ExpandoButtonClicked = 10           // wParam = 0 (dialog is now collapsed), wParam != 0 (dialog is now expanded)
    }
}
