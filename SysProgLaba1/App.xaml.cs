using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace SysProgLaba1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ApplySystemTheme();
        }

        private void ApplySystemTheme()
        {
            bool isDarkTheme = IsSystemUsingDarkTheme();
            
            var themeDict = Resources[isDarkTheme ? "DarkTheme" : "LightTheme"] as ResourceDictionary;
            
            if (themeDict != null)
            {
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(themeDict);
            }
        }

        private bool IsSystemUsingDarkTheme()
        {
            try
            {
                const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string valueName = "AppsUseLightTheme";

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(valueName);
                        if (value != null && value is int intValue)
                        {
                            // 0 = темная тема, 1 = светлая тема
                            return intValue == 0;
                        }
                    }
                }
            }
            catch
            {
                // Если не удалось определить, используем светлую тему по умолчанию
            }

            return false; // По умолчанию светлая тема
        }
    }
}

