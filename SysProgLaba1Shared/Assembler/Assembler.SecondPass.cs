using System;
using System.Collections.Generic;
using System.Linq;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Helpers;
using SysProgLaba1Shared.Models;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Второй проход ассемблера для полноперемещаемого формата
    /// </summary>
    public partial class Assembler 
    {
        public List<string> SecondPass(List<List<string>> firstPassCode)
        {
            var secondPassCode = new List<string>();
            CodeLine? codeLine = null;
            var textLine = string.Empty;
            var secondPassLine = string.Empty;
            second_ip = 0;
            int sectionIndex = 0;

            for (int i = 0; i < firstPassCode.Count; i++)
            {
                codeLine = GetCodeLineFromFirstPass(firstPassCode[i]);

                textLine = string.Join(" ", firstPassCode[i]);  

                // Первая строка = директива START
                if (i == 0)
                {
                    currentSection = Sections[0];
                    second_ip = currentSection.StartAddress;

                    // Формируем H-запись
                    secondPassLine = $"{"H"} {codeLine.Label}\t{currentSection.StartAddress:X6}\t{(currentSection.Length):X6}";
                }
                else
                {
                    switch (codeLine.Command) 
                    {
                        // Переключение на следующую секцию
                        case "CSECT":
                            {
                                if (currentSection.EndAddress < currentSection.StartAddress || 
                                    currentSection.EndAddress > currentSection.Length)
                                    throw new AssemblerException(ErrorFormatter.Format(i + 1, 
                                        $"Некорректный адрес входа в программу: {currentSection.EndAddress:X6}", 
                                        textLine));

                                // Добавляем M-записи для текущей секции
                                foreach (TNLine tnLine in TN.Where(s => s.Section == currentSection.Name))
                                {
                                    secondPassCode.Add($"{"M"} {tnLine.Address}\t{((tnLine.Label != null) ? tnLine.Label : string.Empty)}");
                                }

                                // Добавляем E-запись для текущей секции
                                secondPassCode.Add($"{"E"} {currentSection.EndAddress:X6}");

                                // Переключаемся на следующую секцию
                                sectionIndex++;
                                currentSection = Sections[sectionIndex];
                                second_ip = currentSection.StartAddress;

                                // Формируем H-запись для новой секции
                                secondPassLine = $"{"H"} {codeLine.Label}\t{currentSection.StartAddress:X6}\t{(currentSection.Length):X6}";
                                break; 
                            }

                        case "EXTDEF":
                            {
                                // Проверяем, что метка существует в TSI
                                SymbolicName? symbolicName = GetSymbolicName(codeLine.FirstOperand, currentSection.Name);

                                if (symbolicName == null)
                                    throw new AssemblerException(ErrorFormatter.LabelNotFound(i + 1, codeLine.FirstOperand!, textLine));

                                // Формируем D-запись
                                secondPassLine = $"{"D"} {codeLine.FirstOperand}\t{symbolicName.Address:X6}";
                                break; 
                            }

                        case "EXTREF":
                            {
                                // Проверяем, что метка существует в TSI
                                SymbolicName? symbolicName = GetSymbolicName(codeLine.FirstOperand, currentSection.Name);

                                if (symbolicName == null)
                                    throw new AssemblerException(ErrorFormatter.LabelNotFound(i + 1, codeLine.FirstOperand!, textLine));

                                // Формируем R-запись
                                secondPassLine = $"{"R"} {codeLine.FirstOperand}";
                                break;
                            }

                        // Если WORD + 3-байтовое значение: WORD => 6 (длина) + hex значение
                        case "WORD":
                            {
                                second_ip += 3; 
                                string labelStr = codeLine.Label ?? "";
                                secondPassLine = $"{"T"} {labelStr}\t{3:X2}\t{codeLine.FirstOperand:X6}";
                                break; 
                            }

                        // Если BYTE + 1-байтовое значение: BYTE => 1 (длина) + hex значение
                        // BYTE + строка: BYTE => длина строки + строка, преобразованная в ASCII
                        case "BYTE":
                            {
                                try
                                {
                                    int value = Convert.ToInt32(codeLine.FirstOperand, 16);

                                    second_ip += 1;
                                    string labelStr = codeLine.Label ?? "";
                                    secondPassLine = $"{"T"} {labelStr}\t{1:X2}\t{value:X2}";
                                    break; 
                                }
                                catch 
                                {
                                    if (IsCString(codeLine.FirstOperand))
                                    {
                                        string symbols = codeLine.FirstOperand!.Substring(2, codeLine.FirstOperand.Length-3);

                                        int length = symbols.Length;

                                        second_ip += length;
                                        string labelStr = codeLine.Label ?? "";
                                        secondPassLine = $"{"T"} {labelStr}\t{length:X2}\t{AssemblerHelper.ConvertToASCII(symbols)}";
                                        break;
                                    }
                                    else if (IsXString(codeLine.FirstOperand))
                                    {
                                        string symbols = codeLine.FirstOperand!.Substring(2, codeLine.FirstOperand.Length - 3);

                                        int length = symbols.Length/2;

                                        second_ip += length;
                                        string labelStr = codeLine.Label ?? "";
                                        secondPassLine = $"{"T"} {labelStr}\t{length:X2}\t{symbols}";
                                        break;
                                    }
                                    else
                                    {
                                        throw new AssemblerException(ErrorFormatter.Format(i + 1, 
                                            "Невозможно преобразовать первый операнд в строку", 
                                            textLine));
                                    }
                                }
                            }

                        // Если RESB/RESW: только длина
                        case "RESB":
                            {
                                int length;

                                try
                                {
                                    length = Convert.ToInt32(codeLine.FirstOperand, 16); 
                                }
                                catch 
                                {
                                    throw new AssemblerException(ErrorFormatter.InvalidFormat(i + 1, codeLine.FirstOperand!, "шестнадцатеричное число", textLine));
                                }

                                second_ip += length;
                                string labelStr = codeLine.Label ?? "";
                                secondPassLine = $"{"T"} {labelStr}\t{length:X2}";

                                break; 
                            }

                        case "RESW":
                            {
                                int length;

                                try
                                {
                                    length = Convert.ToInt32(codeLine.FirstOperand, 16);
                                }
                                catch 
                                {
                                    throw new AssemblerException(ErrorFormatter.InvalidFormat(i + 1, codeLine.FirstOperand!, "шестнадцатеричное число", textLine));
                                }

                                second_ip += length*3;
                                string labelStr = codeLine.Label ?? "";
                                secondPassLine = $"{"T"} {labelStr}\t{(length*3):X2}";

                                break;
                            }

                        // Команда
                        default:
                            {
                                // Проверяем, что это действительно команда (hex-код), а не директива
                                if (IsDirective(codeLine.Command))
                                {
                                    throw new AssemblerException(ErrorFormatter.Format(i + 1, 
                                        $"Директива '{codeLine.Command}' не обработана во втором проходе", 
                                        textLine));
                                }

                                // Пытаемся распарсить как hex-код команды
                                int commandHex;
                                try
                                {
                                    commandHex = Convert.ToInt32(codeLine.Command, 16);
                                }
                                catch
                                {
                                    throw new AssemblerException(ErrorFormatter.Format(i + 1, 
                                        $"Неверный формат команды: '{codeLine.Command}'", 
                                        textLine));
                                }

                                int addressingType = (byte)commandHex & 0x03;
                                int commandCode = ((byte)commandHex & 0xFC) >> 2;

                                var command = AvailibleCommands.Where(c => c.Code == commandCode).FirstOrDefault();
                                if (command == null)
                                {
                                    throw new AssemblerException(ErrorFormatter.Format(i + 1, 
                                        $"Команда с кодом {commandCode} не найдена в списке доступных команд", 
                                        textLine));
                                }

                                switch (addressingType) 
                                {
                                    case 0:
                                        {
                                            string labelStr = codeLine.Label ?? "";
                                            if(codeLine.FirstOperand == null && codeLine.SecondOperand == null) // команда без операндов
                                            {
                                                second_ip += command.Length; 
                                                secondPassLine = $"{"T"} {labelStr}\t{command.Length:X2}\t{codeLine.Command}";
                                            }
                                            else if(codeLine.SecondOperand != null) // регистры
                                            {
                                                second_ip += command.Length;
                                                secondPassLine = $"{"T"} {labelStr}\t{command.Length:X2}\t{codeLine.Command}{AssemblerHelper.GetRegisterNumber(codeLine.FirstOperand!):X1}{AssemblerHelper.GetRegisterNumber(codeLine.SecondOperand):X1}";
                                            }
                                            else // один операнд
                                            {
                                                second_ip += command.Length;
                                                secondPassLine = $"{"T"} {labelStr}\t{command.Length:X2}\t{codeLine.Command}{codeLine.FirstOperand}";
                                            }

                                            break;
                                        }
                                    
                                    // Прямая адресация
                                    case 1:
                                        {
                                            string operand = codeLine.FirstOperand!;
                                            if (operand.StartsWith("[") && operand.EndsWith("]"))
                                            {
                                                operand = operand.Substring(1, operand.Length - 2);
                                            }

                                            var symbolicName = GetSymbolicName(operand, currentSection.Name);    

                                            if(symbolicName == null)
                                                throw new AssemblerException(ErrorFormatter.LabelNotFound(i + 1, operand, textLine));

                                            second_ip += command.Length;
                                            string labelStr = codeLine.Label ?? "";

                                            if (symbolicName.Type == "ВС") // Внешняя ссылка
                                            {
                                                // Для внешних ссылок адрес = 0, метка добавляется в TN
                                                secondPassLine = $"{"T"} {labelStr}\t{command.Length:X2}\t{codeLine.Command}{0:X6}";
                                                PushToTN(labelStr, symbolicName.Name, currentSection.Name);
                                            }
                                            else
                                            {
                                                // Обычная метка - добавляем адрес и запись в TN
                                                secondPassLine = $"{"T"} {labelStr}\t{command.Length:X2}\t{codeLine.Command}{symbolicName.Address:X6}";
                                                PushToTN(labelStr, string.Empty, currentSection.Name);
                                            }

                                            break;  
                                        }
                                    
                                    // Относительная адресация
                                    case 2:
                                        {
                                            string operand = codeLine.FirstOperand!;
                                            if (operand.StartsWith("[") && operand.EndsWith("]"))
                                            {
                                                operand = operand.Substring(1, operand.Length - 2);
                                            }

                                            var symbolicName = GetSymbolicName(operand, currentSection.Name);

                                            if (symbolicName == null)
                                                throw new AssemblerException(ErrorFormatter.LabelNotFound(i + 1, operand, textLine));
                                            else
                                            {
                                                second_ip += command.Length;
   
                                                if(symbolicName.Type == "ВС")
                                                    throw new AssemblerException(ErrorFormatter.Format(i + 1, 
                                                        "Относительная адресация недопустима для внешних ссылок", 
                                                        textLine));
                                                else
                                                {
                                                    // Вычисляем смещение: адрес метки - адрес следующей команды
                                                    int currentAddress = string.IsNullOrEmpty(codeLine.Label) ? 0 : Convert.ToInt32(codeLine.Label, 16);
                                                    int nextAddress = second_ip;
                                                    int offset = symbolicName.Address - nextAddress;

                                                    // Форматируем смещение как 6-значное hex число
                                                    string offsetStr = ((offset < 0) 
                                                        ? offset.ToString("X6").Substring(2)
                                                        : offset.ToString("X6"));

                                                    string labelStr = codeLine.Label ?? "";
                                                    secondPassLine = $"{"T"} {labelStr}\t{command.Length:X2}\t{codeLine.Command}{offsetStr}";
                                                }
                                            }

                                            break;
                                        }

                                    default:  
                                        {
                                            throw new AssemblerException(ErrorFormatter.InvalidAddressingType(i + 1, textLine));
                                        }
                                }

                                break;  
                            }
                    }
                }

                secondPassCode.Add(secondPassLine); 
            }
            
            // Добавляем M-записи для последней секции
            foreach(TNLine tnLine in TN.Where(s => s.Section == currentSection.Name))
            {
                secondPassCode.Add($"{"M"} {tnLine.Address}\t{((tnLine.Label != null)? tnLine.Label : string.Empty)}");
            }

            // Проверяем адрес входа
            if (currentSection.EndAddress < currentSection.StartAddress || 
                currentSection.EndAddress > currentSection.Length) 
                throw new AssemblerException(ErrorFormatter.Format(0, 
                    $"Некорректный адрес входа в программу: {currentSection.EndAddress:X6}", 
                    ""));

            // Добавляем E-запись для последней секции
            secondPassCode.Add($"{"E"} {currentSection.EndAddress:X6}"); 

            return secondPassCode; 
        }
    }
}
