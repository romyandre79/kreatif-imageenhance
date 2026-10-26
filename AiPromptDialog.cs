using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fotocopy
{
    public class AiPromptDialog : Form
    {
        private ComboBox cbProvider;
        private TextBox txtApiKey;
        private ComboBox cbModel;
        private TextBox txtPrompt;
        private Button btnGenerate;
        private Button btnCancel;
        private Button btnApply;
        private PictureBox picResult;
        private Label lblStatus;
        private Panel pnlHeader;
        private AiConfig _config;

        public Image GeneratedImage { get; private set; }

        public AiPromptDialog()
        {
            _config = AiConfig.Load();
            InitializeComponent();
            LoadConfigToUi();
        }

        private ComboBox cbCategory;
        private FlowLayoutPanel flowQuick;

        private void InitializeComponent()
        {
            this.Text = "✨ AI Generator & Agent (Gemini, OpenAI, Pollinations)";
            this.Size = new Size(880, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            // Header Banner
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(79, 70, 229),
                Padding = new Padding(16, 12, 16, 12)
            };
            var lblTitle = new Label
            {
                Text = "✨ AI Image Generator & Agent",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 8)
            };
            var lblSubtitle = new Label
            {
                Text = "Ketik atau pilih template prompt AI untuk pasfoto, dokumen, kop surat, stempel, brosur, atau kartu nama",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(224, 231, 255),
                AutoSize = true,
                Location = new Point(17, 34)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);

            // Left Panel (Input Settings & Prompt)
            var pnlLeft = new Panel
            {
                Location = new Point(16, 75),
                Size = new Size(420, 530)
            };

            // Provider selection
            var lblProv = new Label { Text = "Penyedia AI (Provider):", Location = new Point(0, 0), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F) };
            cbProvider = new ComboBox
            {
                Location = new Point(0, 20),
                Size = new Size(420, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbProvider.Items.AddRange(new object[] {
                "Pollinations.ai (Free - No Key)",
                "OpenAI (DALL-E 3)",
                "Google Gemini (Imagen 3)"
            });
            cbProvider.SelectedIndex = 0;
            cbProvider.SelectedIndexChanged += CbProvider_SelectedIndexChanged;

            // API Key
            var lblKey = new Label { Text = "API Key (Diperlukan untuk OpenAI & Google Gemini):", Location = new Point(0, 52), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F) };
            txtApiKey = new TextBox
            {
                Location = new Point(0, 72),
                Size = new Size(420, 26),
                PasswordChar = '•'
            };
            txtApiKey.TextChanged += TxtApiKey_TextChanged;

            // Model selection
            var lblMod = new Label { Text = "Model AI (Pilih model generator gambar):", Location = new Point(0, 103), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F) };
            cbModel = new ComboBox
            {
                Location = new Point(0, 123),
                Size = new Size(330, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var btnFetchModels = new Button
            {
                Text = "🔄 Ambil Semua Model",
                Location = new Point(335, 122),
                Size = new Size(85, 29),
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Color.FromArgb(67, 56, 202),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFetchModels.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnFetchModels.Click += async (s, e) => await FetchLiveModelsAsync();

            // Preset Category Selector
            var lblCat = new Label { Text = "📂 Kategori Preset Prompt AI:", Location = new Point(0, 155), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(67, 56, 202) };
            cbCategory = new ComboBox
            {
                Location = new Point(0, 175),
                Size = new Size(420, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbCategory.Items.AddRange(new object[] {
                "📷 Pasfoto KTP / Ijazah / SKCK",
                "📄 Dokumen & Kop Surat / Piagam",
                "💮 Stempel, Cap & Logo Toko",
                "📢 Brosur, Pamflet & Flyer Promosi",
                "💳 Kartu Nama & ID Card"
            });
            cbCategory.SelectedIndex = 0;
            cbCategory.SelectedIndexChanged += CbCategory_SelectedIndexChanged;

            // Flow panel for preset chips
            var lblChips = new Label { Text = "Klik template cepat di bawah untuk mengisi prompt:", Location = new Point(0, 207), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Segoe UI", 8F) };
            flowQuick = new FlowLayoutPanel
            {
                Location = new Point(0, 226),
                Size = new Size(420, 68),
                AutoScroll = true,
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(3)
            };
            PopulatePresetChips(0);

            // Prompt Box
            var lblPrm = new Label { Text = "Teks Prompt AI (Bisa diedit sesuka hati):", Location = new Point(0, 300), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F) };
            txtPrompt = new TextBox
            {
                Location = new Point(0, 321),
                Size = new Size(420, 140),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5F)
            };

            // Tombol Generate
            btnGenerate = new Button
            {
                Text = "🚀 Generate Gambar Sekarang",
                Location = new Point(0, 472),
                Size = new Size(420, 44),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += BtnGenerate_Click;

            pnlLeft.Controls.Add(lblProv);
            pnlLeft.Controls.Add(cbProvider);
            pnlLeft.Controls.Add(lblKey);
            pnlLeft.Controls.Add(txtApiKey);
            pnlLeft.Controls.Add(lblMod);
            pnlLeft.Controls.Add(cbModel);
            pnlLeft.Controls.Add(btnFetchModels);
            pnlLeft.Controls.Add(lblCat);
            pnlLeft.Controls.Add(cbCategory);
            pnlLeft.Controls.Add(lblChips);
            pnlLeft.Controls.Add(flowQuick);
            pnlLeft.Controls.Add(lblPrm);
            pnlLeft.Controls.Add(txtPrompt);
            pnlLeft.Controls.Add(btnGenerate);

            this.Controls.Add(pnlLeft);

            // Right Panel (Preview Result)
            var gbPreview = new GroupBox
            {
                Text = "Hasil Generate AI",
                Location = new Point(450, 75),
                Size = new Size(400, 526),
                Font = new Font("Segoe UI Semibold", 9F)
            };

            picResult = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle
            };
            gbPreview.Controls.Add(picResult);
            this.Controls.Add(gbPreview);

            // Status Bar & Bottom Buttons
            lblStatus = new Label
            {
                Text = "Siap membuat gambar dengan AI.",
                Location = new Point(16, 620),
                Size = new Size(450, 25),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            this.Controls.Add(lblStatus);

            btnCancel = new Button
            {
                Text = "Batal",
                Location = new Point(590, 615),
                Size = new Size(110, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            btnApply = new Button
            {
                Text = "✓ Muat ke Aplikasi",
                Location = new Point(710, 615),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnApply.Click += (s, e) =>
            {
                if (picResult.Image != null)
                {
                    GeneratedImage = new Bitmap(picResult.Image);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
            this.Controls.Add(btnApply);
        }

        private void CbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulatePresetChips(cbCategory.SelectedIndex);
        }

        private void PopulatePresetChips(int categoryIndex)
        {
            flowQuick.Controls.Clear();
            switch (categoryIndex)
            {
                case 0: // Pasfoto KTP / Ijazah / SKCK
                    AddPromptChip(flowQuick, "Latar Merah Formal (Pria)", "Pasfoto resmi formal pria Indonesia mengenakan kemeja putih berdasi dan jas hitam, latar belakang merah polos studio rapi tajam, pencahayaan merata, pas foto 4x6 untuk ijazah dan lamaran kerja.");
                    AddPromptChip(flowQuick, "Latar Biru Formal (Wanita)", "Pasfoto resmi formal wanita Indonesia mengenakan kemeja putih dan blazer hitam, latar belakang biru polos bersih, pencahayaan studio studio lighting jernih tajam, pasfoto KTP dan paspor.");
                    AddPromptChip(flowQuick, "Pasfoto Berhijab Rapi", "Foto formal pasfoto wanita muslimah Indonesia berhijab hitam rapi, kemeja putih, latar belakang merah terang, kualitas foto studio studio lighting hd.");
                    AddPromptChip(flowQuick, "Pasfoto Jas + Dasi Merah", "Studio portrait ID photo, formal Asian man in sleek dark navy suit with red necktie and crisp white collar, solid background, high definition passport photo.");
                    break;

                case 1: // Dokumen & Kop Surat / Piagam
                    AddPromptChip(flowQuick, "Bingkai Sertifikat Emas", "Desain template bingkai sertifikat piagam penghargaan elegan, border ornamen ukir emas mewah (golden luxury ornament frame), latar belakang kertas bertekstur krem lembut bersih.");
                    AddPromptChip(flowQuick, "Header Kop Surat Modern", "Clean modern business letterhead header design, geometric corporate wave header footer banner in blue and silver colors, white background, minimalist clean layout.");
                    AddPromptChip(flowQuick, "Watermark Rahasia / Confidential", "Subtle light gray transparent official security guilloche pattern background with subtle watermark for legal certificate document.");
                    AddPromptChip(flowQuick, "Piagam Floral Hijau Islami", "Desain piagam penghargaan islami kaligrafi ornamen hijau dan emas, border elegan berkelas untuk piagam pengajian dan tahfidz.");
                    break;

                case 2: // Stempel, Cap & Logo Toko
                    AddPromptChip(flowQuick, "Stempel Toko Fotocopy", "Desain stempel toko cap bulat vector 'FOTOCOPY & ATK SEJAHTERA', ada bintang melingkar di pinggir, teks melengkung rapi, monochrome hitam putih tajam pada background putih.");
                    AddPromptChip(flowQuick, "Stempel LUNAS Merah", "Cap stempel kotak 'LUNAS / PAID' bergaya grunge stempel karet merah retro resmi, tajam di atas latar putih bersih.");
                    AddPromptChip(flowQuick, "Logo Percetakan Digital", "Modern minimalist logo icon for creative printing and copy center, CMYK colors palette, sleek vector graphic, white background.");
                    AddPromptChip(flowQuick, "Stempel Legalitas Oval", "Cap stempel oval resmi kedinasan instansi kantor, ornamen garis ganda, teks rapi simetris di atas background putih polos.");
                    break;

                case 3: // Brosur, Pamflet & Flyer Promosi
                    AddPromptChip(flowQuick, "Brosur Toko Fotocopy & Jilid", "Desain brosur promosi jasa Fotocopy, Print Warna, Penjilidan Skripsi, dan ATK, layout modern eye-catching, warna biru oranye cerah, ruang teks luas, clean flyer design.");
                    AddPromptChip(flowQuick, "Flyer Promo Diskon Pelajar", "Vibrant promotional sales flyer design, bold typography 'PROMO SPESIAL', discounts celebration elements, colorful modern marketing background.");
                    AddPromptChip(flowQuick, "Pamflet Seminar / Pelatihan", "Corporate business training workshop banner flyer, clean aesthetic, dark blue and gold tones, modern professional layout.");
                    AddPromptChip(flowQuick, "Daftar Harga Jasa Print (Pricelist)", "Clean and neat menu price list template banner for copy and print shop services, organized columns, modern minimalist typography.");
                    break;

                case 4: // Kartu Nama & ID Card
                    AddPromptChip(flowQuick, "Kartu Nama Usaha Fotocopy", "Modern business card template for Printing & Stationery shop, clean minimalist front design, blue and dark grey corporate colors, mockup layout.");
                    AddPromptChip(flowQuick, "ID Card Pegawai / Karyawan", "Official vertical employee ID card badge template, sleek corporate design with photo frame placeholder, QR code area, lanyard clip top.");
                    AddPromptChip(flowQuick, "Kartu Nama Mewah Gold Foil", "Luxury black and gold business card design, subtle texture background, premium golden typography accents, elegant minimalist.");
                    AddPromptChip(flowQuick, "Kartu Anggota / Member Card", "VIP Customer membership reward card design, holographic gradient background pattern, modern badge styling.");
                    break;
            }
        }

        private void AddPromptChip(FlowLayoutPanel panel, string label, string prompt)
        {
            var btn = new Button
            {
                Text = label,
                AutoSize = true,
                Height = 27,
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Color.FromArgb(67, 56, 202),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F),
                Margin = new Padding(3),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (s, e) =>
            {
                txtPrompt.Text = prompt;
                txtPrompt.Focus();
                txtPrompt.Select(txtPrompt.Text.Length, 0);
            };
            panel.Controls.Add(btn);
        }

        private void TxtApiKey_TextChanged(object sender, EventArgs e)
        {
            RefreshModelList();
        }

        private void CbProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = cbProvider.SelectedItem?.ToString() ?? "";

            if (selected.StartsWith("Pollinations", StringComparison.OrdinalIgnoreCase))
            {
                txtApiKey.Enabled = false;
                txtApiKey.Text = "(Tidak perlu API Key)";
            }
            else
            {
                txtApiKey.Enabled = true;
                if (txtApiKey.Text == "(Tidak perlu API Key)")
                {
                    txtApiKey.Text = _config.ApiKey ?? "";
                }
            }

            RefreshModelList();
        }

        private void RefreshModelList()
        {
            string selectedProvider = cbProvider.SelectedItem?.ToString() ?? "";
            string currentModel = cbModel.SelectedItem?.ToString();

            cbModel.Items.Clear();

            if (selectedProvider.StartsWith("Pollinations", StringComparison.OrdinalIgnoreCase))
            {
                cbModel.Enabled = true;
                cbModel.Items.AddRange(new object[] {
                    "flux (Kualitas Tinggi)",
                    "turbo (Cepat)",
                    "midjourney (Artistik)"
                });

                SelectMatchingModel(currentModel ?? _config.ImageModel, 0);
            }
            else if (selectedProvider.StartsWith("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                string key = txtApiKey.Text.Trim();
                bool hasKey = !string.IsNullOrWhiteSpace(key) && key != "(Tidak perlu API Key)";

                if (!hasKey)
                {
                    cbModel.Items.Add("⚠️ Masukkan API Key OpenAI terlebih dahulu");
                    cbModel.SelectedIndex = 0;
                    cbModel.Enabled = false;
                }
                else
                {
                    cbModel.Enabled = true;
                    cbModel.Items.AddRange(new object[] {
                        "dall-e-3 (DALL-E 3 - 1024x1024 High Res)",
                        "dall-e-2 (DALL-E 2 - Cepat & Ekonomis)",
                        "gpt-4o (Multimodal Vision / Image)",
                        "gpt-4o-mini (Multimodal Mini)"
                    });

                    SelectMatchingModel(currentModel ?? _config.ImageModel, 0);
                }
            }
            else if (selectedProvider.StartsWith("Google", StringComparison.OrdinalIgnoreCase) || selectedProvider.StartsWith("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                string key = txtApiKey.Text.Trim();
                bool hasKey = !string.IsNullOrWhiteSpace(key) && key != "(Tidak perlu API Key)";

                if (!hasKey)
                {
                    cbModel.Items.Add("⚠️ Masukkan API Key Gemini terlebih dahulu");
                    cbModel.SelectedIndex = 0;
                    cbModel.Enabled = false;
                }
                else
                {
                    cbModel.Enabled = true;
                    cbModel.Items.AddRange(new object[] {
                        "gemini-2.0-flash-exp (Gemini 2.0 Multimodal - Support Free Key)",
                        "gemini-1.5-flash (Gemini 1.5 Flash)",
                        "gemini-1.5-pro (Gemini 1.5 Pro)",
                        "imagen-3.0-generate-002 (Imagen 3 Standar - Paid Tier)",
                        "imagen-3.0-fast-generate-001 (Imagen 3 Cepat - Paid Tier)"
                    });

                    SelectMatchingModel(currentModel ?? _config.ImageModel, 0);
                }
            }
        }

        private async Task FetchLiveModelsAsync()
        {
            string provider = cbProvider.SelectedItem?.ToString() ?? "";
            string key = txtApiKey.Text.Trim();

            if (provider.StartsWith("Pollinations", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Provider Pollinations gratis tidak memerlukan sinkronisasi API Key.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(key) || key == "(Tidak perlu API Key)")
            {
                MessageBox.Show("Silakan masukkan API Key Anda terlebih dahulu untuk mengambil daftar model yang aktif pada akun Anda.", "API Key Diperlukan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtApiKey.Focus();
                return;
            }

            lblStatus.Text = "⏳ Mengambil seluruh daftar model langsung dari API server...";
            lblStatus.ForeColor = Color.FromArgb(79, 70, 229);
            Cursor = Cursors.WaitCursor;

            try
            {
                var models = await Task.Run(() => AiAgentService.FetchAvailableModelsAsync(provider, key));
                if (models != null && models.Count > 0)
                {
                    cbModel.Items.Clear();
                    foreach (var m in models)
                    {
                        cbModel.Items.Add(m);
                    }
                    cbModel.Enabled = true;
                    cbModel.SelectedIndex = 0;
                    lblStatus.Text = $"✅ Berhasil memuat {models.Count} model langsung dari akun Anda!";
                    lblStatus.ForeColor = Color.FromArgb(22, 101, 52);
                }
                else
                {
                    lblStatus.Text = "ℹ️ Tidak ada model tambahan yang terdeteksi, menggunakan daftar model bawaan.";
                    lblStatus.ForeColor = Color.FromArgb(71, 85, 105);
                    RefreshModelList();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ Gagal mengambil daftar model: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void SelectMatchingModel(string targetModel, int defaultIndex = 0)
        {
            if (cbModel.Items.Count == 0) return;

            if (!string.IsNullOrEmpty(targetModel))
            {
                string cleanTarget = targetModel.Split(' ')[0].Trim().ToLowerInvariant();
                for (int i = 0; i < cbModel.Items.Count; i++)
                {
                    string itemText = cbModel.Items[i].ToString().ToLowerInvariant();
                    if (itemText.StartsWith(cleanTarget) || itemText.Contains(cleanTarget))
                    {
                        cbModel.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (defaultIndex >= 0 && defaultIndex < cbModel.Items.Count)
                cbModel.SelectedIndex = defaultIndex;
        }

        private void LoadConfigToUi()
        {
            if (!string.IsNullOrEmpty(_config.Provider))
            {
                for (int i = 0; i < cbProvider.Items.Count; i++)
                {
                    if (cbProvider.Items[i].ToString().StartsWith(_config.Provider.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
                    {
                        cbProvider.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (cbProvider.SelectedIndex != 0 && !string.IsNullOrEmpty(_config.ApiKey))
            {
                txtApiKey.Text = _config.ApiKey;
            }

            RefreshModelList();
            SelectMatchingModel(_config.ImageModel, 0);
        }

        private void SaveUiToConfig()
        {
            _config.Provider = cbProvider.SelectedItem?.ToString() ?? "Pollinations.ai (Free - No Key)";
            if (txtApiKey.Enabled && txtApiKey.Text != "(Tidak perlu API Key)")
            {
                _config.ApiKey = txtApiKey.Text.Trim();
            }

            string rawModel = cbModel.SelectedItem?.ToString() ?? "flux";
            // Ambil identifier model utama sebelum tanda kurung deskripsi
            string cleanModel = rawModel.Split(' ')[0].Trim();
            if (!cleanModel.StartsWith("⚠️"))
            {
                _config.ImageModel = cleanModel;
            }

            _config.Save();
        }

        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            string prompt = txtPrompt.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("Silakan masukkan teks prompt terlebih dahulu.", "Prompt Kosong", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!cbModel.Enabled || cbModel.SelectedItem == null || cbModel.SelectedItem.ToString().StartsWith("⚠️"))
            {
                MessageBox.Show("Silakan masukkan API Key yang valid terlebih dahulu agar model AI dapat aktif.", "API Key Belum Diisi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtApiKey.Focus();
                return;
            }

            SaveUiToConfig();

            btnGenerate.Enabled = false;
            btnApply.Enabled = false;
            Cursor = Cursors.WaitCursor;
            lblStatus.Text = "⏳ Sedang menghubungi AI Agent untuk membuat foto...";
            lblStatus.ForeColor = Color.FromArgb(79, 70, 229);

            try
            {
                byte[] imgBytes = await Task.Run(() => AiAgentService.GenerateImageAsync(prompt, _config));

                using (var ms = new MemoryStream(imgBytes))
                {
                    picResult.Image?.Dispose();
                    picResult.Image = new Bitmap(ms);
                }

                btnApply.Enabled = true;
                lblStatus.Text = "✅ Gambar berhasil di-generate! Klik '✓ Muat ke Aplikasi'.";
                lblStatus.ForeColor = Color.FromArgb(22, 101, 52);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ Gagal menghasilkan gambar.";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show($"Terjadi kesalahan saat memproses gambar AI:\n{ex.Message}", "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
    }
}
