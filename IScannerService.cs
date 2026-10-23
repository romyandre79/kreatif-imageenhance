using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fotocopy
{
    public interface IScannerService : IDisposable
    {
        event EventHandler<Image> ImageScanned;
        event EventHandler<string> StatusChanged;
        event EventHandler<string> ErrorOccurred;

        List<string> GetScanners();
        bool StartScan(string scannerName, bool showUI);
        bool ShowScannerProperties(string scannerName);
        void CancelScan();
    }
}
