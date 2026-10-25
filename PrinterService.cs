using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Fotocopy
{
    public class PrinterService : IPrinterService
    {
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int DocumentProperties(
            IntPtr hWnd,
            IntPtr hPrinter,
            string pDeviceName,
            IntPtr pDevModeOutput,
            IntPtr pDevModeInput,
            int fMode);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern int PrinterProperties(IntPtr hWnd, IntPtr hPrinter);

        private const int DM_IN_BUFFER = 8;
        private const int DM_IN_PROMPT = 4;
        private const int DM_OUT_BUFFER = 2;

        public List<string> GetPrinters()
        {
            var list = new List<string>();
            try
            {
                if (PrinterSettings.InstalledPrinters != null)
                {
                    foreach (string printer in PrinterSettings.InstalledPrinters)
                    {
                        if (!string.IsNullOrEmpty(printer))
                        {
                            list.Add(printer);
                        }
                    }
                }
            }
            catch
            {
                // Print spooler might be stopped or unavailable in win7
            }
            return list;
        }

        public string GetDefaultPrinter()
        {
            try
            {
                var settings = new PrinterSettings();
                return settings.PrinterName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Membuka dialog pengaturan / preferensi cetak printer bawaan driver pabrikan (Epson, Canon, HP, dll).
        /// </summary>
        public bool ShowPrinterProperties(string printerName, IntPtr ownerHandle)
        {
            if (string.IsNullOrEmpty(printerName)) return false;

            // Metode 1: Menggunakan DocumentProperties API Windows (Dialog Printing Preferences resmi driver bawaan)
            try
            {
                IntPtr hPrinter = IntPtr.Zero;
                if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    try
                    {
                        // Dapatkan ukuran buffer DEVMODE
                        int neededSize = DocumentProperties(ownerHandle, hPrinter, printerName, IntPtr.Zero, IntPtr.Zero, 0);
                        if (neededSize > 0)
                        {
                            IntPtr pDevMode = Marshal.AllocHGlobal(neededSize);
                            try
                            {
                                // Buka dialog konfigurasi preferensi printer interaktif driver
                                int result = DocumentProperties(
                                    ownerHandle,
                                    hPrinter,
                                    printerName,
                                    pDevMode,
                                    pDevMode,
                                    DM_IN_PROMPT | DM_OUT_BUFFER);

                                if (result == 1) // IDOK
                                {
                                    return true;
                                }
                                else if (result == 2) // IDCANCEL
                                {
                                    return true;
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(pDevMode);
                            }
                        }

                        // Jika DocumentProperties gagal membuka dialog, coba PrinterProperties
                        int propRes = PrinterProperties(ownerHandle, hPrinter);
                        if (propRes != 0)
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        ClosePrinter(hPrinter);
                    }
                }
            }
            catch { }

            // Metode 2: Fallback memanggil rundll32 printui.dll dengan opsi preferensi cetak driver (/e)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = $"printui.dll,PrintUIEntry /e /n \"{printerName}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch { }

            // Metode 3: Fallback rundll32 opsi properti printer (/p)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = $"printui.dll,PrintUIEntry /p /n \"{printerName}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tidak dapat membuka properti printer: {ex.Message}", "Error Printer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        /// <summary>
        /// Membuat gambar komposit dari 1 gambar asli yang digandakan menjadi n gambar per halaman.
        /// Posisi sejajar ke samping dengan ukuran seragam/sama persis. Jika tidak muat di baris yang sama, pindah ke baris berikutnya.
        /// <summary>
        /// Mendapatkan ukuran pixel standar (300 DPI) untuk ukuran kertas tertentu.
        /// </summary>
        public static Size GetPaperDimensions(string paperSizeName, int dpi = 300)
        {
            float wMm, hMm;
            switch ((paperSizeName ?? "A4").ToUpperInvariant())
            {
                case "F4":
                case "FOLIO":
                    wMm = 215.9f; hMm = 330.2f; break; // 8.5 x 13 inch
                case "LETTER":
                    wMm = 215.9f; hMm = 279.4f; break; // 8.5 x 11 inch
                case "LEGAL":
                    wMm = 215.9f; hMm = 355.6f; break; // 8.5 x 14 inch
                case "A3":
                    wMm = 297f; hMm = 420f; break;
                case "A5":
                    wMm = 148f; hMm = 210f; break;
                case "A4":
                default:
                    wMm = 210f; hMm = 297f; break;
            }

            int wPx = (int)Math.Round((wMm / 25.4f) * dpi);
            int hPx = (int)Math.Round((hMm / 25.4f) * dpi);
            return new Size(wPx, hPx);
        }

        /// <summary>
        /// Membuat kanvas halaman kertas (sesuai ukuran kertas dan margin) yang berisi 1 atau lebih gambar fotokopi.
        /// </summary>
        public static Bitmap CreateMultiCopyCanvas(
            Image sourceImage,
            int countPerSheet,
            int targetWidth = 2480,
            int targetHeight = 3508,
            int marginTop = 100,
            int marginBottom = 100,
            int marginLeft = 100,
            int marginRight = 100,
            bool fitToPage = true,
            bool drawMarginGuides = false,
            int scalePercent = 100,
            int gapXMm = 3,
            int gapYMm = 3)
        {
            if (sourceImage == null) throw new ArgumentNullException(nameof(sourceImage));

            var canvas = new Bitmap(targetWidth, targetHeight);
            using (var g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                int printableW = Math.Max(100, targetWidth - (marginLeft + marginRight));
                int printableH = Math.Max(100, targetHeight - (marginTop + marginBottom));

                // Gambar garis batas tepi kertas halus agar kertas putih terlihat jelas di preview
                if (drawMarginGuides)
                {
                    using (Pen paperBorderPen = new Pen(Color.FromArgb(203, 213, 225), 2)) // Slate-300
                    {
                        g.DrawRectangle(paperBorderPen, 0, 0, targetWidth - 1, targetHeight - 1);
                    }

                    // Gambar garis putus-putus indikator margin (area cetak)
                    using (Pen marginGuidePen = new Pen(Color.FromArgb(148, 163, 184), 1)) // Slate-400
                    {
                        marginGuidePen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(marginGuidePen, marginLeft, marginTop, printableW, printableH);
                    }
                }

                float scaleMultiplier = Math.Max(0.1f, Math.Min(3.0f, scalePercent / 100f));

                // Hitung DPI kanvas sebenarnya dari lebar kertas (standar A4 210mm)
                // targetWidth 2480 = 300 DPI, targetWidth 1240 = 150 DPI
                float canvasDpi = (float)targetWidth / (210f / 25.4f);
                float srcDpiX = sourceImage.HorizontalResolution > 0 ? sourceImage.HorizontalResolution : 300f;
                float srcDpiY = sourceImage.VerticalResolution > 0 ? sourceImage.VerticalResolution : 300f;
                float dpiRatioX = canvasDpi / srcDpiX;
                float dpiRatioY = canvasDpi / srcDpiY;

                // Ukuran fisik objek 1:1 sesuai resolusi asli (WYSIWYG)
                int naturalW = (int)Math.Round(sourceImage.Width * dpiRatioX * scaleMultiplier);
                int naturalH = (int)Math.Round(sourceImage.Height * dpiRatioY * scaleMultiplier);

                if (countPerSheet <= 1)
                {
                    // 1 Gambar: Gambar di dalam batas margin, mulai dari margin Atas (Top)
                    if (fitToPage)
                    {
                        float imgAspect = (float)sourceImage.Width / Math.Max(1, sourceImage.Height);
                        float printAspect = (float)printableW / Math.Max(1, printableH);

                        int destW, destH;
                        if (imgAspect > printAspect)
                        {
                            destW = printableW;
                            destH = (int)(printableW / imgAspect);
                        }
                        else
                        {
                            destH = printableH;
                            destW = (int)(printableH * imgAspect);
                        }

                        // Terapkan scale multiplier jika diatur selain 100%
                        if (scalePercent != 100)
                        {
                            destW = (int)Math.Round(destW * scaleMultiplier);
                            destH = (int)Math.Round(destH * scaleMultiplier);
                        }

                        int destX = marginLeft + Math.Max(0, (printableW - destW) / 2);
                        int destY = marginTop;
                        g.DrawImage(sourceImage, destX, destY, destW, destH);
                    }
                    else
                    {
                        // WYSIWYG Ukuran Apa Adanya / Preset Fisik:
                        // Gambar dirender dengan skala fisik 1:1 di layar & printer sama persis
                        g.DrawImage(sourceImage, marginLeft, marginTop, naturalW, naturalH);
                    }

                    return canvas;
                }

                // Ganda Gambar: Multi-copy (KTP / kartu / pas foto / dokumen sejajar)
                // Jarak antar foto (kolom) dan jarak antar baris dihitung dari milimeter (mm) ke pixel kanvas
                int gapX = Math.Max(0, (int)Math.Round(((float)gapXMm / 25.4f) * canvasDpi));
                int gapY = Math.Max(0, (int)Math.Round(((float)gapYMm / 25.4f) * canvasDpi));

                float aspect = (float)sourceImage.Width / Math.Max(1, sourceImage.Height);

                // Tentukan jumlah kolom dan baris yang optimal
                int cols, rows;
                if (countPerSheet <= 2)
                {
                    if (aspect > 2.2f) { cols = 1; rows = countPerSheet; }
                    else { cols = countPerSheet; rows = 1; }
                }
                else if (countPerSheet == 3)
                {
                    cols = 3; rows = 1;
                    if (aspect < 0.8f && (printableW / 3 < 100)) { cols = 2; rows = 2; }
                }
                else if (countPerSheet == 4)
                {
                    cols = 2; rows = 2;
                }
                else if (countPerSheet == 6)
                {
                    cols = aspect < 0.9f ? 3 : 2;
                    rows = (int)Math.Ceiling(6.0 / cols);
                }
                else if (countPerSheet == 8)
                {
                    cols = aspect < 0.9f ? 4 : 2;
                    rows = (int)Math.Ceiling(8.0 / cols);
                }
                else if (countPerSheet == 9)
                {
                    cols = 3; rows = 3;
                }
                else if (countPerSheet <= 12)
                {
                    cols = aspect < 0.9f ? 4 : 3;
                    rows = (int)Math.Ceiling((double)countPerSheet / cols);
                }
                else if (countPerSheet <= 16)
                {
                    cols = 4; rows = 4;
                }
                else
                {
                    cols = (int)Math.Ceiling(Math.Sqrt(countPerSheet));
                    rows = (int)Math.Ceiling((double)countPerSheet / cols);
                }

                int itemW, itemH;

                if (!fitToPage)
                {
                    // WYSIWYG: Pertahankan ukuran fisik asli 1:1 objek foto / scan
                    itemW = naturalW;
                    itemH = naturalH;

                    // Mengalir ke samping sebanyak mungkin hingga tidak muat lebar kertas (flow horizontally first)
                    int maxColsPossible = Math.Max(1, (printableW + gapX) / (itemW + gapX));
                    cols = Math.Min(countPerSheet, maxColsPossible);
                    rows = (int)Math.Ceiling((double)countPerSheet / cols);
                }
                else
                {
                    // Fit to page: Jika gambar relatif kecil dibanding lebar kertas, atau diatur flow kesamping
                    // Tentukan kolom default berdasarkan proporsi aspek dan lebar kertas
                    int cellW = (printableW - ((cols - 1) * gapX)) / cols;
                    int maxAvailableH = (printableH - ((rows - 1) * gapY)) / rows;

                    itemW = cellW;
                    itemH = (int)Math.Round(itemW / aspect);

                    if (itemH > maxAvailableH)
                    {
                        itemH = maxAvailableH;
                        itemW = (int)Math.Round(itemH * aspect);
                    }

                    if (scalePercent != 100)
                    {
                        itemW = (int)Math.Round(itemW * scaleMultiplier);
                        itemH = (int)Math.Round(itemH * scaleMultiplier);
                    }
                }

                // Render setiap baris rapat dimulai dari Margin Atas (marginTop)
                for (int i = 0; i < countPerSheet; i++)
                {
                    int c = i % cols;
                    int r = i / cols;

                    int posX = marginLeft + c * (itemW + gapX);
                    int posY = marginTop + r * (itemH + gapY);

                    g.DrawImage(sourceImage, posX, posY, itemW, itemH);
                }
            }

            return canvas;
        }

        public void PrintImage(
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
            int scalePercent = 100,
            int gapXMm = 3,
            int gapYMm = 3)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            Size paperPx = GetPaperDimensions(paperSizeName, 300);
            int mTopPx = (int)Math.Round((marginTopMm / 25.4f) * 300);
            int mBottomPx = (int)Math.Round((marginBottomMm / 25.4f) * 300);
            int mLeftPx = (int)Math.Round((marginLeftMm / 25.4f) * 300);
            int mRightPx = (int)Math.Round((marginRightMm / 25.4f) * 300);

            // Selalu render ke kanvas kertas lengkap dengan ukuran dan margin presisi
            Bitmap pageToPrint = CreateMultiCopyCanvas(
                image,
                itemsPerSheet,
                paperPx.Width,
                paperPx.Height,
                mTopPx,
                mBottomPx,
                mLeftPx,
                mRightPx,
                fitToPage,
                false,
                scalePercent,
                gapXMm,
                gapYMm);

            try
            {
                using (PrintDocument pd = new PrintDocument())
                {
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        pd.PrinterSettings.PrinterName = printerName;
                    }

                    if (!pd.PrinterSettings.IsValid)
                    {
                        throw new InvalidOperationException($"Printer '{printerName}' tidak valid atau tidak ditemukan.");
                    }

                    pd.PrinterSettings.Copies = copies > 0 ? copies : (short)1;

                    // Cocokkan ukuran kertas printer driver jika tersedia
                    foreach (PaperSize ps in pd.PrinterSettings.PaperSizes)
                    {
                        if (ps.PaperName.IndexOf(paperSizeName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            pd.DefaultPageSettings.PaperSize = ps;
                            break;
                        }
                    }

                    // Margin cetak nol pada driver karena margin sudah dirender tepat di kanvas halaman
                    pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                    pd.OriginAtMargins = false;

                    pd.PrintPage += (sender, e) =>
                    {
                        Rectangle pageBounds = e.PageBounds;
                        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        e.Graphics.DrawImage(pageToPrint, pageBounds);
                        e.HasMorePages = false;
                    };

                    pd.Print();
                }
            }
            finally
            {
                pageToPrint?.Dispose();
            }
        }
    }
}
