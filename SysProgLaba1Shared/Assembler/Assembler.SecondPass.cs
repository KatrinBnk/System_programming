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
                                secondPassLine = $"{"T"} {codeLine.Label} {3:X2} {codeLine.FirstOperand:X6}";
                                break; 
                            }

                        // Если BYTE + 1-байтовое значение: BYTE => 1 (длина) + hex значение
                        // BYTE + строка: BYTE => длина строки + строка, преобразованная в ASCII
                        case "BYTE":
                            {
                                try
                                {
                                    int value = Convert.ToInt32(codeLine.FirstOperand, 16);
                                    secondPassLine = $"{"T"} {codeLine.Label} {1:X2} {value:X2}";
                                    break; 
                                }
                                catch
                                {
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
                                        int length = symbols.Length / 2;
                                        secondPassLine = $"{"T"} {codeLine.Label} {length:X2} {symbols}";
                                        break;
                                    }
                                    else{
                                        throw new AssemblerException(ErrorFormatter.InvalidFormat(i + 1, codeLine.FirstOperand!, "числовое значение, C-строку или X-строку", textLine));
                                    }
                                }
                            }

                        // Если RESB/RESW
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
                                                int commandCode = Convert.ToInt32(codeLine.Command, 16) >> 2; // Убираем биты адресации
                                                var command = AvailibleCommands.Find(c => c.Code == commandCode); // находим команду в списке доступных
                                                secondPassLine = $"{"T"} {codeLine.Label} {command.Length:X2} {codeLine.Command}{codeLine.FirstOperand!}";
                                            }
                                            break;
                                        }

                                    case 1:
                                        {
                                            var symbolicName = GetSymbolicName(codeLine.FirstOperand);    

                                            if(symbolicName == null)
                                            {
                                                throw new AssemblerException(ErrorFormatter.LabelNotFound(i + 1, codeLine.FirstOperand!, textLine));
                                            }
                                            else
                                            {
                                                secondPassLine = $"{"T"} {codeLine.Label} {4:X2} {codeLine.Command}{symbolicName.Address:X6}";
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

            secondPassCode.Add($"{"E"} {endAddress:X6}"); 

            return secondPassCode; 
        }
    }
}

