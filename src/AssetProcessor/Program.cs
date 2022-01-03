using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using AssetPrimitives;

namespace AssetProcessor
{
    class Program
    {
        private static readonly Dictionary<string, BinaryAssetProcessor> s_assetProcessors = GetAssetProcessors();
        private static readonly Dictionary<Type, BinaryAssetSerializer> s_assetSerializers = DefaultSerializers.Get();

        private static Dictionary<string, BinaryAssetProcessor> GetAssetProcessors()
        {
            ImageSharpProcessor texProcessor = new ImageSharpProcessor();
            StbImageProcessor stbImageProcessor = new StbImageProcessor();
            KtxFileProcessor ktxFileProcessor = new KtxFileProcessor();
            AssimpProcessor assimpProcessor = new AssimpProcessor();

            return new Dictionary<string, BinaryAssetProcessor>()
            {
                { ".png", texProcessor },
                { ".hdr", stbImageProcessor },
                { ".ktx", ktxFileProcessor },
                { ".dae", assimpProcessor },
                { ".obj", assimpProcessor },
                { ".fbx", assimpProcessor }
            };
        }

        private static NameValueCollection ParseArgs(string args)
            => HttpUtility.ParseQueryString(args);

        static int Main(string[] args)
        {
            string outputDirectory = args[0];
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                var assetParts = arg.Split("::", 2);
                var path = assetParts[0];
                var processorArgs = assetParts.Length > 1 && !string.IsNullOrWhiteSpace(assetParts[1]) ? ParseArgs(assetParts[1]) : null;

                Console.WriteLine($"Processing {path}");
                if (processorArgs != null)
                    Console.WriteLine($"\t with args {processorArgs}");

                string extension = Path.GetExtension(path);
                if (string.IsNullOrEmpty(extension))
                {
                    Console.Error.WriteLine($"Invalid path: {path}");
                    return -1;
                }

                if (!s_assetProcessors.TryGetValue(extension, out BinaryAssetProcessor processor))
                {
                    Console.Error.WriteLine($"Unable to process asset with extension {extension}.");
                    return -1;
                }

                object processedAsset;
                using (FileStream fs = File.OpenRead(path))
                {
                    processedAsset = processor.Process(fs, extension, processorArgs);
                }

                Type assetType = processedAsset.GetType();
                if (!s_assetSerializers.TryGetValue(assetType, out BinaryAssetSerializer serializer))
                {
                    Console.Error.WriteLine($"Unable to serialize asset of type {assetType}.");
                    return -1;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                string outputFileName = Path.Combine(outputDirectory, fileName + ".binary");
                using (FileStream outFS = File.Create(outputFileName))
                {
                    BinaryWriter writer = new BinaryWriter(outFS);
                    serializer.Write(writer, processedAsset);
                }
                Console.WriteLine($"Processed asset: {arg} => {outputFileName}");
            }

            return 0;
        }
    }
}
