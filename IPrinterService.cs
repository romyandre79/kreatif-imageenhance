using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fotocopy
{
    public interface IPrinterService
    {
        List<string> GetPrinters();
        string GetDefaultPrinter();
        bool ShowPrinterProperties(string printerName, IntPtr parentWindowHandle);
        void PrintImage(
            Image image,
            string printerName,
            short copies = 1,
            bool fitToPage = true,
            int itemsPerSheet = 1,
            string paperSizeName = "A4",
            int marginTopMm = 10,
            int marginBottomMm = 10,
            int marginLeftMm = 10,
            int marginRightMm = 10,
            int scalePercent = 100);
    }
}
