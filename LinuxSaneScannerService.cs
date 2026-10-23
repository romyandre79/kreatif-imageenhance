using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fotocopy
{
    /// <summary>
    /// Implementasi IScannerService untuk platform Linux menggunakan SANE (Scanner Access Now Easy)
    /// Memanfaatkan utilitas native Linux 'scanimage'.
    /// </summary>
    public class LinuxSaneScannerService : IScannerService
    {
        public event EventHandler<Image> ImageScanned;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<string> ErrorOccurred;

        private Process _activeProcess;
        private bool _isDisposed;

        public LinuxSaneScannerService()
        {
        }

        public List<string> GetScanners()
        {
            var list = new List<string>();
            try
            {
                // Eksekusi: scanimage -L (format: device `genesys:libusb:001:004' is a Canon CanoScan LiDE 110 flatbed scanner)
                var psi = new ProcessStartInfo
                {
                    FileName = "scanimage",
                    Arguments = "-L",
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
                        proc.WaitForExit(5000);

                        var matches = Regex.Matches(output, @"device\s+`([^']+)'\s+is\s+a\s+(.+)");
                        foreach (Match m in matches)
                        {
                            if (m.Groups.Count >= 3)
                            {
                                string devId = m.Groups[1].Value.Trim();
                                string devDesc = m.Groups[2].Value.Trim();
                                list.Add($"{devDesc} [{devId}]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Gagal mendeteksi scanner Linux SANE: {ex.Message}");
            }

            return list;
        }

        public bool StartScan(string scannerName, bool showUI)
        {
            try
            {
                // Ekstrak device id dari nama jika ada pola [device_id]
                string deviceArg = "";
                if (!string.IsNullOrEmpty(scannerName))
                {
                    var match = Regex.Match(scannerName, @"\[([^\]]+)\]$");
                    if (match.Success)
                    {
                        deviceArg = $"-d \"{match.Groups[1].Value}\"";
                    }
                }

                string tempOutput = Path.Combine(Path.GetTempPath(), $"sane_scan_{Guid.NewGuid():N}.png");

                StatusChanged?.Invoke(this, "Memulai scanning dokumen via Linux SANE...");

                Task.Run(() =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "scanimage",
                            Arguments = $"{deviceArg} --format=png --resolution 300 -o \"{tempOutput}\"",
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using (_activeProcess = Process.Start(psi))
                        {
                            if (_activeProcess != null)
                            {
                                string stderr = _activeProcess.StandardError.ReadToEnd();
                                _activeProcess.WaitForExit();

                                if (File.Exists(tempOutput))
                                {
                                    byte[] imgBytes = File.ReadAllBytes(tempOutput);
                                    using (var ms = new MemoryStream(imgBytes))
                                    {
                                        var scannedBmp = new Bitmap(ms);
                                        ImageScanned?.Invoke(this, scannedBmp);
                                    }
                                    StatusChanged?.Invoke(this, "Pemindaian selesai.");
                                }
                                else
                                {
                                    ErrorOccurred?.Invoke(this, $"Gagal memindai gambar via SANE: {stderr}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, $"Error saat scan via SANE: {ex.Message}");
                    }
                    finally
                    {
                        try { if (File.Exists(tempOutput)) File.Delete(tempOutput); } catch { }
                        _activeProcess = null;
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Gagal menjalankan SANE scan: {ex.Message}");
                return false;
            }
        }

        public bool ShowScannerProperties(string scannerName)
        {
            StatusChanged?.Invoke(this, "SANE Linux tidak menggunakan modal GUI Windows Driver.");
            return true;
        }

        public void CancelScan()
        {
            try
            {
                if (_activeProcess != null && !_activeProcess.HasExited)
                {
                    _activeProcess.Kill();
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                CancelScan();
                _isDisposed = true;
            }
        }
    }
}
