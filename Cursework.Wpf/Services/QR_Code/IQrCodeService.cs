using System.Threading.Tasks;
using System.Windows.Media;
using Cursework.Domains.Models;

namespace Cursework.Wpf.Services.QR_Code
{
    public interface IQrCodeService
    {
        string BuildTableUrl(int tableId);
        string BuildTableUrl(DiningTable table);

        ImageSource GenerateQrImage(int tableId);
        ImageSource GenerateQrImage(DiningTable table);

        Task SaveQrPngAsync(int tableId, string filePath);
        Task SaveQrPngAsync(DiningTable table, string filePath);

        Task PrintQrAsync(int tableId);
        Task PrintQrAsync(DiningTable table);
    }
}
