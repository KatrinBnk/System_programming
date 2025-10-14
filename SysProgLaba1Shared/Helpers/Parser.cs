using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SysProgLaba1Shared.Dto;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Models;


namespace SysProgLaba1Shared.Helpers
{
    public static class Parser
    {
        public static List<List<string>> ParseCode(string input)
        {
            string[] lines = Regex.Split(input, @"\r?\n");

            var result = new List<List<string>>();

            foreach (string line in lines)
            {
                List<string> words = Regex.Matches(line, @"((?:[CX])""[^""]*(?:""[^""]*)*""|\S+)").Select(s => s.Value).ToList();

                var trimmedAndFilteredWords = words
                                                .Select(word => word.Trim())
                                                .Where(word => !string.IsNullOrWhiteSpace(word));

                if (trimmedAndFilteredWords.Count() != 0)
                {
                    result.Add(trimmedAndFilteredWords.ToList());
                }
            }

            return result;
        }

        public static List<CommandDto> TextToCommandDtos(string text)
        {
            var lines = Parser.ParseCode(text);

            foreach (List<string> line in lines)
            {
                if(line.Count != 3)
                    throw new AssemblerException($"Неверный формат таблицы команд. Ожидается 3 элемента (Имя Код Длина).\nКод: {string.Join(" ", line)}");
            }

            var commandDtos = lines.Select(l => new CommandDto() {
                Name = l[0], 
                Code = l[1], 
                Length = l[2] 
            }).ToList();

            return commandDtos; 
        }
    }
}

