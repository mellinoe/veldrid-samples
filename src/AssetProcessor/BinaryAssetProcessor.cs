using System.Collections.Specialized;
using System.IO;

namespace AssetProcessor
{
    public abstract class BinaryAssetProcessor
    {
        public abstract object Process(Stream stream, string extension, NameValueCollection args = null);
    }

    public abstract class BinaryAssetProcessor<T> : BinaryAssetProcessor
    {
        public override object Process(Stream stream, string extension, NameValueCollection args = null) => ProcessT(stream, extension, args);

        public abstract T ProcessT(Stream stream, string extension, NameValueCollection args = null);
    }
}
