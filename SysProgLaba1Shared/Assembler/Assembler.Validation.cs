using System;
using System.Linq;
using System.Text.RegularExpressions;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Helpers;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Методы валидации для ассемблера
    /// </summary>
    public partial class Assembler 
    {
        // ========== Простые проверки (Is*) - возвращают bool ==========
        
        public bool IsCommand(string? chunk)
        {
            if(chunk == null) return false;  
            return AvailibleCommands.Select(c => c.Name.ToUpper()).Contains(chunk.ToUpper()); 
        } 

        public bool IsDirective(string? chunk)
        {
            if (chunk == null) return false; 
            return AvailibleDirectives.Contains(chunk.ToUpper());
        }

        public bool IsRegister(string? chunk)
        {
            if (chunk == null) return false; 
            // Регистры R0-R15 (16 регистров)
            return Regex.IsMatch(chunk, @"^R(?:[0-9]|1[0-5])$");
        }

        public bool IsLabel(string? chunk)
        {
            if(chunk == null) return false;

            // Метка: начинается с буквы, содержит буквы, цифры, подчеркивание, максимум 10 символов
            if (!Regex.IsMatch(chunk, @"^[a-zA-Z][a-zA-Z0-9_]{0,9}$")) 
                return false;

            // Не должна быть регистром
            if (IsRegister(chunk.ToUpper())) return false; 

            // Не должна совпадать с командами или директивами
            if (IsCommand(chunk) || IsDirective(chunk)) 
                return false;

            return true; 
        }

        public bool IsXString(string? chunk)
        {
            if (chunk == null) return false;
            // Hex-строка: X"<hex>" или x"<hex>", где hex - четное количество hex-символов (минимум 2)
            return Regex.IsMatch(chunk, @"^[Xx]""([0-9A-Fa-f]{2})+""$");
        }

        public bool IsCString(string? chunk)
        {
            if (chunk == null) return false;

            // Символьная строка: C"<text>" или c"<text>", где text - ASCII символы (не пустая)
            if (!Regex.IsMatch(chunk, @"^[Cc]"".+""$"))
                return false;

            // Проверяем, что все символы внутри кавычек - ASCII (0-127)
            string content = chunk.Substring(2, chunk.Length - 3);
            return content.All(c => c <= 127);
        }

        // ========== Валидация с детальными ошибками (используют ErrorFormatter) ==========

        /// <summary>
        /// Валидирует метку и выбрасывает исключение с подробным описанием из ErrorFormatter
        /// </summary>
        public void ValidateLabel(string? chunk, int lineNumber, string code)
        {
            if (chunk == null)
                throw new AssemblerException(ErrorFormatter.Format(lineNumber, "Отсутствует метка.", code));

            if (chunk.Length > 10)
                throw new AssemblerException(ErrorFormatter.LabelTooLong(lineNumber, chunk, code));

            if (!Regex.IsMatch(chunk, @"^[a-zA-Z]"))
                throw new AssemblerException(ErrorFormatter.LabelInvalidStart(lineNumber, chunk, code));

            if (!Regex.IsMatch(chunk, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                throw new AssemblerException(ErrorFormatter.LabelInvalidChars(lineNumber, chunk, code));

            if (IsRegister(chunk.ToUpper()))
                throw new AssemblerException(ErrorFormatter.LabelIsRegister(lineNumber, chunk, code));

            if (IsCommand(chunk))
                throw new AssemblerException(ErrorFormatter.LabelIsCommand(lineNumber, chunk, code));

            if (IsDirective(chunk))
                throw new AssemblerException(ErrorFormatter.LabelIsDirective(lineNumber, chunk, code));
        }

        /// <summary>
        /// Валидирует hex-строку и выбрасывает исключение с подробным описанием из ErrorFormatter
        /// </summary>
        public void ValidateXString(string? chunk, int lineNumber, string code)
        {
            if (chunk == null || !chunk.StartsWith("X\"", StringComparison.OrdinalIgnoreCase))
                throw new AssemblerException(ErrorFormatter.Format(lineNumber, "Hex-строка должна начинаться с X\".", code));

            if (!chunk.EndsWith('\"'))
                throw new AssemblerException(ErrorFormatter.XStringNotClosed(lineNumber, chunk, code));

            string content = chunk.Substring(2, chunk.Length - 3);

            if (content.Length == 0)
                throw new AssemblerException(ErrorFormatter.XStringEmpty(lineNumber, code));

            if (content.Length % 2 != 0)
                throw new AssemblerException(ErrorFormatter.XStringOddLength(lineNumber, chunk, code));

            if (!Regex.IsMatch(content, @"^[0-9A-Fa-f]+$"))
                throw new AssemblerException(ErrorFormatter.XStringInvalidChars(lineNumber, chunk, code));
        }

        /// <summary>
        /// Валидирует символьную строку и выбрасывает исключение с подробным описанием из ErrorFormatter
        /// </summary>
        public void ValidateCString(string? chunk, int lineNumber, string code)
        {
            if (chunk == null || !chunk.StartsWith("C\"", StringComparison.OrdinalIgnoreCase))
                throw new AssemblerException(ErrorFormatter.Format(lineNumber, "Символьная строка должна начинаться с C\".", code));

            if (!chunk.EndsWith('\"'))
                throw new AssemblerException(ErrorFormatter.CStringNotClosed(lineNumber, chunk, code));

            if (chunk.Length < 4)
                throw new AssemblerException(ErrorFormatter.CStringEmpty(lineNumber, code));

            string content = chunk.Substring(2, chunk.Length - 3);
            
            var nonAscii = content.Where(c => c > 127).ToList();
            if (nonAscii.Any())
            {
                string nonAsciiStr = string.Join(", ", nonAscii.Select(c => $"'{c}'"));
                throw new AssemblerException(ErrorFormatter.CStringNonAscii(lineNumber, chunk, nonAsciiStr, code));
            }
        }
    }
}
