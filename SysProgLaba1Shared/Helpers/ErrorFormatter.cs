namespace SysProgLaba1Shared.Helpers
{
    /// <summary>
    /// Утилита для форматирования сообщений об ошибках
    /// </summary>
    public static class ErrorFormatter
    {
        // Базовое форматирование с номером строки
        public static string Format(int lineNumber, string message, string code)
        {
            return $"[Строка {lineNumber}] {message}\nКод: {code}";
        }

        // Форматирование без номера строки
        public static string Format(string message)
        {
            return message;
        }

        // Ошибки директив
        public static string DirectiveRequiresOperand(int lineNumber, string directive, string expectedFormat, string code)
        {
            return Format(lineNumber, $"Директива {directive} требует один операнд ({expectedFormat}).", code);
        }

        public static string DirectiveTooManyOperands(int lineNumber, string directive, string code)
        {
            return Format(lineNumber, $"Директива {directive} принимает только один операнд.", code);
        }

        public static string DirectiveNoOperands(int lineNumber, string directive, string expectedFormat, string code)
        {
            return Format(lineNumber, $"Директива {directive} принимает максимум один операнд ({expectedFormat}).", code);
        }

        // Ошибки команд
        public static string CommandNoOperands(int lineNumber, string commandName, string code)
        {
            return Format(lineNumber, $"Команда '{commandName}' не принимает операндов.", code);
        }

        public static string CommandRequiresOperand(int lineNumber, string commandName, string expectedFormat, string code)
        {
            return Format(lineNumber, $"Команда '{commandName}' требует операнд ({expectedFormat}).", code);
        }

        public static string CommandTooManyOperands(int lineNumber, string commandName, string code)
        {
            return Format(lineNumber, $"Команда '{commandName}' принимает только один операнд.", code);
        }

        public static string CommandInvalidOperands(int lineNumber, string commandName, string expectedFormat, string code)
        {
            return Format(lineNumber, $"Команда '{commandName}' с двумя операндами требует {expectedFormat}.", code);
        }

        // Ошибки значений
        public static string InvalidFormat(int lineNumber, string value, string expectedFormat, string code)
        {
            return Format(lineNumber, $"Неверный формат значения '{value}'. Ожидается {expectedFormat}.", code);
        }

        public static string ValueOutOfRange(int lineNumber, int value, string rangeDescription, string code)
        {
            return Format(lineNumber, $"Значение {value} выходит за допустимый диапазон. {rangeDescription}.", code);
        }

        public static string AddressOutOfRange(int lineNumber, int address, string rangeDescription, string code)
        {
            return Format(lineNumber, $"Адрес {address} выходит за допустимый диапазон ({rangeDescription}).", code);
        }

        public static string MemoryOverflow(int address, int maxAddress, string code, int lineNumber = 0)
        {
            if (lineNumber > 0)
                return Format(lineNumber, $"Адрес {address:X6} выходит за пределы доступной памяти (максимум {maxAddress:X6}).", code);
            else
                return $"Адрес {address:X6} выходит за пределы доступной памяти (максимум {maxAddress:X6}).\nКод: {code}";
        }

        // Ошибки меток
        public static string LabelAlreadyDefined(int lineNumber, string labelName, string code)
        {
            return Format(lineNumber, $"Метка '{labelName}' уже определена в программе.", code);
        }

        public static string LabelNotFound(int lineNumber, string labelName, string code)
        {
            return Format(lineNumber, $"Метка '{labelName}' не определена в программе.", code);
        }

        public static string LabelRequired(int lineNumber, string directive, string code)
        {
            return Format(lineNumber, $"Перед директивой {directive} должна быть метка (имя программы).", code);
        }

        // Общие ошибки
        public static string UnknownCommand(int lineNumber, string commandName, string code)
        {
            return Format(lineNumber, $"Неизвестная команда или директива '{commandName}'.", code);
        }

        public static string InvalidOperandFormat(int lineNumber, string operand, string expectedFormats, string code)
        {
            return Format(lineNumber, $"Неверный формат операнда '{operand}'. Ожидается:\n{expectedFormats}", code);
        }

        public static string InvalidLine(int lineNumber, string reason, string code)
        {
            return Format(lineNumber, reason, code);
        }

        // Специфичные ошибки START/END
        public static string StartMustBeFirst(int lineNumber, string code)
        {
            return Format(lineNumber, "Директива START должна быть первой в программе и встречаться только один раз.", code);
        }

        public static string AddressCannotBeZero(int lineNumber, string code)
        {
            return Format(lineNumber, "Адрес начала программы не может быть равен нулю.", code);
        }

        public static string EndMustBeAfterStart(int lineNumber, string code)
        {
            return Format(lineNumber, "Директива END должна быть единственной и идти после START.", code);
        }

        public static string EntryPointOutOfProgram(int entryAddress, int startAddr, int endAddr)
        {
            return $"Адрес точки входа {entryAddress:X6} находится вне программы.\nДопустимый диапазон: {startAddr:X6} - {endAddr:X6}";
        }

        // ========== Валидация строк ==========

        public static string LabelTooLong(int lineNumber, string label, string code)
        {
            return Format(lineNumber, $"Метка '{label}' слишком длинная (максимум 10 символов).", code);
        }

        public static string LabelInvalidStart(int lineNumber, string label, string code)
        {
            return Format(lineNumber, $"Метка '{label}' должна начинаться с латинской буквы.", code);
        }

        public static string LabelInvalidChars(int lineNumber, string label, string code)
        {
            return Format(lineNumber, $"Метка '{label}' содержит недопустимые символы. Разрешены только буквы, цифры и подчеркивание.", code);
        }

        public static string LabelIsRegister(int lineNumber, string label, string code)
        {
            return Format(lineNumber, $"'{label}' является регистром и не может использоваться как метка.", code);
        }

        public static string LabelIsCommand(int lineNumber, string label, string code)
        {
            return Format(lineNumber, $"'{label}' является именем команды и не может использоваться как метка.", code);
        }

        public static string LabelIsDirective(int lineNumber, string label, string code)
        {
            return Format(lineNumber, $"'{label}' является директивой и не может использоваться как метка.", code);
        }

        public static string XStringNotClosed(int lineNumber, string xstring, string code)
        {
            return Format(lineNumber, $"Hex-строка '{xstring}' не закрыта кавычкой.", code);
        }

        public static string XStringEmpty(int lineNumber, string code)
        {
            return Format(lineNumber, "Hex-строка не может быть пустой.", code);
        }

        public static string XStringOddLength(int lineNumber, string xstring, string code)
        {
            return Format(lineNumber, $"Hex-строка '{xstring}' должна содержать четное количество символов (по 2 на байт).", code);
        }

        public static string XStringInvalidChars(int lineNumber, string xstring, string code)
        {
            return Format(lineNumber, $"Hex-строка '{xstring}' содержит недопустимые символы. Разрешены только 0-9, A-F.", code);
        }

        public static string CStringNotClosed(int lineNumber, string cstring, string code)
        {
            return Format(lineNumber, $"Символьная строка '{cstring}' не закрыта кавычкой.", code);
        }

        public static string CStringEmpty(int lineNumber, string code)
        {
            return Format(lineNumber, "Символьная строка не может быть пустой.", code);
        }

        public static string CStringNonAscii(int lineNumber, string cstring, string nonAsciiChars, string code)
        {
            return Format(lineNumber, $"Символьная строка '{cstring}' содержит не-ASCII символы: {nonAsciiChars}.", code);
        }

        public static string InvalidAddressingType(int lineNumber, string code)
        {
            return Format(lineNumber, "Неизвестный тип адресации в команде.", code);
        }
    }
}

