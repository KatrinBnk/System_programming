using System;
using System.Collections.Generic;
using System.Linq;
using SysProgLaba1Shared;
using SysProgLaba1Shared.Exceptions;
using SysProgLaba1Shared.Helpers;
using SysProgLaba1Shared.Models;

int passed = 0;
int failed = 0;

// ═══════════════════════════════════════════════════════════
// ПОЗИТИВНЫЕ ТЕСТЫ — все 8 новых примеров проходят оба прохода
// ═══════════════════════════════════════════════════════════

RunPositive("forward_ref: оба прохода без ошибок", @"
PROG START 100h
     JMP NEXT
     LOADR1 A1
     LOADR2 A2
NEXT ADD R1 R2
     SAVER1 B
     INT 0
A1 WORD 10
A2 WORD 20
B WORD 1
     END 100h
", expectMRecords: 4);

RunPositive("backward_ref: относительная, нет M-записей", @"
PROG START 100h
A1 WORD 10
A2 WORD 20
B WORD 1
LOOP LOADR1 [A1]
     LOADR2 [A2]
     ADD R1 R2
     SAVER1 [B]
     JMP [LOOP]
     INT 0
     END 100h
", expectMRecords: 0);

RunPositive("self_jump: самопереход offset=-4", @"
PROG START 100h
LOOP JMP [LOOP]
     INT 0
     END 100h
", expectMRecords: 0, secondPassContains: "06FFFFFC");

RunPositive("no_memory_ops: только 2-байтовые, пустая таблица", @"
PROG START 100h
     ADD R1 R2
     ADD R1 R1
     INT 0
     END 100h
", expectMRecords: 0);

RunPositive("data_heavy: WORD, BYTE, C-строка, X-строка, RESB, RESW", @"
PROG START 0
S1 WORD 255
S2 BYTE 12h
S3 BYTE C""Hi!""
S4 BYTE X""FF01""
S5 RESB 4
S6 RESW 2
     LOADR1 S1
     LOADR2 S2
     SAVER1 S1
     INT 0
     END
", expectMRecords: 3);

RunPositive("start_zero: START 0, перемещаемый формат", @"
PROG START 0
     LOADR1 DATA
     SAVER1 RES
     INT 0
DATA WORD 42
RES WORD 1
     END
", expectMRecords: 2);

RunPositive("numeric_operand: числовой адрес — тест багфикса", @"
PROG START 100h
     JMP 150h
     LOADR1 200h
     INT 0
     END 100h
", expectMRecords: 0);

RunPositive("next_instr_jump: offset=0", @"
PROG START 100h
     JMP [NEXT]
NEXT ADD R1 R2
     INT 0
     END 100h
", expectMRecords: 0, secondPassContains: "06000000");


// ═══════════════════════════════════════════════════════════
// НЕГАТИВНЫЕ ТЕСТЫ — ошибки первого прохода
// ═══════════════════════════════════════════════════════════

RunNegativeFirstPass("Нет START: первая строка — команда",
    "JMP 100h\nEND",
    "START");

RunNegativeFirstPass("START без операнда",
    "PROG START\nEND",
    "требует один операнд");

RunNegativeFirstPass("START без метки",
    "START 100h\nEND",
    "метка");

RunNegativeFirstPass("Двойной START",
    "PROG START 100h\nPROG2 START 200h\nEND",
    "START");

RunNegativeFirstPass("Нет END",
    "PROG START 100h\nADD R1 R2",
    "END");

RunNegativeFirstPass("Дублирование метки",
    "PROG START 100h\nA WORD 1\nA WORD 2\nEND",
    "уже определена");

RunNegativeFirstPass("Неизвестная команда (парсер ловит раньше)",
    "PROG START 100h\nFOOBAR 100h\nEND",
    "не является командой");

RunNegativeFirstPass("WORD без операнда (парсер: 1 элемент — не команда)",
    "PROG START 100h\nWORD\nEND",
    "не является известной командой");

RunNegativeFirstPass("WORD значение 0 (вне диапазона)",
    "PROG START 100h\nWORD 0\nEND",
    "диапазон");

RunNegativeFirstPass("BYTE значение 256 (вне диапазона)",
    "PROG START 100h\nBYTE 256\nEND",
    "диапазон");

RunNegativeFirstPass("RESB значение 0 (вне диапазона)",
    "PROG START 100h\nRESB 0\nEND",
    "диапазон");

RunNegativeFirstPass("RESW значение 0 (вне диапазона)",
    "PROG START 100h\nRESW 0\nEND",
    "диапазон");

RunNegativeFirstPass("Команда 4 байта без операнда",
    "PROG START 100h\nJMP\nEND",
    "требует операнд");

RunNegativeFirstPass("Команда 4 байта: два операнда",
    "PROG START 100h\nJMP A B\nEND",
    "только один операнд");

RunNegativeFirstPass("Команда ADD без операндов",
    "PROG START 100h\nADD\nEND",
    "требует операнд");

RunNegativeFirstPass("Метка начинается с цифры",
    "PROG START 100h\n1ABC WORD 1\nEND",
    "");

RunNegativeFirstPass("Метка — имя регистра",
    "PROG START 100h\nR1 WORD 1\nEND",
    "регистр");

RunNegativeFirstPass("Метка — имя команды",
    "PROG START 100h\nJMP WORD 1\nEND",
    "");

RunNegativeFirstPass("Метка — имя директивы",
    "PROG START 100h\nWORD WORD 1\nEND",
    "");

RunNegativeFirstPass("Отрицательный адрес START",
    "PROG START -1\nEND",
    "");

RunNegativeFirstPass("END точка входа вне диапазона адресов",
    "PROG START 100h\nINT 0\nEND FFFFFFFh",
    "диапазон");

RunNegativeSecondPass("Второй проход: точка входа за пределами программы",
    firstPassLines: new List<string> {
        "PROG START 000100",
        "000100 18 00"
    },
    expectedFragment: "вне программы",
    overrideEndAddress: 0x999999);

RunNegativeFirstPass("Прямая адресация запрещена в режиме 'относительная'",
    "PROG START 100h\nJMP NEXT\nNEXT INT 0\nEND 100h",
    "адресация", AddressingType.RelativeOnly);

RunNegativeFirstPass("Относительная адресация запрещена в режиме 'прямая'",
    "PROG START 100h\nJMP [NEXT]\nNEXT INT 0\nEND 100h",
    "адресация", AddressingType.DirectOnly);


// ═══════════════════════════════════════════════════════════
// НЕГАТИВНЫЕ ТЕСТЫ — ошибки второго прохода
// ═══════════════════════════════════════════════════════════

RunNegativeSecondPass("Второй проход: некорректный тип адресации",
    firstPassLines: new List<string> {
        "PROG START 000100",
        "000100 07 000200"
    },
    expectedFragment: "тип адресации");


// ═══════════════════════════════════════════════════════════
// РЕЗУЛЬТАТЫ
// ═══════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine(new string('═', 60));
Console.ForegroundColor = passed > 0 ? ConsoleColor.Green : ConsoleColor.White;
Console.Write($"  PASSED: {passed}");
Console.ResetColor();
Console.Write("  |  ");
Console.ForegroundColor = failed > 0 ? ConsoleColor.Red : ConsoleColor.White;
Console.Write($"  FAILED: {failed}");
Console.ResetColor();
Console.Write($"  |  TOTAL: {passed + failed}");
Console.WriteLine();
Console.WriteLine(new string('═', 60));

return failed > 0 ? 1 : 0;


// ═══════════════════════════════════════════════════════════
// Вспомогательные методы
// ═══════════════════════════════════════════════════════════

void RunPositive(string name, string sourceCode, int expectMRecords, string? secondPassContains = null)
{
    try
    {
        var asm = new Assembler();
        asm.SetAddressingMode(AddressingType.Mixed);
        var parsed = Parser.ParseCode(sourceCode);
        var firstPassResult = asm.FirstPass(parsed);

        var firstPassParsed = Parser.ParseCode(string.Join("\n", firstPassResult));
        asm.ClearRelocationTable();
        var secondPassResult = asm.SecondPass(firstPassParsed);

        int mCount = secondPassResult.Count(l => l.StartsWith("M "));
        if (mCount != expectMRecords)
        {
            Fail(name, $"Ожидалось M-записей: {expectMRecords}, получено: {mCount}");
            return;
        }

        if (secondPassContains != null)
        {
            string joined = string.Join("\n", secondPassResult);
            if (!joined.Contains(secondPassContains, StringComparison.OrdinalIgnoreCase))
            {
                Fail(name, $"Ожидалась подстрока \"{secondPassContains}\" в выводе второго прохода.\nВывод:\n{joined}");
                return;
            }
        }

        Pass(name);
    }
    catch (Exception ex)
    {
        Fail(name, $"Неожиданное исключение: {ex.Message}");
    }
}

void RunNegativeFirstPass(string name, string sourceCode, string expectedFragment, AddressingType? mode = null)
{
    try
    {
        var asm = new Assembler();
        if (mode != null)
            asm.SetAddressingMode(mode.Value);
        else
            asm.SetAddressingMode(AddressingType.Mixed);

        var parsed = Parser.ParseCode(sourceCode);
        asm.FirstPass(parsed);

        Fail(name, "Исключение НЕ было выброшено (ожидалась ошибка)");
    }
    catch (AssemblerException ex)
    {
        if (string.IsNullOrEmpty(expectedFragment) || ex.Message.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase))
        {
            Pass(name, $"  → {Truncate(ex.Message, 90)}");
        }
        else
        {
            Fail(name, $"Ожидался фрагмент \"{expectedFragment}\" в сообщении:\n  {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Fail(name, $"Неожиданный тип исключения ({ex.GetType().Name}): {ex.Message}");
    }
}

void RunNegativeSecondPass(string name, List<string> firstPassLines, string expectedFragment, int? overrideEndAddress = null)
{
    try
    {
        var asm = new Assembler();
        asm.SetAddressingMode(AddressingType.Mixed);

        var startParts = firstPassLines[0].Split(' ');
        var startAddr = Convert.ToInt32(startParts[2], 16);

        // Собираем минимальный исходник для инициализации внутренних полей ассемблера
        string endDirective = overrideEndAddress.HasValue ? $"END {overrideEndAddress.Value}" : "END";
        string minimalSource = $"{startParts[0]} START {startAddr}\nINT 0\n{endDirective}";
        var parsedMinimal = Parser.ParseCode(minimalSource);
        try { asm.FirstPass(parsedMinimal); } catch { }

        var firstPassParsed = firstPassLines.Select(l => l.Split(' ').ToList()).ToList();
        asm.ClearRelocationTable();
        asm.SecondPass(firstPassParsed);

        Fail(name, "Исключение НЕ было выброшено (ожидалась ошибка)");
    }
    catch (AssemblerException ex)
    {
        if (string.IsNullOrEmpty(expectedFragment) || ex.Message.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase))
        {
            Pass(name, $"  → {Truncate(ex.Message, 90)}");
        }
        else
        {
            Fail(name, $"Ожидался фрагмент \"{expectedFragment}\" в сообщении:\n  {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Fail(name, $"Неожиданный тип исключения ({ex.GetType().Name}): {ex.Message}");
    }
}

void Pass(string name, string? detail = null)
{
    passed++;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("  ✓ PASS ");
    Console.ResetColor();
    Console.WriteLine(name);
    if (detail != null)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(detail);
        Console.ResetColor();
    }
}

void Fail(string name, string reason)
{
    failed++;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write("  ✗ FAIL ");
    Console.ResetColor();
    Console.WriteLine(name);
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($"    Причина: {reason}");
    Console.ResetColor();
}

string Truncate(string s, int maxLen)
{
    s = s.Replace("\r", "").Replace("\n", " | ");
    return s.Length <= maxLen ? s : s[..maxLen] + "…";
}
