#!/usr/bin/env bash
# Copyright (c) 2026 popovantondev. This build script alone: LGPL-2.1-or-later.
set -euo pipefail
export PATH="/usr/bin:$PATH"
cd "$(dirname "$0")"
root="$PWD"
export PATH="${YTD_TOOLCHAIN_PATH:-$root/llvm-mingw-20240619-msvcrt-x86_64}/bin:$PATH"
prefix="$root/prefix"
mkdir -p build evidence "$prefix"
export CC=i686-w64-mingw32-clang CXX=i686-w64-mingw32-clang++ AR=llvm-ar RANLIB=llvm-ranlib
export CFLAGS='-O2 -msse2 -D_WIN32_WINNT=0x0601 -DWINVER=0x0601'
export LDFLAGS='-static -static-libgcc'
export PKG_CONFIG_PATH="$prefix/lib/pkgconfig"
export PKG_CONFIG_LIBDIR="$PKG_CONFIG_PATH"
printf '%s  %s\n' f4fa066278f7a47feab316fef905f4db0d5e9b589451949740f83972b30901bd sources/ffmpeg-5.1.6.tar.xz ddfe36cab873794038ae2c1210557ad34857a4b6bdc515785d1da9e175b1da1e sources/lame-3.100.tar.gz | sha256sum -c -
"$CC" --version > evidence/compiler.txt
if [[ ! -d build/lame-3.100 ]]; then tar -xf sources/lame-3.100.tar.gz -C build; fi
cd build/lame-3.100
if [[ ! -f "$prefix/lib/libmp3lame.a" ]]; then
./configure --host=i686-w64-mingw32 --prefix="$prefix" --disable-shared --enable-static --disable-frontend --disable-decoder --disable-nasm --without-libiconv > "$root/evidence/lame-configure.txt" 2>&1
make -j4 > "$root/evidence/lame-make.txt" 2>&1
make install >> "$root/evidence/lame-make.txt" 2>&1
fi
cd "$root"
if [[ ! -d build/ffmpeg-5.1.6 ]]; then tar -xf sources/ffmpeg-5.1.6.tar.xz -C build; fi
cd build/ffmpeg-5.1.6
./configure --prefix=/YouTubeDownloader-build --arch=x86 --cpu=i686 --target-os=mingw32 --enable-cross-compile \
 --cc="$CC" --cxx="$CXX" --ar="$AR" --ranlib="$RANLIB" --strip=llvm-strip --nm=llvm-nm \
 --pkg-config=pkg-config --pkg-config-flags=--static \
 --extra-cflags="$CFLAGS -I../../prefix/include" --extra-ldflags="$LDFLAGS -L../../prefix/lib" \
 --disable-autodetect --disable-everything --disable-network --disable-shared --enable-static \
 --disable-doc --disable-debug --disable-x86asm --disable-asm --disable-ffplay --enable-ffmpeg --enable-ffprobe \
 --disable-w32threads --enable-pthreads --enable-libmp3lame \
 --enable-avcodec --enable-avformat --enable-avfilter --enable-swresample --enable-swscale \
 --enable-protocol=file,pipe,concat --enable-demuxer=mov,matroska,mp3,aac,ogg,wav,flac,concat,h264,ivf,obu \
 --enable-muxer=mp4,ipod,matroska,mp3,adts,null,wav,pcm_f32le,pcm_s16le \
 --enable-decoder=flac,aac,aac_fixed,aac_latm,opus,mp3,mp3float,pcm_s16le,pcm_s24le,pcm_s32le,pcm_f32le,pcm_f64le,h264,vp9 \
 --enable-encoder=libmp3lame,pcm_s16le,pcm_s24le,pcm_f32le \
 --enable-parser=flac,aac,aac_latm,mpegaudio,opus,h264,vp9,av1 \
 --enable-filter=aresample,anull,aformat,atrim,asetpts,concat,astats,volume,acompressor,alimiter,asplit,amerge,amix \
 --enable-bsf=aac_adtstoasc,extract_extradata,h264_mp4toannexb,av1_frame_merge > "$root/evidence/ffmpeg-configure.txt" 2>&1
cp config.h ffbuild/config.mak ffbuild/config.log "$root/evidence/"
make -j4 > "$root/evidence/ffmpeg-make.txt" 2>&1
mkdir -p "$prefix/bin"
cp ffmpeg.exe ffprobe.exe "$prefix/bin/"
"$prefix/bin/ffmpeg.exe" -version > "$root/evidence/ffmpeg-version.txt" 2>&1
"$prefix/bin/ffprobe.exe" -version > "$root/evidence/ffprobe-version.txt" 2>&1
echo BUILD_COMPLETE
