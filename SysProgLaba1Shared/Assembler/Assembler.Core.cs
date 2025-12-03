using System;
using System.Collections.Generic;
using System.Linq;
using SysProgLaba1Shared.Models;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Dto;
using SysProgLaba1Shared.Helpers;

namespace SysProgLaba1Shared
{
    /// <summary>
    /// Основной класс ассемблера - поля и основные методы
    /// </summary>
    public partial class Assembler 
    {
        private const int maxAddress = (1 << 24) - 1;  // 2^24 - 1 = 16777215  
        private int startAddress = 0;
        private int ip = 0; 
        private int second_ip = 0; // Счетчик адресов во втором проходе

        // Список секций программы
        private List<Section> Sections = new List<Section>();
        private Section currentSection = new Section();

        // Список базовых команд (используются в примерах, дополняются через ЮИ)
        public List<Command> AvailibleCommands { get; set; } = [
            new Command(){ Name = "JMP", Code = 1, Length = 4 },
            new Command(){ Name = "LOADR1", Code = 2, Length = 4 },
            new Command(){ Name = "LOADR2", Code = 3, Length = 4 },
            new Command(){ Name = "ADD", Code = 4, Length = 2 },
            new Command(){ Name = "SAVER1", Code = 5, Length = 4 },
            new Command(){ Name = "INT", Code = 6, Length = 2 },
        ];

        // Директивы (добавлены EXTREF, EXTDEF, CSECT для полноперемещаемого формата)
        private readonly string[] AvailibleDirectives = ["START", "END", "WORD", "BYTE", "RESB", "RESW", "EXTREF", "EXTDEF", "CSECT"]; 

        public List<SymbolicName> TSI = new(); 

        // Таблица настройки (перемещений) - для полноперемещаемого формата
        public List<TNLine> TN = new();

        // Режим адресации для валидации
        public AddressingType? AddressingMode { get; set; } = null;

        // Вызывается в первом проходе, валидирует команды (уникальность по названию и коду) + базовая валидация
        public void SetAvailibleCommands(List<CommandDto> newAvailibleCommandsDto)
        {
            var newAvailibleCommands = newAvailibleCommandsDto.Select(c => new Command(c)).ToList();

            var nameGroups = newAvailibleCommands.GroupBy(x => x.Name.ToUpper())
                                                 .Where(g => g.Count() > 1)
                                                 .ToList();

            if (nameGroups.Any())
            {
                var duplicates = string.Join(", ", nameGroups.Select(g => $"'{g.Key}' ({g.Count()} раз)"));
                throw new AssemblerException($"Обнаружены дублирующиеся имена команд: {duplicates}\nВсе имена команд должны быть уникальными.");
            }

            var codeGroups = newAvailibleCommands.GroupBy(x => x.Code)
                                                 .Where(g => g.Count() > 1)
                                                 .ToList();

            if (codeGroups.Any())
            {
                var duplicates = string.Join(", ", codeGroups.Select(g => $"{g.Key} (команды: {string.Join(", ", g.Select(c => c.Name))})"));
                throw new AssemblerException($"Обнаружены дублирующиеся коды команд: {duplicates}\nВсе коды команд должны быть уникальными.");
            }

            this.AvailibleCommands = newAvailibleCommands; 
        }

        /// <summary>
        /// Добавляет символическое имя в TSI (для полноперемещаемого формата)
        /// </summary>
        public void PushToTSI(string symbolicName, int address, string section, string type, string textLine)
        {
            symbolicName = symbolicName.ToUpper();

            // Проверяем, есть ли уже такая метка в этой секции
            if(TSI.Where(sn => sn.Section == section).Select(n => n.Name).Contains(symbolicName))
            {
                var sn = TSI.Where(sn => sn.Section == section).First(n => n.Name == symbolicName);

                if (sn.Type == "ВИ") // Внешний идентификатор (EXTDEF)
                {
                    if (type == "ВИ" || type == "ВС") // Дублирование EXTDEF не допускается
                    {
                        throw new AssemblerException(ErrorFormatter.LabelAlreadyDefined(0, symbolicName, textLine));
                    }
                    else // Устанавливаем адрес для EXTDEF
                    {
                        if (sn.Address == -1 && address != -1)
                        {
                            sn.Address = address;
                            return;
                        }
                        else
                            throw new AssemblerException(ErrorFormatter.LabelAlreadyDefined(0, symbolicName, textLine));
                    }
                }
                else
                    throw new AssemblerException(ErrorFormatter.LabelAlreadyDefined(0, symbolicName, textLine));
            }

            TSI.Add(new SymbolicName()
            {
                Name = symbolicName.ToUpper(),
                Address = address,
                Section = section,
                Type = type
            });
        }

        /// <summary>
        /// Добавляет символическое имя в TSI (старый метод для обратной совместимости)
        /// </summary>
        public void PushToTSI(string symbolicName, int address)
        {
            PushToTSI(symbolicName, address, currentSection.Name, string.Empty, string.Empty);
        }

        /// <summary>
        /// Проверяет, что всем внешним именам (EXTDEF) присвоены адреса
        /// </summary>
        public void TSICheck()
        {
            if (TSI.Any(n => n.Type == "ВИ" && n.Address == -1))
                throw new AssemblerException("Не всем внешним именам было присвоено значение");
        }

        /// <summary>
        /// Добавляет запись в таблицу настройки (TN)
        /// </summary>
        public void PushToTN(string address, string? label, string section)
        {
            var tnLine = new TNLine() { 
                Address = address, 
                Label = label, 
                Section = section 
            };

            TN.Add(tnLine); 
        }

        /// <summary>
        /// Добавляет секцию в список секций
        /// </summary>
        public void AddSection(Section section)
        {
            if(Sections.Select(s => s.Name).Contains(section.Name))
                throw new AssemblerException($"Все имена секций должны быть уникальными: {section.Name}");

            OverflowCheck(Sections.Sum(s => s.Length) + section.Length, $"{section.Name}", 0); 

            Sections.Add(section); 
        }

        public void ClearTSI()
        {
            TSI.Clear();
        }

        public void ClearTN()
        {
            TN.Clear();
        }

        public void ClearSections()
        {
            Sections.Clear(); 
        }

        public void ClearRelocationTable()
        {
            TN.Clear();
        }

        /// <summary>
        /// Устанавливает режим адресации для валидации
        /// </summary>
        /// <param name="addressingMode">Режим адресации</param>
        public void SetAddressingMode(AddressingType addressingMode)
        {
            AddressingMode = addressingMode;
        }

        /// <summary>
        /// Очищает режим адресации
        /// </summary>
        public void ClearAddressingMode()
        {
            AddressingMode = null;
        }

        public void OverflowCheck(int value, string textLine)
        {
            if (value < 0 || value > maxAddress)
                throw new AssemblerException($"Выход за границы выделенной памяти: {textLine}");
        }

        /// <summary>
        /// Перегрузка для обратной совместимости
        /// </summary>
        public void OverflowCheck(int value, string textLine, int lineNumber)
        {
            OverflowCheck(value, textLine);
        }
    }
}

