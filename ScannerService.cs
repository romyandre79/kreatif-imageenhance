using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NTwain;
using NTwain.Data;

namespace Fotocopy
{
    public class ScannerService : IScannerService
    {
        private TwainSession _session;
        private IntPtr _windowHandle;
        private bool _isDisposed;

        public event EventHandler<Image> ImageScanned;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<string> ErrorOccurred;

        public TwainSession Session => _session;

        public ScannerService(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            InitializeSession();
        }

        private void InitializeSession()
        {
            try
            {
                // Prioritize standard 32-bit Windows DSM (twain_32.dll) for Windows 7/10/11 drivers
                try
                {
                    PlatformInfo.Current.PreferNewDSM = false;
                }
                catch { }

                var appId = TWIdentity.Create(
                    DataGroups.Image,
                    new Version(1, 0, 0, 0),
                    "FotocopyApp",
                    "Fotocopy",
                    "Fotocopy",
                    "Fotocopy Application");

                _session = new TwainSession(appId);
                
                // Disable strict state enforcement so driver quirks on Win7 do not throw exceptions
                _session.EnforceState = false;

                _session.DataTransferred += Session_DataTransferred;
                _session.TransferError += Session_TransferError;
                _session.TransferReady += Session_TransferReady;
                _session.SourceDisabled += Session_SourceDisabled;

                ReturnCode rc = ReturnCode.Failure;
                if (_windowHandle != IntPtr.Zero)
                {
                    try
                    {
                        rc = _session.Open(new WindowsFormsMessageLoopHook(_windowHandle));
                    }
                    catch
                    {
                        rc = _session.Open();
                    }
                }
                else
                {
                    rc = _session.Open();
                }

                if (rc != ReturnCode.Success)
                {
                    OnStatusChanged($"Twain Session Open info: {rc}");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Gagal inisialisasi TWAIN: {ex.Message}");
            }
        }

        public List<string> GetScanners()
        {
            var list = new List<string>();
            try
            {
                if (_session != null && _session.State >= 3)
                {
                    foreach (var src in _session)
                    {
                        if (src != null && !string.IsNullOrEmpty(src.Name))
                        {
                            list.Add(src.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Gagal membaca daftar scanner: {ex.Message}");
            }

            return list;
        }

        public bool StartScan(string scannerName, bool showUI = false)
        {
            if (_session == null)
            {
                OnErrorOccurred("Sesi scanner belum siap.");
                return false;
            }

            try
            {
                // Ensure state is clean before opening source
                if (_session.CurrentSource != null)
                {
                    try
                    {
                        _session.CurrentSource.Close();
                    }
                    catch { }
                }

                if (_session.State > 3)
                {
                    try
                    {
                        _session.ForceStepDown(3);
                    }
                    catch { }
                }

                DataSource targetSource = null;
                if (!string.IsNullOrEmpty(scannerName))
                {
                    foreach (var src in _session)
                    {
                        if (src != null && string.Equals(src.Name, scannerName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetSource = src;
                            break;
                        }
                    }
                }

                if (targetSource == null)
                {
                    targetSource = _session.DefaultSource;
                }

                if (targetSource == null)
                {
                    OnErrorOccurred("Scanner yang dipilih tidak ditemukan dalam daftar perangkat.");
                    return false;
                }

                // If not in state 3, attempt to step down to state 3
                if (_session.State < 3)
                {
                    _session.Open();
                }

                var openRc = targetSource.Open();
                if (openRc != ReturnCode.Success && _session.State < 4)
                {
                    OnErrorOccurred($"Gagal membuka scanner '{targetSource.Name}'. Kode respons TWAIN: {openRc}");
                    return false;
                }

                OnStatusChanged($"Menghubungkan ke scanner '{targetSource.Name}'...");
                var mode = showUI ? SourceEnableMode.ShowUI : SourceEnableMode.NoUI;

                // Call Enable
                ReturnCode enableRc;
                try
                {
                    enableRc = targetSource.Enable(mode, false, _windowHandle);
                }
                catch (TwainStateException)
                {
                    // If driver auto-transitioned state, retry enable
                    enableRc = targetSource.Enable(mode, false, _windowHandle);
                }

                if (enableRc != ReturnCode.Success && enableRc != ReturnCode.CheckStatus)
                {
                    OnErrorOccurred($"Scanner mengembalikan kode respons: {enableRc}. Silakan coba centang opsi 'Tampilkan UI Scanner'.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Terjadi kesalahan saat memulai scan: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Membuka dialog pengaturan properti / konfigurasi driver bawaan scanner.
        /// </summary>
        public bool ShowScannerProperties(string scannerName)
        {
            if (_session == null)
            {
                OnErrorOccurred("Sesi scanner belum siap.");
                return false;
            }

            try
            {
                if (_session.CurrentSource != null)
                {
                    try { _session.CurrentSource.Close(); } catch { }
                }

                if (_session.State > 3)
                {
                    try { _session.ForceStepDown(3); } catch { }
                }

                DataSource targetSource = null;
                if (!string.IsNullOrEmpty(scannerName))
                {
                    foreach (var src in _session)
                    {
                        if (src != null && string.Equals(src.Name, scannerName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetSource = src;
                            break;
                        }
                    }
                }

                if (targetSource == null) targetSource = _session.DefaultSource;
                if (targetSource == null)
                {
                    OnErrorOccurred("Pilih scanner terlebih dahulu.");
                    return false;
                }

                if (_session.State < 3) _session.Open();

                var openRc = targetSource.Open();
                if (openRc != ReturnCode.Success && _session.State < 4)
                {
                    OnErrorOccurred($"Gagal membuka scanner '{targetSource.Name}'. Kode: {openRc}");
                    return false;
                }

                OnStatusChanged($"Membuka dialog konfigurasi '{targetSource.Name}'...");

                // Metode 1: TWAIN ShowUIOnly (Dialog konfigurasi driver murni)
                ReturnCode enableRc = targetSource.Enable(SourceEnableMode.ShowUIOnly, true, _windowHandle);
                if (enableRc == ReturnCode.Success || enableRc == ReturnCode.CheckStatus)
                {
                    return true;
                }

                // Metode 2: Sebagian driver TWAIN (Epson/Canon/HP tertentu) tidak mengimplementasikan ShowUIOnly,
                // melainkan menampilkan UI penuh saat ShowUI
                enableRc = targetSource.Enable(SourceEnableMode.ShowUI, true, _windowHandle);
                if (enableRc == ReturnCode.Success || enableRc == ReturnCode.CheckStatus)
                {
                    return true;
                }

                // Metode 3: Fallback membuka Windows Scanners & Cameras Properties bawaan Windows
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "control.exe",
                        Arguments = "/name Microsoft.ScannersAndCameras",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return true;
                }
                catch { }

                OnErrorOccurred($"Driver scanner tidak dapat menampilkan dialog konfigurasi (Kode: {enableRc}).");
                return false;
            }
            catch (Exception ex)
            {
                // Fallback terakhir: buka panel Scanners and Cameras
                try
                {
                    Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.ScannersAndCameras") { UseShellExecute = true });
                    return true;
                }
                catch { }

                OnErrorOccurred($"Gagal membuka konfigurasi scanner: {ex.Message}");
                return false;
            }
        }

        private void Session_TransferReady(object sender, TransferReadyEventArgs e)
        {
            // Ready to receive image data
            OnStatusChanged("Scanner siap mengirim data gambar...");
        }

        private void Session_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            try
            {
                Image scannedImg = null;

                if (e != null)
                {
                    var stream = e.GetNativeImageStream();
                    if (stream != null)
                    {
                        using (stream)
                        {
                            var ms = new MemoryStream();
                            stream.CopyTo(ms);
                            ms.Position = 0;
                            scannedImg = Image.FromStream(ms);
                        }
                    }
                }

                if (scannedImg != null)
                {
                    OnImageScanned(scannedImg);
                    OnStatusChanged("Scan berhasil selesai.");
                }
                else
                {
                    OnStatusChanged("Scan selesai tanpa tangkapan gambar.");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Gagal memproses hasil gambar: {ex.Message}");
            }
        }

        private void Session_TransferError(object sender, TransferErrorEventArgs e)
        {
            string err = e?.Exception != null ? e.Exception.Message : ("Kode: " + (e != null ? e.ReturnCode.ToString() : "Unknown"));
            OnErrorOccurred($"Error transfer gambar dari scanner: {err}");
        }

        private void Session_SourceDisabled(object sender, EventArgs e)
        {
            try
            {
                if (_session != null && _session.CurrentSource != null)
                {
                    _session.CurrentSource.Close();
                }
            }
            catch { }
        }

        private void OnImageScanned(Image img)
        {
            ImageScanned?.Invoke(this, img);
        }

        private void OnStatusChanged(string msg)
        {
            StatusChanged?.Invoke(this, msg);
        }

        private void OnErrorOccurred(string msg)
        {
            ErrorOccurred?.Invoke(this, msg);
        }

        public void CancelScan()
        {
            try
            {
                if (_session != null && _session.State >= 4 && _session.CurrentSource != null)
                {
                    _session.CurrentSource.Close();
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                try
                {
                    if (_session != null)
                    {
                        if (_session.State >= 4 && _session.CurrentSource != null)
                        {
                            try { _session.CurrentSource.Close(); } catch { }
                        }
                        if (_session.State >= 3)
                        {
                            try { _session.Close(); } catch { }
                        }
                    }
                }
                catch { }
            }
        }
    }
}
