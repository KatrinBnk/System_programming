using System;
using SysProgLaba1Shared.Models;
using SysProgLaba1Shared.Helpers;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Вспомогательные методы для работы с TSI
    /// </summary>
    public partial class Assembler 
    {
        /// <summary>
        /// Находит символическое имя (метку) в таблице TSI
        /// </summary>
        public SymbolicName? GetSymbolicName(string? labelName)
        {
            if (string.IsNullOrEmpty(labelName))
                return null;
            
            return TSI.Find(n => n.Name.ToUpper() == labelName.ToUpper());
        }

        /// <summary>
        /// Парсит число из строки. Поддерживает:
        /// - Десятичные числа (по умолчанию): "123", "456"
        /// - Шестнадцатеричные числа с суффиксом 'h' или 'H': "1Ah", "FFh", "A0H"
        /// </summary>
        /// <param name="value">Строка с числом</param>
        /// <returns>Целое число</returns>
        /// <exception cref="FormatException">Если формат числа некорректен</exception>
        public int ParseNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("Пустое значение не может быть преобразовано в число");

            // Проверяем, заканчивается ли число на 'h' или 'H'
            if (value.EndsWith("h", StringComparison.OrdinalIgnoreCase))
            {
                // Убираем суффикс 'h' и парсим как шестнадцатеричное
                string hexValue = value.Substring(0, value.Length - 1);
                return Convert.ToInt32(hexValue, 16);
            }
            else
            {
                // Парсим как десятичное число
                return Convert.ToInt32(value, 10);
            }
        }

        /// <summary>
        /// Пытается распарсить число из строки. Поддерживает:
        /// - Десятичные числа (по умолчанию): "123", "456"
        /// - Шестнадцатеричные числа с суффиксом 'h' или 'H': "1Ah", "FFh", "A0H"
        /// </summary>
        /// <param name="value">Строка с числом</param>
        /// <param name="result">Результат парсинга</param>
        /// <returns>true, если парсинг успешен, иначе false</returns>
        public bool TryParseNumber(string value, out int result)
        {
            result = 0;
            
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                result = ParseNumber(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

