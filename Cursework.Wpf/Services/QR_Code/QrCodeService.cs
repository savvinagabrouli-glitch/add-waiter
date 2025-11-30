using Cursework.Domains.Models;
using Cursework.Wpf.Options;
using Cursework.Wpf.Services.QR_Code;
using Microsoft.Extensions.Options;
using QRCoder;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cursework.Wpf.Services.QR_Code
{
    public class QrCodeService : IQrCodeService
    {
        private readonly string _baseUrl;

        public QrCodeService(IOptions<GuestSiteOptions> options)
        {
            _baseUrl = options.Value.BaseUrl?.TrimEnd('/')
                ?? throw new ArgumentNullException(nameof(options));
        }

        public string BuildTableUrl(int tableId)
            => $"{_baseUrl}/menu?tableId={tableId}";

        public string BuildTableUrl(DiningTable table)
            => BuildTableUrl(table.Id);

        public ImageSource GenerateQrImage(int tableId)
        {
            var url = BuildTableUrl(tableId);
            var bytes = GenerateQrPngBytes(url);
            return CreateBitmapImage(bytes);
        }

        public ImageSource GenerateQrImage(DiningTable table)
            => GenerateQrImage(table.Id);

        public async Task SaveQrPngAsync(int tableId, string filePath)
        {
            var url = BuildTableUrl(tableId);
            var bytes = GenerateQrPngBytes(url);
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        public Task SaveQrPngAsync(DiningTable table, string filePath)
            => SaveQrPngAsync(table.Id, filePath);

        public async Task PrintQrAsync(int tableId)
        {
            var imageSource = GenerateQrImage(tableId) as BitmapSource;
            if (imageSource == null)
                return;

            var printDialog = new PrintDialog();
            var result = printDialog.ShowDialog();
            if (result != true)
                return;

            const double size = 256;

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(imageSource, new Rect(0, 0, size, size));
            }

            printDialog.PrintVisual(visual, $"QR стол {tableId}");
            await Task.CompletedTask;
        }

        public Task PrintQrAsync(DiningTable table)
            => PrintQrAsync(table.Id);

        private static byte[] GenerateQrPngBytes(string url)
        {
            using var generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(data);
            return qrCode.GetGraphic(20);
        }

        private static BitmapImage CreateBitmapImage(byte[] pngBytes)
        {
            var bitmap = new BitmapImage();
            using var stream = new MemoryStream(pngBytes);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
