using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AssetPrimitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace AssetProcessor
{
    public class ImageSharpProcessor : BinaryAssetProcessor<ProcessedTexture>
    {
        private static NameValueCollection DefaultArgs = new NameValueCollection() { { "PixelFormat", nameof(Rgba32) }, { nameof(GenerateMipmaps), "true" } };

        private static Func<Stream, NameValueCollection, ProcessedTexture> WithPixelFormat(Func<Stream, NameValueCollection, PixelFormat, ProcessedTexture> f, PixelFormat pixelFormat)
            => (stream, arg) => f(stream, arg, pixelFormat);

        private static new Dictionary<string, Func<Stream, NameValueCollection, ProcessedTexture>> ImageProcessFormats = new Dictionary<string, Func<Stream, NameValueCollection, ProcessedTexture>>()
            {
                { nameof(Rgba32), WithPixelFormat(ProcessImage<Rgba32>, PixelFormat.R8_G8_B8_A8_UNorm) },
                { nameof(L8), WithPixelFormat(ProcessImage<L8>, PixelFormat.R8_UNorm) }
            };

        public unsafe override ProcessedTexture ProcessT(Stream stream, string extension, NameValueCollection args = null)
        {
            args = args ?? DefaultArgs;
            return ImageProcessFormats[args["PixelFormat"]](stream, args);
        }

        private unsafe static ProcessedTexture ProcessImage<T>(Stream stream, NameValueCollection args, PixelFormat pixelFormat) where T : unmanaged, IPixel<T>
        {
            Image<T> image = Image.Load<T>(stream);
            Image<T>[] mipmaps = new Image<T>[] { image };
            int totalSize = image.Width * image.Height * Unsafe.SizeOf<T>();
            bool generateMipmaps = args[nameof(GenerateMipmaps)] == "true";
            if (generateMipmaps)
                mipmaps = GenerateMipmaps(image, out totalSize);

            byte[] allTexData = new byte[totalSize];
            long offset = 0;
            fixed (byte* allTexDataPtr = allTexData)
            {
                foreach (Image<T> mipmap in mipmaps)
                {
                    long mipSize = mipmap.Width * mipmap.Height * sizeof(T);
                    if (!mipmap.TryGetSinglePixelSpan(out Span<T> pixelSpan))
                    {
                        throw new VeldridException("Unable to get image pixelspan.");
                    }
                    fixed (void* pixelPtr = &MemoryMarshal.GetReference(pixelSpan))
                    {
                        Buffer.MemoryCopy(pixelPtr, allTexDataPtr + offset, mipSize, mipSize);
                    }

                    offset += mipSize;
                }
            }

            return new ProcessedTexture(
                    pixelFormat, TextureType.Texture2D,
                    (uint)image.Width, (uint)image.Height, 1,
                    (uint)mipmaps.Length, 1,
                    allTexData);
        }

        // Taken from Veldrid.ImageSharp

        private static Image<T>[] GenerateMipmaps<T>(Image<T> baseImage, out int totalSize) where T : unmanaged, IPixel<T>
        {
            int mipLevelCount = ComputeMipLevels(baseImage.Width, baseImage.Height);
            Image<T>[] mipLevels = new Image<T>[mipLevelCount];
            mipLevels[0] = baseImage;
            totalSize = baseImage.Width * baseImage.Height * Unsafe.SizeOf<T>();
            int i = 1;

            int currentWidth = baseImage.Width;
            int currentHeight = baseImage.Height;
            while (currentWidth != 1 || currentHeight != 1)
            {
                int newWidth = Math.Max(1, currentWidth / 2);
                int newHeight = Math.Max(1, currentHeight / 2);
                Image<T> newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                Debug.Assert(i < mipLevelCount);
                mipLevels[i] = newImage;

                totalSize += newWidth * newHeight * Unsafe.SizeOf<T>();
                i++;
                currentWidth = newWidth;
                currentHeight = newHeight;
            }

            Debug.Assert(i == mipLevelCount);

            return mipLevels;
        }

        public static int ComputeMipLevels(int width, int height)
        {
            return 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
        }
    }
}
