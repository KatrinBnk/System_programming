namespace SysProgLaba1Shared.Models
{
    public class SymbolicName
    {
        public string Name { get; set; } = default!; 
        public int Address { get; set; } = -1;
        public string Section { get; set; } = default!; 
        public string Type { get; set; } = default!; // Пустая строка для обычных меток, "ВИ" для EXTDEF, "ВС" для EXTREF
    }
}

