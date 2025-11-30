using Cursework.Domains.Models;
using Cursework.Wpf.Services.QR_Code;
using Cursework.Wpf.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace Cursework.Wpf.ViewModels.Admin
{
    public class TableQrViewModel : ViewModelBase
    {
        private readonly IQrCodeService _qrService;
        private readonly DiningTable _table;

        private ImageSource? _qrImage;
        public ImageSource? QrImage
        {
            get => _qrImage;
            private set => Set(ref _qrImage, value);
        }

        public string Title => $"QR-код стола {_table.Name}";

        public ICommand SaveCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }

        public event EventHandler? RequestClose;

        public TableQrViewModel(IQrCodeService qrService, DiningTable table)
        {
            _qrService = qrService;
            _table = table;

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            PrintCommand = new RelayCommand(async _ => await PrintAsync());
            CloseCommand = new RelayCommand(_ => Close());

            QrImage = _qrService.GenerateQrImage(_table);
        }

        private async Task SaveAsync()
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"table-{_table.Id}-qr.png",
                Filter = "PNG image|*.png"
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                await _qrService.SaveQrPngAsync(_table, dialog.FileName);
            }
        }

        private Task PrintAsync()
        {
            return _qrService.PrintQrAsync(_table);
        }

        private void Close()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
