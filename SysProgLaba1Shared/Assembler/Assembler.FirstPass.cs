using System;
using System.Collections.Generic;
using System.Linq;
using SysProgLaba1Shared.Models;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Helpers;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Первый проход ассемблера
    /// </summary>
    public partial class Assembler 
    {
        public List<string> FirstPass(List<List<string>> lines)
        {
            var firstPassCode = new List<string>();

            startAddress = 0; 
            endAddress = 0; 
            ip = 0; // Адреса команд
            
            bool startFlag = false;  
            bool endFlag = false;

            CodeLine? codeLine = null; 
            int lineNumber = 0; // Номер строки для отладки (чтобы показывать понятные ошибки)

            foreach (List<string> line in lines)
            {
                lineNumber++;
                var textLine = string.Join(" ", line);
                var firstPassLine = string.Empty;

                if (!startFlag && ip != 0) 
                    throw new AssemblerException(ErrorFormatter.Format(lineNumber, "Первой директивой программы должна быть START", textLine));

                // Проверка переполнения памяти
                if(startFlag) OverflowCheck(ip, textLine, lineNumber); 

                // Если директива END уже найдена в предыдущих строках, выходим
                if (endFlag) break;

                codeLine = GetCodeLineFromSource(line, lineNumber); 

                // Сначала обрабатываем метку
                if(codeLine.Label != null)
                {
                    // Пытаемся найти метку в ТСИ
                    if (TSI.Select(w => w.Name.ToUpper()).Contains(codeLine.Label.ToUpper()))
                    {
                        throw new AssemblerException(ErrorFormatter.LabelAlreadyDefined(lineNumber, codeLine.Label, textLine));
                    }
                    else if (startFlag)
                    {
                        PushToTSI(codeLine.Label, ip);
                    }
                }

                // Обрабатываем команду/директиву
                if (IsDirective(codeLine.Command))
                {
                    firstPassLine = ProcessDirective(codeLine, textLine, lineNumber, ref startFlag, ref endFlag);
                }
                else if (IsCommand(codeLine.Command))
                {
                    firstPassLine = ProcessCommand(codeLine, textLine, lineNumber);
                }
                else
                {
                    throw new AssemblerException(ErrorFormatter.UnknownCommand(lineNumber, codeLine.Command, textLine)); 
                }

                firstPassCode.Add(firstPassLine);
            }

            if (!endFlag) throw new AssemblerException(ErrorFormatter.Format("Программа должна завершаться директивой END.")); 

            return firstPassCode; 
        }

        private string ProcessDirective(CodeLine codeLine, string textLine, int lineNumber, ref bool startFlag, ref bool endFlag)
        {
            string firstPassLine = string.Empty;

            switch (codeLine.Command)
            {
                case "START":
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveRequiresOperand(lineNumber, "START", "ненулевое значение адреса", textLine));

                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveTooManyOperands(lineNumber, "START", textLine));

                        // START должна быть в начале и быть первой
                        if (ip != 0 || startFlag) 
                            throw new AssemblerException(ErrorFormatter.StartMustBeFirst(lineNumber, textLine));

                        // START найдена
                        startFlag = true;

                        // Обрабатываем первый операнд
                        int address;
                        
                        // Проверяем, что это корректное числовое значение
                        try
                        {
                            address = ParseNumber(codeLine.FirstOperand);
                        }
                        catch
                        {
                            throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, codeLine.FirstOperand, "число (десятичное или шестнадцатеричное с суффиксом 'h')", textLine));
                        }

                        // Проверяем границы выделенной памяти
                        OverflowCheck(address, textLine, lineNumber); 

                        if (address == 0) 
                            throw new AssemblerException(ErrorFormatter.AddressCannotBeZero(lineNumber, textLine));

                        if(codeLine.Label == null) 
                            throw new AssemblerException(ErrorFormatter.LabelRequired(lineNumber, "START", textLine));

                        ip = address;
                        startAddress = address;

                        // Формируем выходную строку
                        firstPassLine = $"{codeLine.Label} {codeLine.Command} {address:X6}";
                        break;
                    }

                case "WORD":
                    // Может содержать только 3-байтовое беззнаковое целое значение 
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveRequiresOperand(lineNumber, "WORD", "значение 1-16777215", textLine));
                        
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveTooManyOperands(lineNumber, "WORD", textLine));

                        int value;

                        // Пытаемся преобразовать
                        try
                        {
                            value = ParseNumber(codeLine.FirstOperand);
                        }
                        catch
                        {
                            throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, codeLine.FirstOperand, "число (десятичное или шестнадцатеричное с суффиксом 'h')", textLine));
                        }

                        // Проверяем диапазон 0-16777215
                        if (value <= 0 || value > 16777215) 
                            throw new AssemblerException(ErrorFormatter.ValueOutOfRange(lineNumber, value, "Для WORD допустимы значения 1-16777215", textLine));

                        // Проверяем переполнение выделенной памяти
                        OverflowCheck(ip + 3, textLine, lineNumber);

                        firstPassLine = $"{ip:X6} {"WORD"} {value:X6}";
                        ip += 3;
                        break;
                    }

                case "BYTE":
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveRequiresOperand(lineNumber, "BYTE", "число 0-255, строка C\"...\" или X\"...\"", textLine));
                        
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveTooManyOperands(lineNumber, "BYTE", textLine));

                        int value;

                        // Пытаемся распарсить как 1-байтовое значение
                        if (TryParseNumber(codeLine.FirstOperand, out value))
                        {
                            // Проверяем диапазон 0-255
                            if (value < 0 || value > 255) 
                                throw new AssemblerException(ErrorFormatter.ValueOutOfRange(lineNumber, value, "Для BYTE допустимы значения 0-255", textLine));

                            // Проверяем переполнение выделенной памяти
                            OverflowCheck(ip + 1, textLine, lineNumber);

                            firstPassLine = $"{ip:X6} {"BYTE"} {value:X2}";
                            ip += 1;
                            break;
                        }
                        // Не удалось распарсить как число => парсим как символьную строку
                        else
                        {
                            // Сначала пытаемся как C-строку
                            if (IsCString(codeLine.FirstOperand))
                            {
                                ValidateCString(codeLine.FirstOperand, lineNumber, textLine);
                                string symbols = codeLine.FirstOperand.Trim('C').Trim('\"');

                                // Проверяем переполнение выделенной памяти
                                OverflowCheck(ip + symbols.Length, textLine, lineNumber); 

                                firstPassLine = $"{ip:X6} {"BYTE"} {codeLine.FirstOperand}";
                                ip += symbols.Length;
                                break;
                            }
                            // Пытаемся как X-строку
                            else if (IsXString(codeLine.FirstOperand))
                            {
                                ValidateXString(codeLine.FirstOperand, lineNumber, textLine);
                                string symbols = codeLine.FirstOperand.Trim('X').Trim('\"');

                                // Проверяем переполнение выделенной памяти
                                OverflowCheck(ip + symbols.Length / 2, textLine, lineNumber);

                                firstPassLine = $"{ip:X6} {"BYTE"} {codeLine.FirstOperand.ToUpper()}";
                                ip += symbols.Length / 2;
                                break;
                            }
                            else
                            {
                                throw new AssemblerException(ErrorFormatter.InvalidOperandFormat(lineNumber, codeLine.FirstOperand, 
                                    "- Число 0-255\n- Строка C\"текст\"\n- Hex-строка X\"AB12...\"", textLine));
                            }
                        }
                    }

                case "RESW":
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveRequiresOperand(lineNumber, "RESW", "количество слов 1-255", textLine));
                        
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveTooManyOperands(lineNumber, "RESW", textLine));

                        int value;

                        // Пытаемся преобразовать
                        try
                        {
                            value = ParseNumber(codeLine.FirstOperand);
                        }
                        catch
                        {
                            throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, codeLine.FirstOperand, "число (десятичное или шестнадцатеричное с суффиксом 'h')", textLine));
                        }

                        // Проверяем диапазон 1-255
                        if (value <= 0 || value > 255) 
                            throw new AssemblerException(ErrorFormatter.ValueOutOfRange(lineNumber, value, "Для RESW допустимы значения 1-255", textLine));

                        // Проверяем переполнение выделенной памяти
                        OverflowCheck(ip + value * 3, textLine, lineNumber);

                        firstPassLine = $"{ip:X6} {"RESW"} {value:X2}";
                        ip += value*3;
                        break;
                    }

                case "RESB":
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveRequiresOperand(lineNumber, "RESB", "количество байт 1-255", textLine));
                        
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveTooManyOperands(lineNumber, "RESB", textLine));

                        int value;

                        // Пытаемся преобразовать
                        try
                        {
                            value = ParseNumber(codeLine.FirstOperand);
                        }
                        catch
                        {
                            throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, codeLine.FirstOperand, "число (десятичное или шестнадцатеричное с суффиксом 'h')", textLine));
                        }

                        // Проверяем диапазон 1-255
                        if (value <= 0 || value > 255) 
                            throw new AssemblerException(ErrorFormatter.ValueOutOfRange(lineNumber, value, "Для RESB допустимы значения 1-255", textLine));

                        // Проверяем переполнение выделенной памяти
                        OverflowCheck(ip + value, textLine, lineNumber);

                        firstPassLine = $"{ip:X6} {"RESB"} {value:X2}";
                        ip += value;
                        break;
                    }

                case "END":
                    {
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveNoOperands(lineNumber, "END", "адрес точки входа, необязательно", textLine));

                        if (!startFlag || endFlag) 
                            throw new AssemblerException(ErrorFormatter.EndMustBeAfterStart(lineNumber, textLine));

                        if (codeLine.FirstOperand == null)
                        {
                            endAddress = startAddress;
                        }
                        else
                        {
                            int address;

                            // Проверяем, что это корректное числовое значение
                            try
                            {
                                address = ParseNumber(codeLine.FirstOperand);
                            }
                            catch
                            {
                                throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, codeLine.FirstOperand, "число (десятичное или шестнадцатеричное с суффиксом 'h')", textLine));
                            }

                            if (address < 0 || address > 16777215) 
                                throw new AssemblerException(ErrorFormatter.AddressOutOfRange(lineNumber, address, "0-16777215", textLine));

                            // Проверяем границы выделенной памяти
                            OverflowCheck(address, textLine, lineNumber); 

                            endAddress = address;
                        }

                        endFlag = true;
                        break;
                    }
            }

            return firstPassLine;
        }

        private string ProcessCommand(CodeLine codeLine, string textLine, int lineNumber)
        {
            string firstPassLine = string.Empty;
            var command = AvailibleCommands.Find(c => c.Name.ToUpper() == codeLine.Command)!;

            switch (command.Length) 
            {
                // Длина 1
                case 1:
                    {
                        if (codeLine.FirstOperand != null) 
                            throw new AssemblerException(ErrorFormatter.CommandNoOperands(lineNumber, command.Name, textLine));

                        // Проверяем переполнение выделенной памяти
                        OverflowCheck(ip + 1, textLine, lineNumber);

                        // Тип адресации 00
                        firstPassLine = $"{ip:X6} {(command.Code*4 + 0):X2}";

                        ip += 1;
                        break;
                    }

                // Длина 2  
                // либо два регистра как два операнда
                // либо одно 1-байтовое значение
                case 2:
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.CommandRequiresOperand(lineNumber, command.Name, "два регистра или одно значение", textLine));

                        // Два регистра
                        if (codeLine.SecondOperand != null)
                        {
                            if (IsRegister(codeLine.FirstOperand) && IsRegister(codeLine.SecondOperand))
                            {
                                // Проверяем переполнение выделенной памяти
                                OverflowCheck(ip + 2, textLine, lineNumber);

                                // Тип адресации 00
                                firstPassLine = $"{ip:X6} {(command.Code * 4 + 0):X2} {codeLine.FirstOperand} {codeLine.SecondOperand}";

                                ip += 2;
                                break;
                            }
                            else
                            {
                                throw new AssemblerException(ErrorFormatter.CommandInvalidOperands(lineNumber, command.Name, "два регистра (R0-R15)", textLine));
                            }
                        }
                        // 1-байтовое значение
                        else
                        {
                            if (codeLine.SecondOperand != null) 
                                throw new AssemblerException(ErrorFormatter.CommandTooManyOperands(lineNumber, command.Name, textLine));

                            int value; 

                            // Пытаемся преобразовать
                            try
                            {
                                value = ParseNumber(codeLine.FirstOperand);
                            }
                            catch
                            {
                                throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, codeLine.FirstOperand, "число (десятичное или шестнадцатеричное с суффиксом 'h')", textLine));
                            }

                            // Проверяем диапазон 0-255
                            if (value < 0 || value > 255)
                                throw new AssemblerException(ErrorFormatter.ValueOutOfRange(lineNumber, value, "Допустимый диапазон: 0-255", textLine));

                            // Проверяем переполнение выделенной памяти
                            OverflowCheck(ip + 2, textLine, lineNumber); 

                            // Тип адресации 00
                            firstPassLine = $"{ip:X6} {(command.Code * 4 + 0):X2} {value:X2}";
                            
                            ip += 2;
                            break;
                        }
                    }

                // Длина 4
                case 4:
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.CommandRequiresOperand(lineNumber, command.Name, "метку или адрес", textLine));
                        
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.CommandTooManyOperands(lineNumber, command.Name, textLine));

                        // Сначала пытаемся распарсить как число
                        if (TryParseNumber(codeLine.FirstOperand, out var value))
                        {
                            if(value < 0 || value > 16777215) 
                                throw new AssemblerException(ErrorFormatter.AddressOutOfRange(lineNumber, value, "0-16777215", textLine));

                            // Проверяем переполнение выделенной памяти
                            OverflowCheck(ip + 4, textLine, lineNumber);

                            // Тип адресации 01
                            firstPassLine = $"{ip:X6} {(command.Code * 4):X2} {value:X6}";

                            ip += 4;
                            break;
                        }
                        // Не число - должна быть метка, валидируем с детальными ошибками
                        else if (IsLabel(codeLine.FirstOperand))
                        {
                            ValidateLabel(codeLine.FirstOperand, lineNumber, textLine);
                            
                            // Проверяем переполнение выделенной памяти
                            OverflowCheck(ip + 4, textLine, lineNumber);

                            // Тип адресации 01
                            firstPassLine = $"{ip:X6} {(command.Code * 4 + 1):X2} {codeLine.FirstOperand}"; 

                            ip += 4;
                            break;
                        }
                        else
                        {
                            throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, codeLine.FirstOperand, "метку или числовой адрес", textLine));
                        }
                    }
            }

            return firstPassLine;
        }
    }
}
