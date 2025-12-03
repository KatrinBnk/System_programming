namespace SysProgLaba1Shared.Models
{
    public class Section
    {
        public string Name { get; set; } = default!;
        public int StartAddress { get; set; } = 0;
        public int EndAddress { get; set; } = 0;
        public int Length { get; set; } = 0; 
    }
}

