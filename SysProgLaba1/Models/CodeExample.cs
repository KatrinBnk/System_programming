using System.Text.Json.Serialization;

namespace SysProgLaba1.Models
{
    /// <summary>
    /// Класс для хранения примера кода
    /// </summary>
    public class CodeExample
    {
        /// <summary>
        /// Отображаемое название примера
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Код примера
        /// </summary>
        [JsonIgnore]
        public string Code { get; set; }

        /// <summary>
        /// Путь к файлу с кодом (используется при загрузке из конфига)
        /// </summary>
        public string File { get; set; }

        public CodeExample()
        {
        }

        public CodeExample(string name, string code)
        {
            Name = name;
            Code = code;
        }

        // Переопределяем ToString для отображения в ComboBox
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Конфигурация примеров кода
    /// </summary>
    public class ExamplesConfig
    {
        public CodeExample[] Examples { get; set; }
    }
}

