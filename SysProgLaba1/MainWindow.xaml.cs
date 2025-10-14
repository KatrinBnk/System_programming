using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SysProgLaba1Shared.Models;
using SysProgLaba1Shared;
using System.Text.Json;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Dto;
using SysProgLaba1Shared.Helpers;
using SysProgLaba1.Models;


namespace SysProgLaba1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Assembler Assembler {get; set; } = new Assembler();

        // Коллекция примеров кода
        public ObservableCollection<CodeExample> CodeExamples { get; set; }

        private TextBox SourceCodeTextBox { get; set; }
        private TextBox SourceCodeLineNumbersTextBox { get; set; }

        // Таблица кодов операций
        private TextBox CommandsTextBox { get; set; }

        // Вспомогатеьная таблица (результат первого прохода)
        private TextBox FirstPassTextBox { get; set; }

        // Таблица символических имен (далее ТСИ)
        private TextBox TSITextBox { get; set; }

        //Двоичный код (результат второго прохода)
        private TextBox SecondPassTextBox { get; set; } 

        // Ошибки первого прохода 
        private TextBox FirstPassErrorsTextBox { get; set; }

        // Ошибки второго прохода 
        private TextBox SecondPassErrorsTextBox { get; set; }

        // кнопки 
        private Button FirstPassButton { get; set; }
        private Button SecondPassButton { get; set; }

        // Выпадающий список примеров
        private ComboBox ExamplesComboBox { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация примеров кода
            InitializeCodeExamples();

            SourceCodeTextBox = this.SourceCode_TextBox;
            SourceCodeLineNumbersTextBox = this.SourceCodeLineNumbers_TextBox;
            
            // Загружаем первый пример по умолчанию
            if (CodeExamples.Count > 0)
            {
                SourceCodeTextBox.Text = CodeExamples[0].Code;
            }
            UpdateLineNumbers();

            CommandsTextBox = this.Commands_TextBox;
            CommandsTextBox.Text = string.Join("\n", Assembler.AvailibleCommands.Select(c => $"{c.Name} {c.Code} {c.Length}")); 

            FirstPassTextBox = this.FirstPass_TextBox;
            SecondPassTextBox = this.SecondPass_TextBox; 

            TSITextBox = this.TSI_TextBox;

            FirstPassErrorsTextBox = this.FirstPassErrors_TextBox; 
            SecondPassErrorsTextBox = this.SecondPassErrors_TextBox;

            FirstPassButton = this.FirstPass_Button; 
            SecondPassButton = this.SecondPass_Button;

            // Привязываем ComboBox
            ExamplesComboBox = this.Examples_ComboBox;
            ExamplesComboBox.ItemsSource = CodeExamples;
            ExamplesComboBox.SelectedIndex = 0; // Выбираем первый элемент по умолчанию
        }

        private void InitializeCodeExamples()
        {
            CodeExamples = new ObservableCollection<CodeExample>();

            // Загружаем примеры из конфигурационного файла
            string configFilePath = "examples.json";
            
            try
            {
                if (File.Exists(configFilePath))
                {
                    string jsonContent = File.ReadAllText(configFilePath);
                    var config = JsonSerializer.Deserialize<ExamplesConfig>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (config?.Examples != null)
                    {
                        foreach (var example in config.Examples)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(example.File) && File.Exists(example.File))
                                {
                                    example.Code = File.ReadAllText(example.File);
                                    CodeExamples.Add(example);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Файл не найден: {example.File}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке примера '{example.Name}': {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Конфигурационный файл не найден: {configFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при чтении конфигурации: {ex.Message}");
            }
        }


        private void FirstPass_ButtonClick(object sender, RoutedEventArgs e)
        {
            // Отключаем кнопку второго прохода до успешного завершения
            SecondPassButton.IsEnabled = false;
            
            // Очищаем результаты второго прохода
            SecondPassTextBox.Text = null;
            SecondPassErrorsTextBox.Text = null;

            try
            {
                TSITextBox.Text = null;
                FirstPassTextBox.Text = null; 
                FirstPassErrorsTextBox.Text = null; 

                var newCommands = Parser.TextToCommandDtos(CommandsTextBox.Text);
                Assembler.SetAvailibleCommands(newCommands);

                Assembler.ClearTSI();

                var sourceCode = Parser.ParseCode(SourceCodeTextBox.Text); 
                FirstPassTextBox.Text = string.Join("\n", Assembler.FirstPass(sourceCode));
                TSITextBox.Text = string.Join("\n", Assembler.TSI.Select(w => $"{w.Name} {w.Address.ToString("X6")}")); 
                
                // Первый проход успешен - включаем кнопку второго прохода
                SecondPassButton.IsEnabled = true;
            }
            catch (AssemblerException ex)
            {
                FirstPassErrorsTextBox.Text = $"{ex.Message}"; 
                // Кнопка второго прохода остается отключенной при ошибке
            }
        }

        private void SecondPass_ButtonClick(object sender, RoutedEventArgs e)
        {
            SecondPassTextBox.Text = null;
            SecondPassErrorsTextBox.Text = null;

            if (FirstPassTextBox.Text == String.Empty) return; 

            try
            {
                var firstPassCode = Parser.ParseCode(FirstPassTextBox.Text); 
                SecondPassTextBox.Text = string.Join("\n", Assembler.SecondPass(firstPassCode));
            }
            catch(AssemblerException ex)
            {
                SecondPassErrorsTextBox.Text = $"{ex.Message}";

            }
        }

        // доп кнопка для сброса проходов
        private void Reset_ButtonClick(object sender, RoutedEventArgs e)
        {
            // Очистка результатов проходов
            FirstPassTextBox.Text = null;
            TSITextBox.Text = null;
            FirstPassErrorsTextBox.Text = null;
            SecondPassTextBox.Text = null;
            SecondPassErrorsTextBox.Text = null;
            
            // Очистка ТСИ в ассемблере
            Assembler.ClearTSI();
            
            // Отключаем кнопку второго прохода (нужен новый первый проход)
            SecondPassButton.IsEnabled = false;
        }

        // доп кнопка для загрузки выбранного примера
        private void LoadCode_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (ExamplesComboBox.SelectedItem is CodeExample selectedExample)
            {
                SourceCodeTextBox.Text = selectedExample.Code;
            }
        }

        private void SourceCode_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
        }

        private void SourceCode_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Синхронизация прокрутки номеров строк с кодом
            SourceCodeLineNumbersTextBox.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void UpdateLineNumbers()
        {
            // Подсчитываем количество строк
            int lineCount = SourceCodeTextBox.Text.Split('\n').Length;
            
            // Генерируем номера строк
            var lineNumbers = new System.Text.StringBuilder();
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.AppendLine(i.ToString());
            }
            
            SourceCodeLineNumbersTextBox.Text = lineNumbers.ToString();
        }
    }
}

