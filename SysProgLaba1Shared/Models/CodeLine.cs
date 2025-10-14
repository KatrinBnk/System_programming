namespace SysProgLaba1Shared.Models
{
    public class CodeLine
    {
        public string? Label { get; set; }
        public string Command { get; set; } = default!; 
        public string? FirstOperand { get; set; } 
        public string? SecondOperand { get; set; } 
    }
}

