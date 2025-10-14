using System.Text;

namespace SysProgLaba1Shared.Helpers
{
    /// <summary>
    /// Статические вспомогательные методы для ассемблера
    /// </summary>
    public static class AssemblerHelper
    {
        /// <summary>
        /// Получает номер регистра из строки (R0 -> 0, R15 -> 15)
        /// </summary>
        public static int GetRegisterNumber(string? register)
        {
            if (string.IsNullOrEmpty(register) || register.Length < 2)
                return 0;
            
            return int.Parse(register.Substring(1));
        }

        /// <summary>
        /// Конвертирует строку в ASCII hex-представление
        /// </summary>
        public static string ConvertToASCII(string text)
        {
            var result = new StringBuilder();
            byte[] textBytes = Encoding.ASCII.GetBytes(text);
            foreach (byte b in textBytes)
            {
                result.Append(b.ToString("X2"));
            }
            return result.ToString();
        }
    }
}

