using System;
using System.IO;
using System.Threading.Tasks;

namespace Azure.Api.Data.DTOs.Document
{
    public class DownloadDocumentDTO : IDisposable
    {
        public int HttpResponseCode { get; set; }
        public string HttpResponseMessage { get; set; }

        public MemoryStream StreamContent { get; set; }
        public string FileExtension { get; set; }
        public string FileName { get; set; }

        public void Dispose()
        {
            StreamContent?.Dispose();
        }

        public async Task DisposeAsync()
        {
            if (StreamContent != null)
            {
                await StreamContent.DisposeAsync();
            }
        }
    }
}
