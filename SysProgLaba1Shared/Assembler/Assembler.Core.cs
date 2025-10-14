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
        private int endAddress = 0;
        private int ip = 0; 

        // Список базовых команд (используются в примерах, дополняются через ЮИ)
        public List<Command> AvailibleCommands { get; set; } = [
            new Command(){ Name = "JMP", Code = 1, Length = 4 },
            new Command(){ Name = "LOADR1", Code = 2, Length = 4 },
            new Command(){ Name = "LOADR2", Code = 3, Length = 4 },
            new Command(){ Name = "ADD", Code = 4, Length = 2 },
            new Command(){ Name = "SAVER1", Code = 5, Length = 4 },
            new Command(){ Name = "INT", Code = 6, Length = 2 },
        ];

        // Директивы
        private readonly string[] AvailibleDirectives = ["START", "END", "WORD", "BYTE", "RESB", "RESW"]; 

        public List<SymbolicName> TSI = new(); 

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

        public void PushToTSI(string symbolicName, int address)
        {
            TSI.Add(new SymbolicName() {
                Name = symbolicName,
                Address = address 
            });
        }

        public void ClearTSI()
        {
            TSI.Clear();
        }

        public void OverflowCheck(int value, string textLine, int lineNumber = 0)
        {
            if (value < 0 || value > maxAddress)
            {
                throw new AssemblerException(ErrorFormatter.MemoryOverflow(value, maxAddress, textLine, lineNumber));
            }
        }
    }
}

