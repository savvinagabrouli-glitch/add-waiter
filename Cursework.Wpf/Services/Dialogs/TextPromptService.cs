using System.Windows;
using Cursework.Wpf.Views.Dialogs;

namespace Cursework.Wpf.Services.Dialogs
{
    public class TextPromptService : ITextPromptService
    {
        public string? Ask(string title, string message, string defaultValue = "")
        {
            var dlg = new TextPromptDialog(title, message, defaultValue)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            return dlg.ShowDialog() == true ? dlg.ResultText : null;
        }
    }
}
