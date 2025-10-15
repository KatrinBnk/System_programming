using System;

namespace SysProgLaba1Shared.Models
{
    /// <summary>
    /// Типы адресации в ассемблере
    /// </summary>
    public enum AddressingType
    {
        /// <summary>
        /// Только прямая адресация (запрещена относительная)
        /// </summary>
        DirectOnly,
        
        /// <summary>
        /// Только относительная адресация (запрещена прямая)
        /// </summary>
        RelativeOnly,
        
        /// <summary>
        /// Смешанная адресация (разрешены оба типа)
        /// </summary>
        Mixed
    }
}
