using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;

namespace Fotocopy
{
    /// <summary>
    /// Implementasi IPrinterService untuk platform Linux menggunakan CUPS (Common Unix Printing System).
    /// Menggunakan utilitas standar CUPS: 'lpstat' dan 'lp'.
    /// </summary>
    public class LinuxCupsPrinterService : IPrinterService
    {
        public List<string> GetPrinters()
        {
            var list = new List<string>();
            try
            {
                // Eksekusi: lpstat -p
                // Output format: printer Canon_MP280 is idle. enabled since ...
                var psi = new ProcessStartInfo
                {
                    FileName = "lpstat",
                    Arguments = "-p",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(3000);

                        var matches = Regex.Matches(output, @"^printer\s+([^\s]+)", RegexOptions.Multiline);
                        foreach (Match m in matches)
                        {
                            if (m.Groups.Count >= 2)
                            {
                                list.Add(m.Groups[1].Value.Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // CUPS mungkin belum terinstall atau service cups daemon mati
            }

            return list;
        }

        public string GetDefaultPrinter()
        {
            try
            {
                // Eksekusi: lpstat -d
                // Output format: system default destination: Canon_MP280
                var psi = new ProcessStartInfo
                {
                    FileName = "lpstat",
                    Arguments = "-d",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(3000);

                        var match = Regex.Match(output, @"system default destination:\s*([^\s]+)");
                        if (match.Success)
                        {
                            return match.Groups[1].Value.Trim();
                        }
                    }
                }
            }
            catch { }

            var all = GetPrinters();
            return all.Count > 0 ? all[0] : string.Empty;
        }

        public bool ShowPrinterProperties(string printerName, IntPtr parentWindowHandle)
        {
            // Di Linux desktop (GNOME/KDE), dialog printer properties dibuka lewat system settings: system-config-printer
            try
            {
                Process.Start("system-config-printer");
                return true;
            }
            catch
            {
                // Fallback jika system-config-printer tidak ada
                return false;
            }
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

            string tempFile = Path.Combine(Path.GetTempPath(), $"cups_print_{Guid.NewGuid():N}.png");
            try
            {
                // Buat kanvas multi-copy bila itemsPerSheet > 1
                Image toPrint = image;
                Bitmap generatedCanvas = null;
                if (itemsPerSheet > 1)
                {
                    var pageSizePx = PrinterService.GetPaperDimensions(paperSizeName, 300);
                    generatedCanvas = PrinterService.CreateMultiCopyCanvas(
                        image,
                        itemsPerSheet,
                        pageSizePx.Width,
                        pageSizePx.Height,
                        (int)Math.Round((marginTopMm / 25.4f) * 300),
                        (int)Math.Round((marginBottomMm / 25.4f) * 300),
                        (int)Math.Round((marginLeftMm / 25.4f) * 300),
                        (int)Math.Round((marginRightMm / 25.4f) * 300),
                        fitToPage,
                        false,
                        scalePercent);
                    toPrint = generatedCanvas;
                }

                toPrint.Save(tempFile, ImageFormat.Png);
                generatedCanvas?.Dispose();

                // Format opsi CUPS lp:
                // -d <printer>
                // -n <copies>
                // -o media=<paperSize>
                // -o fit-to-page
                string mediaOption = MapPaperSizeToCupsMedia(paperSizeName);
                string fitOption = fitToPage ? "-o fit-to-page" : "";
                string copiesOption = copies > 1 ? $"-n {copies}" : "";
                string destOption = !string.IsNullOrEmpty(printerName) ? $"-d \"{printerName}\"" : "";

                string args = $"{destOption} {copiesOption} -o media={mediaOption} {fitOption} \"{tempFile}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = "lp",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(10000);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CUPS Print Error: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        private string MapPaperSizeToCupsMedia(string paperSize)
        {
            if (string.IsNullOrEmpty(paperSize)) return "A4";

            string lower = paperSize.ToLowerInvariant();
            if (lower.Contains("f4") || lower.Contains("folio")) return "Folio";
            if (lower.Contains("legal")) return "Legal";
            if (lower.Contains("letter")) return "Letter";
            if (lower.Contains("a3")) return "A3";
            if (lower.Contains("a5")) return "A5";

            return "A4";
        }
    }
}
