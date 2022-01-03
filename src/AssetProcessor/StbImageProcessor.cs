using System;
using System.Collections.Specialized;
using System.IO;
using AssetPrimitives;
using StbImageSharp;
using Veldrid;

namespace AssetProcessor
{
    public class StbImageProcessor : BinaryAssetProcessor<ProcessedTexture>
    {
        private ColorComponents ParseColorComponents(string arg) => Enum.Parse<ColorComponents>(arg);
        private PixelFormat SelectPixelFormat(ColorComponents components)
        {
            switch (components)
            {
                case ColorComponents.RedGreenBlueAlpha:
                case ColorComponents.RedGreenBlue:
                    return PixelFormat.R8_G8_B8_A8_UNorm;
                case ColorComponents.Grey:
                    return PixelFormat.R8_UNorm;
                case ColorComponents.GreyAlpha:
                    return PixelFormat.R8_G8_UNorm;
                default:
                    throw new NotImplementedException($"{nameof(SelectPixelFormat)} not implemented for {nameof(components)} {components}");
            }
        }

        private static NameValueCollection DefaultArgs = new NameValueCollection() { { nameof(ColorComponents), "Default" } };

        public unsafe override ProcessedTexture ProcessT(Stream stream, string extension, NameValueCollection args = null)
        {
            args = args ?? DefaultArgs;
            var components = ParseColorComponents(args[nameof(ColorComponents)]);
            var imageResult = ImageResult.FromStream(stream, components);

            var data = imageResult.Data;
            if (imageResult.Comp == ColorComponents.RedGreenBlue)
            {
                Array.Resize(ref data, data.Length + (imageResult.Width * imageResult.Height));
                for (int i = (imageResult.Width * imageResult.Height) - 1; i >= 0; i--)
                {
                    data[(i * 4) + 3] = 255;
                    data[(i * 4) + 2] = data[(i * 3) + 2];
                    data[(i * 4) + 1] = data[(i * 3) + 1];
                    data[(i * 4) + 0] = data[(i * 3) + 0];
                }
            }

            ProcessedTexture texData = new ProcessedTexture(
                    SelectPixelFormat(imageResult.Comp), TextureType.Texture2D,
                    (uint)imageResult.Width, (uint)imageResult.Height, 1,
                    1, 1,
                    data);
            return texData;
        }
    }
}
