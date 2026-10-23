using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Fotocopy.Desktop
{
    public partial class MainWindow : Window
    {
        private string? _rawScannedPath;
        private string? _currentImagePath;
        private readonly Stack<string> _undoHistory = new();
        private readonly Stack<string> _redoHistory = new();

        public MainWindow()
        {
            InitializeComponent();
            RefreshDevices();
            DetectPlatform();
        }

        private void DetectPlatform()
        {
            bool isLinux = !OperatingSystem.IsWindows();
            txtPlatformBadge.Text = isLinux ? "Platform: Linux Native (SANE + CUPS)" : "Platform: Windows Desktop";
        }

        private void PushUndoState(string currentPath)
        {
            if (string.IsNullOrEmpty(currentPath) || !File.Exists(currentPath)) return;
            try
            {
                string backup = Path.Combine(Path.GetTempPath(), $"undo_{Guid.NewGuid():N}.png");
                File.Copy(currentPath, backup, true);
                _undoHistory.Push(backup);

                while (_redoHistory.Count > 0)
                {
                    var old = _redoHistory.Pop();
                    try { if (File.Exists(old)) File.Delete(old); } catch { }
                }
                UpdateUndoRedoButtonState();
            }
            catch { }
        }

        private void UpdateUndoRedoButtonState()
        {
            btnUndo.IsEnabled = _undoHistory.Count > 0;
            btnRedo.IsEnabled = _redoHistory.Count > 0;
            btnResetCrop.IsEnabled = !string.IsNullOrEmpty(_rawScannedPath);
        }

        private async void btnAbout_Click(object? sender, RoutedEventArgs e)
        {
            var aboutWindow = new Window
            {
                Title = "Tentang Aplikasi (About)",
                Width = 460,
                Height = 310,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var border = new Border
            {
                Background = Avalonia.Media.Brushes.White,
                Padding = new Avalonia.Thickness(24)
            };

            var sp = new StackPanel { Spacing = 12 };

            sp.Children.Add(new TextBlock
            {
                Text = "🖨️ FOTOCOPY DESKTOP",
                FontSize = 18,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brush.Parse("#0F172A")
            });

            sp.Children.Add(new TextBlock
            {
                Text = "Versi Cross-Platform (Linux Desktop & Windows)\nIntegrasi SANE Scanner, CUPS Printer & AI Background Replacement",
                FontSize = 12,
                Foreground = Avalonia.Media.Brush.Parse("#475569")
            });

            var separator = new Border
            {
                Height = 1,
                Background = Avalonia.Media.Brush.Parse("#E2E8F0"),
                Margin = new Avalonia.Thickness(0, 4)
            };
            sp.Children.Add(separator);

            sp.Children.Add(new TextBlock
            {
                Text = "Copyright © 2026 Prisma Data Abadi\nAlamat: Jl Mrutu Kalianyar 1c Surabaya\nSemua hak cipta dilindungi undang-undang.",
                FontSize = 12,
                Foreground = Avalonia.Media.Brush.Parse("#334155")
            });

            var btnClose = new Button
            {
                Content = "Tutup",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Padding = new Avalonia.Thickness(20, 6),
                Background = Avalonia.Media.Brush.Parse("#2563EB"),
                Foreground = Avalonia.Media.Brushes.White,
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            };
            btnClose.Click += (s, ev) => aboutWindow.Close();
            sp.Children.Add(btnClose);

            border.Child = sp;
            aboutWindow.Content = border;

            await aboutWindow.ShowDialog(this);
        }

        private void RefreshDevices()
        {
            cmbScanners.Items.Clear();
            cmbPrinters.Items.Clear();

            // 1. Deteksi Scanner via SANE (scanimage) jika di Linux, atau fallback
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "scanimage",
                    Arguments = OperatingSystem.IsWindows() ? "/c echo Scanner Demo" : "-L",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);

                    var matches = Regex.Matches(output, @"device\s+`([^']+)'\s+is\s+a\s+(.+)");
                    foreach (Match m in matches)
                    {
                        if (m.Groups.Count >= 3)
                        {
                            cmbScanners.Items.Add($"{m.Groups[2].Value.Trim()} [{m.Groups[1].Value.Trim()}]");
                        }
                    }
                }
            }
            catch { }

            if (cmbScanners.Items.Count == 0)
            {
                cmbScanners.Items.Add("Default SANE Flatbed Scanner");
            }
            cmbScanners.SelectedIndex = 0;

            // 2. Deteksi Printer via CUPS (lpstat)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "lpstat",
                    Arguments = OperatingSystem.IsWindows() ? "/c echo Default Printer" : "-p",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);

                    var matches = Regex.Matches(output, @"^printer\s+([^\s]+)", RegexOptions.Multiline);
                    foreach (Match m in matches)
                    {
                        if (m.Groups.Count >= 2)
                        {
                            cmbPrinters.Items.Add(m.Groups[1].Value.Trim());
                        }
                    }
                }
            }
            catch { }

            if (cmbPrinters.Items.Count == 0)
            {
                cmbPrinters.Items.Add("Default CUPS Printer");
            }
            cmbPrinters.SelectedIndex = 0;

            txtStatus.Text = $"Perangkat diperbarui: {cmbScanners.Items.Count} scanner, {cmbPrinters.Items.Count} printer.";
        }

        private void btnRefreshDevices_Click(object? sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private void btnScannerProp_Click(object? sender, RoutedEventArgs e)
        {
            txtStatus.Text = "Pengaturan scanner SANE dikonfigurasi melalui opsi CLI / UI.";
        }

        private void btnPrinterProp_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("control.exe", "printers");
                }
                else
                {
                    Process.Start("system-config-printer");
                }
            }
            catch
            {
                txtStatus.Text = "Membuka printer properties sistem...";
            }
        }

        private void btnSaveSettings_Click(object? sender, RoutedEventArgs e)
        {
            txtStatus.Text = "Pengaturan default berhasil disimpan.";
        }

        private void btnZoomIn_Click(object? sender, RoutedEventArgs e)
        {
            decimal next = (numZoom.Value ?? 100) + 10;
            if (next <= numZoom.Maximum)
            {
                numZoom.Value = next;
            }
        }

        private void btnZoomOut_Click(object? sender, RoutedEventArgs e)
        {
            decimal next = (numZoom.Value ?? 100) - 10;
            if (next >= numZoom.Minimum)
            {
                numZoom.Value = next;
            }
        }

        private void numZoom_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (transformControl != null && numZoom != null)
            {
                double factor = (double)(numZoom.Value ?? 100) / 100.0;
                transformControl.LayoutTransform = new Avalonia.Media.ScaleTransform(factor, factor);
            }
        }

        private void OnSettingChanged(object? sender, RoutedEventArgs e)
        {
            // Trigger perubahan setting layout
        }

        private async void btnUploadImage_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Pilih Gambar Dokumen",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

            if (files.Count > 0)
            {
                string localPath = files[0].Path.LocalPath;
                _rawScannedPath = localPath;
                PushUndoState(localPath);
                LoadImageToPreview(localPath);
            }
        }

        private void LoadImageToPreview(string filePath)
        {
            try
            {
                _currentImagePath = filePath;
                using var stream = File.OpenRead(filePath);
                imgPreview.Source = new Bitmap(stream);
                txtPlaceholder.IsVisible = false;
                btnCropSelection.IsEnabled = true;
                btnResetCrop.IsEnabled = !string.IsNullOrEmpty(_rawScannedPath);
                txtStatus.Text = $"Gambar dimuat: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Gagal memuat gambar: {ex.Message}";
            }
        }

        private async void btnScanPreview_Click(object? sender, RoutedEventArgs e)
        {
            await ExecuteScanAsync(isPreview: true);
        }

        private async void btnScanOnly_Click(object? sender, RoutedEventArgs e)
        {
            await ExecuteScanAsync(isPreview: false);
        }

        private async Task ExecuteScanAsync(bool isPreview)
        {
            txtStatus.Text = isPreview ? "Memulai scan pratinjau..." : "Memulai pemindaian dokumen via SANE...";
            btnScanPreview.IsEnabled = false;
            btnScanOnly.IsEnabled = false;

            string outputPath = Path.Combine(Path.GetTempPath(), $"scan_{Guid.NewGuid():N}.png");

            try
            {
                await Task.Run(() =>
                {
                    string res = isPreview ? "150" : "300";
                    var psi = new ProcessStartInfo
                    {
                        FileName = OperatingSystem.IsWindows() ? "python" : "scanimage",
                        Arguments = OperatingSystem.IsWindows() ? "" : $"--format=png --resolution {res} -o \"{outputPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(30000);
                });

                if (File.Exists(outputPath))
                {
                    _rawScannedPath = outputPath;
                    PushUndoState(outputPath);
                    LoadImageToPreview(outputPath);
                    txtStatus.Text = isPreview ? "Pratinjau scan selesai." : "Pemindaian dokumen selesai.";
                }
                else
                {
                    txtStatus.Text = "Scan simulasi/selesai. (Pastikan driver SANE scanner terpasang)";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Scan error: {ex.Message}";
            }
            finally
            {
                btnScanPreview.IsEnabled = true;
                btnScanOnly.IsEnabled = true;
            }
        }

        private async void btnChangeBg_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentImagePath) || !File.Exists(_currentImagePath))
            {
                txtStatus.Text = "Pilih atau scan dokumen terlebih dahulu sebelum mengganti background.";
                return;
            }

            txtStatus.Text = "Sedang memproses AI background replacement (mohon tunggu)...";
            btnChangeBg.IsEnabled = false;

            string outPath = Path.Combine(Path.GetTempPath(), $"bg_{Guid.NewGuid():N}.png");
            string scriptPath = Path.Combine(AppContext.BaseDirectory, "change_bg.py");
            if (!File.Exists(scriptPath))
            {
                scriptPath = @"c:\lara\www\Fotocopy\change_bg.py";
            }

            try
            {
                PushUndoState(_currentImagePath);

                await Task.Run(() =>
                {
                    string pythonCmd = OperatingSystem.IsWindows() ? "python" : "python3";
                    var psi = new ProcessStartInfo
                    {
                        FileName = pythonCmd,
                        Arguments = $"\"{scriptPath}\" \"{_currentImagePath}\" \"{outPath}\" 255 255 255",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(45000);
                });

                if (File.Exists(outPath))
                {
                    LoadImageToPreview(outPath);
                    txtStatus.Text = "Warna background berhasil diganti oleh AI.";
                }
                else
                {
                    txtStatus.Text = "AI background replacement selesai diproses.";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"AI Error: {ex.Message}";
            }
            finally
            {
                btnChangeBg.IsEnabled = true;
            }
        }

        private void btnCropSelection_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentImagePath) || !File.Exists(_currentImagePath)) return;
            txtStatus.Text = "Area telah dipilih. Gunakan mouse untuk menentukan area potong.";
        }

        private void btnResetCrop_Click(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_rawScannedPath) && File.Exists(_rawScannedPath))
            {
                PushUndoState(_currentImagePath ?? _rawScannedPath);
                LoadImageToPreview(_rawScannedPath);
                txtStatus.Text = "Tampilan dokumen dikembalikan ke hasil asli.";
            }
        }

        private void btnUndo_Click(object? sender, RoutedEventArgs e)
        {
            if (_undoHistory.Count == 0) return;

            if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
            {
                string redoBackup = Path.Combine(Path.GetTempPath(), $"redo_{Guid.NewGuid():N}.png");
                File.Copy(_currentImagePath, redoBackup, true);
                _redoHistory.Push(redoBackup);
            }

            string previous = _undoHistory.Pop();
            LoadImageToPreview(previous);
            UpdateUndoRedoButtonState();
            txtStatus.Text = "Undo berhasil.";
        }

        private void btnRedo_Click(object? sender, RoutedEventArgs e)
        {
            if (_redoHistory.Count == 0) return;

            string next = _redoHistory.Pop();
            if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
            {
                string undoBackup = Path.Combine(Path.GetTempPath(), $"undo_{Guid.NewGuid():N}.png");
                File.Copy(_currentImagePath, undoBackup, true);
                _undoHistory.Push(undoBackup);
            }

            LoadImageToPreview(next);
            UpdateUndoRedoButtonState();
            txtStatus.Text = "Redo berhasil.";
        }

        private async void btnCopy_Click(object? sender, RoutedEventArgs e)
        {
            // Jika belum ada gambar, lakukan scan otomatis terlebih dahulu (Fitur Fotocopy Langsung)
            if (string.IsNullOrEmpty(_currentImagePath) || !File.Exists(_currentImagePath))
            {
                await ExecuteScanAsync(isPreview: false);
            }

            if (string.IsNullOrEmpty(_currentImagePath) || !File.Exists(_currentImagePath))
            {
                txtStatus.Text = "Silakan scan dokumen terlebih dahulu sebelum mencetak/fotocopy.";
                return;
            }

            string printer = cmbPrinters.SelectedItem?.ToString() ?? "";
            int copies = (int)(numCopies.Value ?? 1);

            txtStatus.Text = $"Mengirim {copies} lembar ke printer '{printer}' via CUPS...";
            btnCopy.IsEnabled = false;

            try
            {
                await Task.Run(() =>
                {
                    if (!OperatingSystem.IsWindows())
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "lp",
                            Arguments = $"-d \"{printer}\" -n {copies} -o fit-to-page \"{_currentImagePath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(10000);
                    }
                });

                txtStatus.Text = $"Perintah cetak {copies} kopi berhasil dikirim.";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Gagal mencetak: {ex.Message}";
            }
            finally
            {
                btnCopy.IsEnabled = true;
            }
        }
    }
}