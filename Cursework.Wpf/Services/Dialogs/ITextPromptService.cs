using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cursework.Wpf.Services.Dialogs
{
    public interface ITextPromptService
    {
        string? Ask(string title, string message, string defaultValue = "");
    }
}
