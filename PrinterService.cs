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
            int scalePercent = 100)
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
                        // Jika tidak fit (100% ukuran asli tanpa diperbesar ke batas halaman):
                        // Gambar dirender dengan skala fisik 1:1 sesuai resolusi target kanvas
                        // (pada 300 DPI targetWidth ~2480, pada preview 150 DPI targetWidth ~1240).
                        float srcDpiX = sourceImage.HorizontalResolution > 0 ? sourceImage.HorizontalResolution : 300f;
                        float srcDpiY = sourceImage.VerticalResolution > 0 ? sourceImage.VerticalResolution : 300f;

                        // Konversi ukuran pixel gambar scan ke pixel target kanvas
                        float canvasDpi = (float)targetWidth / (210f / 25.4f); // estimasi DPI kanvas dari lebar kertas
                        float dpiRatioX = canvasDpi / srcDpiX;
                        float dpiRatioY = canvasDpi / srcDpiY;

                        int origW = (int)Math.Round(sourceImage.Width * dpiRatioX * scaleMultiplier);
                        int origH = (int)Math.Round(sourceImage.Height * dpiRatioY * scaleMultiplier);

                        g.DrawImage(sourceImage, marginLeft, marginTop, origW, origH);
                    }

                    return canvas;
                }

                // Ganda Gambar: Multi-copy (KTP / kartu / dokumen sejajar)
                // Jarak rapat (compact gap) antar objek agar hemat kertas (sekitar 4-6 mm)
                float scaleFactor = (float)targetWidth / 2480f;
                int gapX = Math.Max(8, (int)Math.Round(25 * scaleFactor)); // ~3-4 mm rapat
                int gapY = Math.Max(8, (int)Math.Round(25 * scaleFactor)); // ~3-4 mm rapat

                float aspect = (float)sourceImage.Width / Math.Max(1, sourceImage.Height);

                // Jika rasio lebar (> 2.0 seperti 2 KTP sejajar hasil crop), 1 item per baris
                // Jika rasio normal KTP (~1.4 - 1.8), 2 kolom per baris
                int maxColsPerRow = 2;
                if (aspect > 2.2f)
                {
                    maxColsPerRow = 1;
                }

                int cols = Math.Min(countPerSheet, maxColsPerRow);
                int rows = (int)Math.Ceiling((double)countPerSheet / cols);

                // Lebar maksimal yang tersedia per kolom
                int cellW = (printableW - ((cols - 1) * gapX)) / cols;

                // Batasi tinggi per baris agar muat di printable area
                int maxAvailableH = (printableH - ((rows - 1) * gapY)) / rows;

                // Hitung dimensi item proporsional
                int itemW = cellW;
                int itemH = (int)Math.Round(itemW / aspect);

                if (itemH > maxAvailableH)
                {
                    itemH = maxAvailableH;
                    itemW = (int)Math.Round(itemH * aspect);
                }

                // Terapkan scale multiplier jika diatur selain 100%
                if (scalePercent != 100)
                {
                    itemW = (int)Math.Round(itemW * scaleMultiplier);
                    itemH = (int)Math.Round(itemH * scaleMultiplier);
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
            int scalePercent = 100)
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
                scalePercent);

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
