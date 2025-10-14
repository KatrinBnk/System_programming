using System.Collections.Generic;
using SysProgLaba1Shared.Models;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Helpers;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Методы парсинга строк кода для ассемблера
    /// </summary>
    public partial class Assembler 
    {
        // Возвращает объект команды с nullable параметрами (метка, первый и второй операнды) и обязательной командой.
        // Гарантирует, что метка и команда/директива соответствуют формату. Не проверяет операнды.
        // Метки и команды/директивы преобразуются в верхний регистр
        public CodeLine GetCodeLineFromSource(List<string> line, int lineNumber = 0)
        {
            var textLine = string.Join(" ", line);

            if(line.Count < 1)
                throw new AssemblerException(ErrorFormatter.Format(lineNumber, "Пустая строка кода.", textLine));
            
            if(line.Count > 4)
                throw new AssemblerException(ErrorFormatter.Format(lineNumber, $"Слишком много элементов в строке ({line.Count}). Максимум 4: [Метка] Команда [Операнд1] [Операнд2]", textLine));

            switch (line.Count) 
            {
                case 1:
                    // Может быть только команда без операндов или END
                    if (IsCommand(line[0]) || line[0].ToUpper() == "END")
                    {
                        return new CodeLine()
                        {
                            Label = null,
                            Command = line[0].ToUpper(), 
                            FirstOperand = null, 
                            SecondOperand = null 
                        };  
                    } 
                    else
                    {
                        throw new AssemblerException(ErrorFormatter.Format(lineNumber, $"'{line[0]}' не является известной командой или директивой.\nОжидается команда без операндов или директива END.", textLine));
                    }

                case 2:
                    // Может быть команда с одним операндом или директива с одним операндом
                    if (IsCommand(line[0]) || IsDirective(line[0]))
                    {
                        return new CodeLine()
                        {
                            Label = null, 
                            Command = line[0].ToUpper(),
                            FirstOperand = line[1], 
                            SecondOperand = null
                        };
                    }
                    // Может быть метка и команда без операндов или START/END
                    else if (IsCommand(line[1]) || line[1].ToUpper() == "START" || line[1].ToUpper() == "END")
                    {
                        // Валидируем метку с детальными ошибками
                        ValidateLabel(line[0], lineNumber, textLine);
                        
                        return new CodeLine()
                        {
                            Label = line[0].ToUpper(),
                            Command = line[1].ToUpper(),
                            FirstOperand = null,
                            SecondOperand = null
                        };
                    }
                    else
                    {
                        throw new AssemblerException(ErrorFormatter.Format(lineNumber, 
                            $"'{line[1]}' не является командой или директивой.\nОжидается: [Метка Команда] или [Команда/Директива Операнд]", textLine));
                    }

                case 3:
                    // Может быть команда с двумя операндами
                    if (IsCommand(line[0]))
                    {
                        return new CodeLine()
                        {
                            Label = null,
                            Command = line[0].ToUpper(),
                            FirstOperand = line[1], 
                            SecondOperand = line[2] 
                        };
                    }
                    // Может быть директива с неправильным операндом (разбитым на части)
                    else if (IsDirective(line[0]))
                    {
                        // Объединяем второй и третий элементы обратно, чтобы показать в ошибке
                        string combinedOperand = line[1] + " " + line[2];
                        
                        // Определяем ожидаемый формат в зависимости от директивы
                        string expectedFormats = line[0].ToUpper() switch
                        {
                            "BYTE" => "- Число 0-255\n- Строка C\"текст\"\n- Hex-строка X\"AB12...\"",
                            "WORD" => "число 1-16777215",
                            "RESB" => "число 1-255 (количество байт)",
                            "RESW" => "число 1-255 (количество слов)",
                            "START" => "ненулевое значение адреса",
                            _ => "корректное значение"
                        };
                        
                        throw new AssemblerException(ErrorFormatter.InvalidOperandFormat(lineNumber, combinedOperand, expectedFormats, textLine));
                    }
                    // Может быть метка и директива с одним операндом
                    else if (IsCommand(line[1]) || IsDirective(line[1]))
                    {
                        // Валидируем метку с детальными ошибками
                        ValidateLabel(line[0], lineNumber, textLine);
                        
                        return new CodeLine()
                        {
                            Label = line[0].ToUpper(),
                            Command = line[1].ToUpper(),
                            FirstOperand = line[2], 
                            SecondOperand = null
                        };
                    }
                    else
                    {
                        throw new AssemblerException(ErrorFormatter.Format(lineNumber, 
                            $"'{line[1]}' не является командой или директивой.\nОжидается: [Метка Команда/Директива Операнд] или [Команда Операнд1 Операнд2]", textLine));
                    }

                case 4:
                    // Может быть только метка, команда и два операнда
                    if (IsCommand(line[1]))
                    {
                        // Валидируем метку с детальными ошибками
                        ValidateLabel(line[0], lineNumber, textLine);
                        
                        return new CodeLine()
                        {
                            Label = line[0].ToUpper(),
                            Command = line[1].ToUpper(),
                            FirstOperand = line[2], 
                            SecondOperand = line[3] 
                        };
                    }
                    // Если это директива, вероятно операнд разбился на несколько частей (например, "Test test" вместо C"Test test")
                    else if (IsDirective(line[1]))
                    {
                        // Объединяем третий и четвертый элементы обратно, чтобы показать в ошибке
                        string combinedOperand = line[2] + " " + line[3];
                        
                        // Определяем ожидаемый формат в зависимости от директивы
                        string expectedFormats = line[1].ToUpper() switch
                        {
                            "BYTE" => "- Число 0-255\n- Строка C\"текст\"\n- Hex-строка X\"AB12...\"",
                            "WORD" => "число 1-16777215",
                            "RESB" => "число 1-255 (количество байт)",
                            "RESW" => "число 1-255 (количество слов)",
                            "START" => "ненулевое значение адреса",
                            _ => "корректное значение"
                        };
                        
                        throw new AssemblerException(ErrorFormatter.InvalidOperandFormat(lineNumber, combinedOperand, expectedFormats, textLine));
                    }
                    else
                    {
                        throw new AssemblerException(ErrorFormatter.Format(lineNumber, 
                            $"'{line[1]}' не является командой.\nОжидается: [Метка Команда Операнд1 Операнд2]", textLine)); 
                    }

                default:
                    throw new AssemblerException(ErrorFormatter.Format(lineNumber, "Внутренняя ошибка парсера: неожиданное количество элементов.", textLine));
            }
        }

        public CodeLine GetCodeLineFromFirstPass(List<string> line, int lineNumber = 0)
        {
            var textLine = string.Join(" ", line);

            if (line.Count < 2)
                throw new AssemblerException(ErrorFormatter.Format(lineNumber, $"Строка вспомогательной таблицы слишком короткая ({line.Count} элемента). Ожидается минимум 2.", textLine));
            
            if (line.Count > 4)
                throw new AssemblerException(ErrorFormatter.Format(lineNumber, $"Строка вспомогательной таблицы слишком длинная ({line.Count} элементов). Максимум 4.", textLine));

            switch (line.Count)
            {
                case 2:
                    {
                        // ip + operand-less command  
                        return new CodeLine()
                        {
                            Label = line[0].ToUpper(),
                            Command = line[1].ToUpper(),
                            FirstOperand = null,
                            SecondOperand = null
                        };
                    }

                case 3:
                    {
                        // ip + command + operand 
                        return new CodeLine()
                        {
                            Label = line[0].ToUpper(),
                            Command = line[1].ToUpper(),
                            FirstOperand = line[2],
                            SecondOperand = null
                        };
                    }

                case 4:
                    {
                        // ip + command + operand1 + operand2 
                        return new CodeLine()
                        {
                            Label = line[0].ToUpper(),
                            Command = line[1].ToUpper(),
                            FirstOperand = line[2],
                            SecondOperand = line[3]
                        };
                    }

                default:
                    throw new AssemblerException(ErrorFormatter.Format(lineNumber, "Внутренняя ошибка парсера вспомогательной таблицы: неожиданное количество элементов.", textLine));
            }
        }
    }
}

