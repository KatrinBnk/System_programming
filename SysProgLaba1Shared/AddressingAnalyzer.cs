using System;
using System.Collections.Generic;
using System.Linq;
using SysProgLaba1Shared.Models;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Helpers;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Анализатор типов адресации в ассемблерном коде
    /// </summary>
    public class AddressingAnalyzer
    {
        /// <summary>
        /// Определяет тип адресации в коде
        /// </summary>
        /// <param name="lines">Строки кода для анализа</param>
        /// <returns>Тип адресации</returns>
        public AddressingType DetermineAddressingType(List<List<string>> lines)
        {
            bool hasDirectAddressing = false;
            bool hasRelativeAddressing = false;

            foreach (var line in lines)
            {
                var codeLine = GetCodeLineFromSource(line, 0); // Номер строки не важен для анализа
                
                if (codeLine.Command != null && IsCommand(codeLine.Command))
                {
                    var command = AvailibleCommands.Find(c => c.Name.ToUpper() == codeLine.Command);
                    if (command != null && command.Length == 4 && codeLine.FirstOperand != null)
                    {
                        // Проверяем тип адресации для команд длиной 4 байта
                        bool isRelative = codeLine.FirstOperand.StartsWith("[") && codeLine.FirstOperand.EndsWith("]");
                        
                        if (isRelative)
                        {
                            hasRelativeAddressing = true;
                        }
                        else
                        {
                            hasDirectAddressing = true;
                        }
                    }
                }
            }

            // Определяем тип адресации
            if (hasDirectAddressing && hasRelativeAddressing)
            {
                return AddressingType.Mixed;
            }
            else if (hasRelativeAddressing)
            {
                return AddressingType.RelativeOnly;
            }
            else
            {
                return AddressingType.DirectOnly;
            }
        }

        /// <summary>
        /// Проверяет соответствие кода заданному типу адресации
        /// </summary>
        /// <param name="lines">Строки кода для проверки</param>
        /// <param name="requiredType">Требуемый тип адресации</param>
        /// <returns>Список ошибок валидации</returns>
        public List<string> ValidateAddressingType(List<List<string>> lines, AddressingType requiredType)
        {
            var errors = new List<string>();
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var codeLine = GetCodeLineFromSource(line, lineNumber);
                
                if (codeLine.Command != null && IsCommand(codeLine.Command))
                {
                    var command = AvailibleCommands.Find(c => c.Name.ToUpper() == codeLine.Command);
                    if (command != null && command.Length == 4 && codeLine.FirstOperand != null)
                    {
                        bool isRelative = codeLine.FirstOperand.StartsWith("[") && codeLine.FirstOperand.EndsWith("]");
                        
                        switch (requiredType)
                        {
                            case AddressingType.DirectOnly:
                                if (isRelative)
                                {
                                    errors.Add($"Строка {lineNumber}: Относительная адресация запрещена в режиме 'только прямая адресация'. Используйте прямую адресацию без квадратных скобок.");
                                }
                                break;
                                
                            case AddressingType.RelativeOnly:
                                if (!isRelative)
                                {
                                    errors.Add($"Строка {lineNumber}: Прямая адресация запрещена в режиме 'только относительная адресация'. Используйте относительную адресацию с квадратными скобками [операнд].");
                                }
                                break;
                                
                            case AddressingType.Mixed:
                                // В смешанном режиме все типы адресации разрешены
                                break;
                        }
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Получает описание типа адресации
        /// </summary>
        /// <param name="addressingType">Тип адресации</param>
        /// <returns>Описание типа адресации</returns>
        public string GetAddressingTypeDescription(AddressingType addressingType)
        {
            switch (addressingType)
            {
                case AddressingType.DirectOnly:
                    return "Только прямая адресация - запрещена относительная адресация";
                case AddressingType.RelativeOnly:
                    return "Только относительная адресация - запрещена прямая адресация";
                case AddressingType.Mixed:
                    return "Смешанная адресация - разрешены оба типа";
                default:
                    return "Неизвестный тип адресации";
            }
        }

        // Вспомогательные методы (копируем из Assembler)
        private List<Command> AvailibleCommands => new List<Command>
        {
            new Command { Name = "LOADR1", Code = 0x01, Length = 4 },
            new Command { Name = "LOADR2", Code = 0x02, Length = 4 },
            new Command { Name = "SAVER1", Code = 0x03, Length = 4 },
            new Command { Name = "SAVER2", Code = 0x04, Length = 4 },
            new Command { Name = "ADD", Code = 0x05, Length = 2 },
            new Command { Name = "SUB", Code = 0x06, Length = 2 },
            new Command { Name = "JMP", Code = 0x07, Length = 4 },
            new Command { Name = "INT", Code = 0x08, Length = 2 }
        };

        private CodeLine GetCodeLineFromSource(List<string> line, int lineNumber)
        {
            // Упрощенная версия парсинга для анализа
            var codeLine = new CodeLine();
            
            if (line.Count > 0)
            {
                codeLine.Label = line[0];
            }
            
            if (line.Count > 1)
            {
                codeLine.Command = line[1];
            }
            
            if (line.Count > 2)
            {
                codeLine.FirstOperand = line[2];
            }
            
            if (line.Count > 3)
            {
                codeLine.SecondOperand = line[3];
            }
            
            return codeLine;
        }

        private bool IsCommand(string command)
        {
            return AvailibleCommands.Any(c => c.Name.ToUpper() == command?.ToUpper());
        }
    }
}
