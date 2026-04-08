using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace POS.Frontend.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task ToggleThemeAsync()
        {
            var script = @"
                var isDark = document.documentElement.classList.contains('dark');
                if (isDark) {
                    document.documentElement.classList.remove('dark');
                    localStorage.theme = 'light';
                } else {
                    document.documentElement.classList.add('dark');
                    localStorage.theme = 'dark';
                }
            ";
            await _jsRuntime.InvokeVoidAsync("eval", script);
        }

        public async Task<bool> IsDarkModeAsync()
        {
            var script = "document.documentElement.classList.contains('dark')";
            return await _jsRuntime.InvokeAsync<bool>("eval", script);
        }
    }
}
