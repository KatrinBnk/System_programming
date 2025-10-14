using System;
using System.Collections.Generic;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Helpers;
using SysProgLaba1Shared.Models;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Второй проход ассемблера
    /// </summary>
    public partial class Assembler 
    {
        public List<string> SecondPass(List<List<string>> firstPassCode)
        {
            var secondPassCode = new List<string>();
            CodeLine? codeLine = null;
            var textLine = string.Empty;
            var secondPassLine = string.Empty;

            for (int i = 0; i < firstPassCode.Count;  i++)
            {
                codeLine = GetCodeLineFromFirstPass(firstPassCode[i], i + 1);
                textLine = string.Join(" ", firstPassCode[i]);  

                // Первая строка = директива START
                if (i == 0)
                {
                    // Формируем выходную строку
                    secondPassLine = $"{"H"} {codeLine.Label} {startAddress:X6} {(ip - startAddress):X6}";
                }
                else
                {
                    switch (codeLine.Command) {

                        // Если WORD + 3-байтовое значение: WORD => 6 (длина) + hex значение
                        case "WORD":
                            {
                                secondPassLine = $"{"T"} {codeLine.Label} {3:X2} {codeLine.FirstOperand}";
                                break; 
                            }

                        // Если BYTE + 1-байтовое значение: BYTE => 1 (длина) + hex значение
                        // BYTE + строка: BYTE => длина строки + строка, преобразованная в ASCII
                        case "BYTE":
                            {
                                // Проверяем, это C-строка или X-строка
                                if (IsCString(codeLine.FirstOperand))
                                {
                                    string symbols = codeLine.FirstOperand!.Substring(2, codeLine.FirstOperand.Length-3);
                                    int length = symbols.Length;
                                    secondPassLine = $"{"T"} {codeLine.Label} {length:X2} {AssemblerHelper.ConvertToASCII(symbols)}";
                                    break;
                                }
                                else if (IsXString(codeLine.FirstOperand))
                                {
                                    string symbols = codeLine.FirstOperand!.Trim('X').Trim('\"');
                                    // X-строка: каждые 2 hex-символа = 1 байт
                                    int length = symbols.Length / 2;
                                    secondPassLine = $"{"T"} {codeLine.Label} {length:X2} {symbols}";
                                    break;
                                }
                                // Иначе это число (уже в hex формате из первого прохода)
                                else
                                {
                                    secondPassLine = $"{"T"} {codeLine.Label} {1:X2} {codeLine.FirstOperand}";
                                    break; 
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
                                secondPassLine = $"{"T"} {codeLine.Label} {length:X2}";
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
                                secondPassLine = $"{"T"} {codeLine.Label} {(length*3):X2}";
                                break;
                            }

                        // Команда
                        default:
                            {
                                int addressingType = (byte)Convert.ToInt32(codeLine.Command, 16) & 0x03;

                                switch (addressingType) 
                                {
                                    case 0:
                                        {
                                            if(codeLine.FirstOperand == null && codeLine.SecondOperand == null) // команда без операндов
                                            {
                                                secondPassLine = $"{"T"} {codeLine.Label} {1:X2} {codeLine.Command}";
                                            }
                                            else if(codeLine.SecondOperand != null) // регистры
                                            {
                                                secondPassLine = $"{"T"} {codeLine.Label} {2:X2} {codeLine.Command}{AssemblerHelper.GetRegisterNumber(codeLine.FirstOperand):X1}{AssemblerHelper.GetRegisterNumber(codeLine.SecondOperand):X1}";
                                            }
                                            else // один операнд
                                            {
                                                // Длина = 1 байт (МКОП) + количество байтов операнда
                                                int operandBytes = codeLine.FirstOperand!.Length / 2;
                                                int totalLength = 1 + operandBytes;
                                                secondPassLine = $"{"T"} {codeLine.Label} {totalLength:X2} {codeLine.Command}{codeLine.FirstOperand!}";
                                            }
                                            break;
                                        }

                                    // Прямая адресация
                                    case 1:
                                        {
                                            // Убираем квадратные скобки, если они есть (хотя для прямой адресации их не должно быть)
                                            string operand = codeLine.FirstOperand!;
                                            if (operand.StartsWith("[") && operand.EndsWith("]"))
                                            {
                                                operand = operand.Substring(1, operand.Length - 2);
                                            }

                                            var symbolicName = GetSymbolicName(operand);    

                                            if(symbolicName == null)
                                            {
                                                throw new AssemblerException(ErrorFormatter.LabelNotFound(i + 1, operand, textLine));
                                            }
                                            else
                                            {
                                                // Добавляем адрес команды в таблицу настройки (перемещений)
                                                int commandAddress = Convert.ToInt32(codeLine.Label, 16);
                                                RelocationTable.Add(commandAddress);

                                                secondPassLine = $"{"T"} {codeLine.Label} {4:X2} {codeLine.Command}{symbolicName.Address:X6}";
                                            }
                                            break; 
                                        }

                                    // Относительная адресация
                                    case 2:
                                        {
                                            // Убираем квадратные скобки
                                            string operand = codeLine.FirstOperand!;
                                            if (operand.StartsWith("[") && operand.EndsWith("]"))
                                            {
                                                operand = operand.Substring(1, operand.Length - 2);
                                            }

                                            var symbolicName = GetSymbolicName(operand);    

                                            if(symbolicName == null)
                                            {
                                                throw new AssemblerException(ErrorFormatter.LabelNotFound(i + 1, operand, textLine));
                                            }
                                            else
                                            {
                                                // Вычисляем смещение: адрес метки - адрес следующей команды
                                                int currentAddress = Convert.ToInt32(codeLine.Label, 16);
                                                int nextAddress = currentAddress + 4; // Команда длиной 4 байта
                                                int offset = symbolicName.Address - nextAddress;

                                                // Смещение может быть отрицательным (для переходов назад)
                                                // Представляем его как беззнаковое 24-битное значение
                                                int offsetAsUnsigned = offset & 0xFFFFFF;

                                                secondPassLine = $"{"T"} {codeLine.Label} {4:X2} {codeLine.Command}{offsetAsUnsigned:X6}";
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
            
            if (endAddress < startAddress || endAddress > ip) 
                throw new AssemblerException(ErrorFormatter.EntryPointOutOfProgram(endAddress, startAddress, ip));

            // Добавляем M-записи (таблица настройки/перемещений) перед точкой входа
            foreach (var address in RelocationTable)
            {
                secondPassCode.Add($"{"M"} {address:X6}");
            }

            secondPassCode.Add($"{"E"} {endAddress:X6}"); 

            return secondPassCode; 
        }
    }
}

