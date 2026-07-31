#!/usr/bin/env bash
# Build and install XSP 4.7.1 (required for Mono 6.8; Ubuntu's mono-xsp4 package is incompatible).
set -euo pipefail

if xsp4 --version >/dev/null 2>&1; then
  # Quick smoke test: xsp4 4.7.1 starts without Mono.Security TypeLoadException
  if timeout 2 xsp4 --help >/dev/null 2>&1; then
    echo "xsp4 already installed."
    exit 0
  fi
fi

sudo apt-get install -y git autoconf automake libtool libtool-bin pkg-config libglib2.0-dev

BUILD_DIR="/tmp/xsp-build"
rm -rf "$BUILD_DIR"
git clone --depth 1 --branch 4.7.1 https://github.com/mono/xsp.git "$BUILD_DIR"
cd "$BUILD_DIR"
./autogen.sh --prefix=/usr
make -j"$(nproc)"
sudo make install
echo "XSP 4.7.1 installed."
