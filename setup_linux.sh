#!/bin/bash
set -e

echo "=== Memasang dependensi Linux untuk Fotocopy Desktop ==="

# 1. Update paket & install utilitas SANE, CUPS, Python
sudo apt update
sudo apt install -y sane-utils cups-client python3 python3-pip python3-pil

# 2. Pasang library AI rembg dengan dukungan CPU (onnxruntime)
echo "Memasang modul AI rembg[cpu]..."
pip install "rembg[cpu]" --break-system-packages 2>/dev/null || pip3 install "rembg[cpu]"

echo ""
echo "=== Instalasi selesai! ==="
echo "Jalankan aplikasi dengan: ./Fotocopy.Desktop"
