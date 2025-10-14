using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using SysProgLaba1Shared.Dto;
using SysProgLaba1Shared.Exceptions;


namespace SysProgLaba1Shared.Models
{
    public class Command 
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = default!;
        
        [Required]
        [Range(0,255)]
        public int Code { get; set; } = default!;

        [Required]
        [Range(1, 255)] 
        public int Length { get; set; } = default!; 

        public Command() {}

        public Command(CommandDto dto)
        {
            string command = $"{dto.Name} {dto.Code} {dto.Length}";

            if (string.IsNullOrEmpty(dto.Name))
                throw new AssemblerException($"Название команды не может быть пустым: {command}");

            // Влидация имени команды: начинается с буквы, содержит только буквы и цифры, минимум 1 символ
            if (!Regex.IsMatch(dto.Name, @"^[a-zA-Z][a-zA-Z0-9]*$"))
            {
                if (!Regex.IsMatch(dto.Name, @"^[a-zA-Z]"))
                    throw new AssemblerException($"Название команды должно начинаться с латинской буквы: {command}");
                else
                    throw new AssemblerException($"Название команды должно состоять только из латинских букв и цифр: {command}");
            }

            Name = dto.Name;


            // Валидация кода команды
            int code;
            try { code = Convert.ToInt32(dto.Code, 16); } 
            catch { throw new AssemblerException($"Код команды должен быть целым числом в 16-ричном формате:  {command}"); } 

            if (code < 0 || code > 255)
                throw new AssemblerException($"Код команды должен быть значением от 0 до 255:  {command}");

            Code = code;


            // Валидация длины команды 
            int length;
            try { length = Convert.ToInt32(dto.Length, 16); }
            catch { throw new AssemblerException($"Код команды должен быть целым числом в 16-ричном формате:  {command}"); }

            if (length < 1 || length > 4 || length == 3)
                throw new AssemblerException($"Длина команды должна быть 1,2 или 4:  {command}");

            Length = length; 
        }
    }
}

