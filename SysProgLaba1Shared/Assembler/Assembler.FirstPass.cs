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
        public List<string> FirstPass(List<List<string>> lines, string AddressingMode)
        {
            var firstPassCode = new List<string>();
            var previousCommand = "";  

            // Сохраняем выбранный режим адресации
            this.AddressingMode = AddressingMode?.ToUpper() switch
            {
                "STRAIGHT" => AddressingType.DirectOnly,
                "RELATIVE" => AddressingType.RelativeOnly,
                "MIXED" => AddressingType.Mixed,
                _ => null
            };

            ip = 0;                        // command address counter thing (not exactly ip)
            
            bool startFlag = false;         // Was START directive found?  
            bool endFlag = false;           // Was END directive found? 

            CodeLine codeLine = null;

            for (int i = 0; i < lines.Count(); i++)
            {
                var line = lines[i];

                var textLine = string.Join(" ", line);
                var firstPassLine = string.Empty;

                if (!startFlag && i != 0) throw new AssemblerException($"Не найдена директива START в начале программы");

                // if  
                if(startFlag) OverflowCheck(ip, textLine); 

                // if the END directive has already been found in a previous lines, break 
                if (endFlag) break;

                codeLine = GetCodeLineFromSource(line); 

                // processing command part
                // cannot be null, so no null check needed
                // is it a keyword? 
                if (IsDirective(codeLine.Command))
                {
                    firstPassLine = ProcessDirective(codeLine, textLine, i + 1, ref startFlag, ref endFlag, previousCommand, AddressingMode);
                }
                // is it a command? 
                else if (IsCommand(codeLine.Command))
                {
                    firstPassLine = ProcessCommand(codeLine, textLine, i + 1, AddressingMode);
                }
                else
                {
                    throw new AssemblerException($"Неизвестная команда: {textLine}"); 
                }

                previousCommand = codeLine.Command; 
                firstPassCode.Add(firstPassLine);
            }

            if (!endFlag) throw new AssemblerException($"Не найдена точка входа в программу.");

            TSICheck(); 

            return firstPassCode; 
        }

        private string ProcessDirective(CodeLine codeLine, string textLine, int lineNumber, ref bool startFlag, ref bool endFlag, string previousCommand, string AddressingMode)
        {
            string firstPassLine = string.Empty;

            switch (codeLine.Command)
            {
                case "START":
                    {
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.DirectiveTooManyOperands(lineNumber, "START", textLine));

                        // START должна быть в начале и быть первой
                        if (ip != 0 || startFlag) 
                            throw new AssemblerException(ErrorFormatter.StartMustBeFirst(lineNumber, textLine));

                        // START найдена
                        startFlag = true;

                        // process first operand
                        int address;
                        
                        // check if it is a valid hex value 
                        if(codeLine.FirstOperand != null)
                        {
                            try
                            {
                                address = Convert.ToInt32(codeLine.FirstOperand, 10);
                            }
                            catch 
                            {
                                throw new AssemblerException($"Невозможно преобразовать первый операнд в адрес начала программы: {textLine}");
                            }

                            if (address != 0)
                                throw new AssemblerException($"Адрес загрузки должен быть равен нулю: {textLine}");                                   
                        }
                        else
                        {
                            address = 0; 
                        }

                        // check if it's within allocated memory bounds  
                        OverflowCheck(address, textLine); 

                        if(codeLine.Label == null) throw new AssemblerException($"Перед директивой START должна быть метка");

                        // initialize currentSection 
                        currentSection = new Section()
                        {
                            Name = codeLine.Label, 
                            StartAddress = address                                     
                        }; 

                        ip = address;
    
                        // output 
                        firstPassLine = $"{codeLine.Label}\t{codeLine.Command}\t{address:X6}";
                        break;
                    }

                case "CSECT":
                    {
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается ноль или один операнд: {textLine}");
                        
                        if (codeLine.Label == null) throw new AssemblerException($"Перед директивой CSECT должна быть метка");

                        // process first operand
                        int endAddress;

                        // check if it is a valid hex value 
                        if (codeLine.FirstOperand != null)
                        {
                            try
                            {
                                endAddress = Convert.ToInt32(codeLine.FirstOperand, 10);
                            }
                            catch 
                            {
                                throw new AssemblerException($"Невозможно преобразовать первый операнд в адрес входа в секцию: {textLine}");
                            }

                            if (endAddress < 0 || endAddress > 16777215) throw new AssemblerException($"Значение первого операнда выходит за границы допустимого диапазона (0-16777215): {textLine}");
                        }
                        else
                        {
                            endAddress = 0; 
                        }

                        // update and pass the previous currentsection into Sections 
                        currentSection.EndAddress = endAddress; 
                        currentSection.Length = ip - currentSection.StartAddress;

                        AddSection(currentSection); 

                        // initialize new currentSection 
                        currentSection = new Section() {
                            Name = codeLine.Label, 
                            StartAddress = 0
                        };

                        // output 
                        firstPassLine = $"{codeLine.Label}\t{codeLine.Command}\t{endAddress:X6}";

                        ip = 0; 

                        break;
                    }

                case "EXTDEF":
                    {
                        if (codeLine.FirstOperand == null) throw new AssemblerException($"Ожидается один операнд, но было получено ноль: {textLine}");
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается один операнд, но найдено два: {textLine}");

                        OrderCheck(codeLine.Command, previousCommand, textLine);

                        // firstOperand must be label 
                        if (!IsLabel(codeLine.FirstOperand))
                            throw new AssemblerException($"Операнд для директивы EXTDEF должен быть меткой: {textLine}");

                        PushToTSI(codeLine.FirstOperand, -1, currentSection.Name, "ВИ", textLine); 

                        firstPassLine = $"\t{"EXTDEF"}\t{codeLine.FirstOperand}";
                        break; 
                    }

                case "EXTREF":
                    {
                        if (codeLine.FirstOperand == null) throw new AssemblerException($"Ожидается один операнд, но было получено ноль: {textLine}");
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается один операнд, но найдено два: {textLine}");

                        OrderCheck(codeLine.Command, previousCommand, textLine);

                        // firstOperand must be label 
                        if (!IsLabel(codeLine.FirstOperand))
                            throw new AssemblerException($"Операнд для директивы EXTREF должен быть меткой: {textLine}");

                        PushToTSI(codeLine.FirstOperand, -1, currentSection.Name, "ВС", textLine);

                        firstPassLine = $"\t{"EXTREF"}\t{codeLine.FirstOperand}";
                        break;
                    }

                case "WORD":
                    // can only contain a 3-byte unsigned int value 
                    {
                        if (codeLine.FirstOperand == null) throw new AssemblerException($"Ожидается один операнд, но было получено ноль: {textLine}");
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается один операнд, но найдено два: {textLine}");

                        if (codeLine.Label != null)
                        {
                            PushToTSI(codeLine.Label, ip, currentSection.Name, string.Empty, textLine);
                        }

                        int value;

                        // try convert 
                        try
                        {
                            value = Convert.ToInt32(codeLine.FirstOperand, 10);
                        }
                        catch
                        { 
                            throw new AssemblerException($"Невозможно преобразовать первый операнд в число: {textLine}");
                        }

                        // check if within 0-16777215 
                        if (value <= 0 || value > 16777215) throw new AssemblerException($"Значение первого операнда выходит за границы допустимого диапазона (1-16777215): {textLine}");

                        // check for allocated memory overflow 
                        OverflowCheck(ip + 3, textLine);

                        firstPassLine = $"{ip:X6}\t{"WORD"}\t{value:X6}";
                        ip += 3;
                        break;
                    }

                case "BYTE":
                    {
                        if (codeLine.FirstOperand == null) throw new AssemblerException($"Ожидается один операнд, но было получено ноль: {textLine}");
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается один операнд, но найдено два: {textLine}");

                        if (codeLine.Label != null)
                        {
                            PushToTSI(codeLine.Label, ip, currentSection.Name, string.Empty, textLine);
                        }

                        int value;

                        // try to parse as a 1 byte value 
                        if (int.TryParse(codeLine.FirstOperand, out value))
                        {
                            // check if within 0-255 
                            if (value < 0 || value > 255) throw new AssemblerException($"Значение первого операнда выходит за границы допустимого диапазона (0-255): {textLine}");

                            // check for allocated memory overflow 
                            OverflowCheck(ip + 1, textLine);

                            firstPassLine = $"{ip:X6}\t{"BYTE"}\t{value:X2}";
                            ip += 1;
                        }
                        // couldnt parse as a numeric value => parse as a character string 
                        else if (IsCString(codeLine.FirstOperand))
                        {
                            string symbols = codeLine.FirstOperand.Substring(2, codeLine.FirstOperand.Length - 3);

                            // check for allocated memory overflow 
                            OverflowCheck(ip + symbols.Length, textLine); 

                            firstPassLine = $"{ip:X6}\t{"BYTE"}\t{codeLine.FirstOperand}";
                            ip += symbols.Length;
                        }
                        else if (IsXString(codeLine.FirstOperand))
                        {
                            string symbols = codeLine.FirstOperand.Trim('X').Trim('\"');

                            // check for allocated memory overflow 
                            OverflowCheck(ip + symbols.Length/2, textLine);

                            firstPassLine = $"{ip:X6}\t{"BYTE"}\t{codeLine.FirstOperand.ToUpper()}";
                            ip += symbols.Length / 2;
                        }
                        else
                        {
                            throw new AssemblerException($"Невозможно преобразовать первый операнд в символьную или шестнадцатеричную строку: {textLine}");
                        }

                        break; 
                    }

                case "RESW":
                    {
                        if (codeLine.FirstOperand == null) throw new AssemblerException($"Ожидается один операнд, но было получено ноль: {textLine}");
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается один операнд, но найдено два: {textLine}");

                        if (codeLine.Label != null)
                        {
                            PushToTSI(codeLine.Label, ip, currentSection.Name, string.Empty, textLine);
                        }

                        int value;

                        // try convert 
                        try
                        {
                            value = Convert.ToInt32(codeLine.FirstOperand, 10);
                        }
                        catch 
                        {
                            throw new AssemblerException($"Невозможно преобразовать первый операнд в число: {textLine}");
                        }

                        // check if within 0-16777215 
                        if (value <= 0 || value > 255) throw new AssemblerException($"Значение первого операнда выходит за границы допустимого диапазона (1-255): {textLine}");

                        // check for allocated memory overflow 
                        OverflowCheck(ip + value * 3, textLine);

                        firstPassLine = $"{ip:X6}\t{"RESW"}\t{value:X2}";
                        ip += value*3;
                        break;
                    }

                case "RESB":
                    {
                        if (codeLine.FirstOperand == null) throw new AssemblerException($"Ожидается один операнд, но было получено ноль: {textLine}");
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается один операнд, но найдено два: {textLine}");

                        if (codeLine.Label != null)
                        {
                            PushToTSI(codeLine.Label, ip, currentSection.Name, string.Empty, textLine);
                        }

                        int value;

                        // try convert 
                        try
                        {
                            value = Convert.ToInt32(codeLine.FirstOperand, 10);
                        }
                        catch 
                        {
                            throw new AssemblerException($"Невозможно преобразовать первый операнд в число: {textLine}");
                        }

                        // check if within 0-16777215 
                        if (value <= 0 || value > 255) throw new AssemblerException($"Значение первого операнда выходит за границы допустимого диапазона (1-255): {textLine}");

                        // check for allocated memory overflow 
                        OverflowCheck(ip + value, textLine);

                        firstPassLine = $"{ip:X6}\t{"RESB"}\t{value:X2}";
                        ip += value;
                        break;
                    }

                case "END":
                    {
                        if (codeLine.SecondOperand != null) throw new AssemblerException($"Ожидается максимум один операнд, но найдено два: {textLine}");

                        if (!startFlag || endFlag) throw new AssemblerException($"Не найдена метка START либо ошибка в директивах START/END: {textLine}");


                        if (codeLine.Label != null)
                        {
                            PushToTSI(codeLine.Label, ip, currentSection.Name, string.Empty, textLine);
                        }


                        // process first operand
                        int endAddress;

                        // check if it is a valid hex value 
                        if (codeLine.FirstOperand != null)
                        {
                            try
                            {
                                endAddress = Convert.ToInt32(codeLine.FirstOperand, 10);
                            }
                            catch 
                            {
                                throw new AssemblerException($"Невозможно преобразовать первый операнд в адрес входа в секцию: {textLine}");
                            }

                            if (endAddress < 0 || endAddress > 16777215) throw new AssemblerException($"Значение первого операнда выходит за границы допустимого диапазона (0-16777215): {textLine}");
                        }
                        else
                        {
                            endAddress = 0;
                        }

                        // update and pass the previous currentsection into Sections 
                        currentSection.EndAddress = endAddress;
                        currentSection.Length = ip - currentSection.StartAddress;

                        AddSection(currentSection); 

                        // output 
                        // firstPassLine = $"{codeLine.Label}\t{codeLine.Command}\t{endAddress}";

                        endFlag = true;
                        break;
                    }
            }

            return firstPassLine;
        }

        private string ProcessCommand(CodeLine codeLine, string textLine, int lineNumber, string AddressingMode)
        {
            string firstPassLine = string.Empty;
            var command = AvailibleCommands.Find(c => c.Name.ToUpper() == codeLine.Command)!;

            // Добавляем метку в TSI, если она есть
            if (codeLine.Label != null)
            {
                PushToTSI(codeLine.Label, ip, currentSection.Name, string.Empty, textLine);
            }

            switch (command.Length) 
            {
                // Длина 1
                case 1:
                    {
                        if (codeLine.FirstOperand != null) 
                            throw new AssemblerException(ErrorFormatter.CommandNoOperands(lineNumber, command.Name, textLine));

                            // check for allocated memory overflow 
                            OverflowCheck(ip + 1, textLine);

                        // Тип адресации 00
                        firstPassLine = $"{ip:X6}\t{(command.Code*4 + 0):X2}";

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
                                firstPassLine = $"{ip:X6}\t{(command.Code * 4 + 0):X2}\t{codeLine.FirstOperand} {codeLine.SecondOperand}";

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
                            firstPassLine = $"{ip:X6}\t{(command.Code * 4 + 0):X2}\t{value:X2}";
                            
                            ip += 2;
                            break;
                        }
                    }

                // Длина 4
                case 4:
                    {
                        if (codeLine.FirstOperand == null) 
                            throw new AssemblerException(ErrorFormatter.CommandRequiresOperand(lineNumber, command.Name, "метку, адрес или [метку] для относительной адресации", textLine));
                        
                        if (codeLine.SecondOperand != null) 
                            throw new AssemblerException(ErrorFormatter.CommandTooManyOperands(lineNumber, command.Name, textLine));

                        // Проверяем, используется ли относительная адресация (операнд в квадратных скобках)
                        bool isRelativeAddressing = codeLine.FirstOperand.StartsWith("[") && codeLine.FirstOperand.EndsWith("]");
                        string operandWithoutBrackets = isRelativeAddressing 
                            ? codeLine.FirstOperand.Substring(1, codeLine.FirstOperand.Length - 2) 
                            : codeLine.FirstOperand;

                        // Валидация режима адресации
                        ValidateAddressingMode(isRelativeAddressing, lineNumber, textLine);

                        // Сначала пытаемся распарсить как число
                        if (TryParseNumber(operandWithoutBrackets, out var value))
                        {
                            if(value < 0 || value > 16777215) 
                                throw new AssemblerException(ErrorFormatter.AddressOutOfRange(lineNumber, value, "0-16777215", textLine));

                            // Проверяем переполнение выделенной памяти
                            OverflowCheck(ip + 4, textLine, lineNumber);

                            // Тип адресации: 01 для прямой, 02 для относительной
                            int addressingType = isRelativeAddressing ? 2 : 1;
                            firstPassLine = $"{ip:X6}\t{(command.Code * 4 + addressingType):X2}\t{value:X6}";

                            ip += 4;
                            break;
                        }
                        // Не число - должна быть метка, валидируем с детальными ошибками
                        else if (IsLabel(operandWithoutBrackets))
                        {
                            ValidateLabel(operandWithoutBrackets, lineNumber, textLine);
                            
                            // Проверяем переполнение выделенной памяти
                            OverflowCheck(ip + 4, textLine, lineNumber);

                            // Тип адресации: 01 для прямой, 02 для относительной
                            int addressingType = isRelativeAddressing ? 2 : 1;
                            firstPassLine = $"{ip:X6}\t{(command.Code * 4 + addressingType):X2}\t{codeLine.FirstOperand}"; 

                            ip += 4;
                            break;
                        }
                        else
                        {
                            throw new AssemblerException(ErrorFormatter.InvalidFormat(lineNumber, operandWithoutBrackets, "метку или числовой адрес", textLine));
                        }
                    }
            }

            return firstPassLine;
        }

        /// <summary>
        /// Валидирует соответствие типа адресации установленному режиму
        /// </summary>
        /// <param name="isRelativeAddressing">Используется ли относительная адресация</param>
        /// <param name="lineNumber">Номер строки</param>
        /// <param name="textLine">Текст строки</param>
        private void ValidateAddressingMode(bool isRelativeAddressing, int lineNumber, string textLine)
        {
            if (AddressingMode == null) return; // Режим не установлен - валидация не требуется

            switch (AddressingMode.Value)
            {
                case AddressingType.DirectOnly:
                    if (isRelativeAddressing)
                    {
                        throw new AssemblerException(ErrorFormatter.Format(lineNumber, 
                            "Относительная адресация запрещена в режиме 'только прямая адресация'. Используйте прямую адресацию без квадратных скобок.", 
                            textLine));
                    }
                    break;

                case AddressingType.RelativeOnly:
                    if (!isRelativeAddressing)
                    {
                        throw new AssemblerException(ErrorFormatter.Format(lineNumber, 
                            "Прямая адресация запрещена в режиме 'только относительная адресация'. Используйте относительную адресацию с квадратными скобками [операнд].", 
                            textLine));
                    }
                    break;

                case AddressingType.Mixed:
                    // В смешанном режиме все типы адресации разрешены
                    break;
            }
        }

        /// <summary>
        /// Проверяет порядок директив EXTREF и EXTDEF
        /// </summary>
        private void OrderCheck(string chunk, string previousCommand, string textLine)
        {
            if(chunk == "EXTDEF")
            {
                if (previousCommand != "START"
                    && previousCommand != "CSECT"
                    && previousCommand != "EXTDEF")
                {
                    throw new AssemblerException($"Директива EXTDEF может стоять только после директив START, CSECT и EXTDEF: {textLine}"); 
                }
            }
            else if (chunk == "EXTREF")
            {
                if (previousCommand != "START"
                    && previousCommand != "CSECT"
                    && previousCommand != "EXTDEF"
                    && previousCommand != "EXTREF")
                {
                    throw new AssemblerException($"Директива EXTREF может стоять только после директив START, CSECT, EXTDEF и EXTREF: {textLine}");
                }
            }
        }
    }
}
