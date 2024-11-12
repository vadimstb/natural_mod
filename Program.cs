using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        const int SERIES_SIZE_LIMIT = 75 * 1024 * 1024;  // Лимит размера серии (100 МБ)

        CreateTestFile("input.txt", 175000000);  // 1 ГБ 

        Stopwatch stopwatch = new Stopwatch();

        string sortedSeriesFile = ModSeriesSort("input.txt", SERIES_SIZE_LIMIT);

        stopwatch.Start();
        PerformExternalMergeSort(sortedSeriesFile, "output.txt");
        stopwatch.Stop();


        File.Delete(sortedSeriesFile);

        Console.WriteLine($"{stopwatch.Elapsed.Minutes} минут, {stopwatch.Elapsed.Seconds} секунд, {stopwatch.Elapsed.Milliseconds} миллисекунд.");
    }

    private static void CreateTestFile(string fileName, int numElements)
    {
        Random rand = new Random();
        using (StreamWriter writer = new StreamWriter(fileName))
        {
            for (int i = 0; i < numElements; i++)
            {
                writer.WriteLine(rand.Next(1, 10000));
            }
        }
    }

    // Основная функция для внешней сортировки
    static void PerformExternalMergeSort(string inputFilePath, string outputFilePath)
    {
        string filePart1 = "part1.txt";
        string filePart2 = "part2.txt";

        SplitFileNaturally(inputFilePath, filePart1, filePart2);

        // Объединяем пока одна из частей не станет пустой
        while (!IsEmptyFile(filePart1) && !IsEmptyFile(filePart2))
        {
            string mergedFile = "mergedFile.txt";
            MergeFiles(filePart1, filePart2, mergedFile);
            SplitFileNaturally(mergedFile, filePart1, filePart2);  // Повторяем разбиение
        }

        File.Copy(IsEmptyFile(filePart1) ? filePart2 : filePart1, outputFilePath, true);

        File.Delete(filePart1);
        File.Delete(filePart2);
        File.Delete("mergedFile.txt");
    }

    #region Подготовка серий фиксированного размера

    // Фиксированные серии для сортировки
    private static string ModSeriesSort(string inputFilePath, int maxSeriesSize)
    {
        string tempFile = "TempInput.txt";
        using (StreamReader reader = new StreamReader(inputFilePath))
        {
            List<int> buffer = new List<int>();
            long currentBufferSize = 0;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int number = int.Parse(line);
                buffer.Add(number);
                currentBufferSize += sizeof(int);

                // Размер буфера превышен, сортируем и записываем его в файл
                if (currentBufferSize >= maxSeriesSize)
                {
                    buffer.Sort();
                    AppendBufferToFile(buffer, tempFile);
                    buffer.Clear();
                    currentBufferSize = 0;
                }
            }

            // Оставшиеся данные
            if (buffer.Count > 0)
            {
                buffer.Sort();
                AppendBufferToFile(buffer, tempFile);
            }
        }
        return tempFile;
    }

    // Запись буфера
    private static void AppendBufferToFile(List<int> buffer, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            foreach (var number in buffer)
            {
                writer.WriteLine(number);
            }
        }
    }

    #endregion

    // Разделение файла на две части
    static void SplitFileNaturally(string inputFilePath, string outputFilePath1, string outputFilePath2)
    {
        using (StreamReader reader = new StreamReader(inputFilePath))
        using (StreamWriter writer1 = new StreamWriter(outputFilePath1))
        using (StreamWriter writer2 = new StreamWriter(outputFilePath2))
        {
            string line;
            StreamWriter currentWriter = writer1;
            int? previousValue = null;
            bool firstElement = true;

            while ((line = reader.ReadLine()) != null)
            {
                int currentValue = int.Parse(line);

                if (firstElement || currentValue >= previousValue)
                {
                    currentWriter.WriteLine(currentValue);
                }
                else
                {
                    currentWriter.WriteLine("---");  // Разделитель серий
                    currentWriter = (currentWriter == writer1) ? writer2 : writer1;
                    currentWriter.WriteLine(currentValue);
                }

                previousValue = currentValue;
                firstElement = false;
            }

            currentWriter.WriteLine("---");
        }
    }

    // Слияние двух файлов
    static void MergeFiles(string inputFile1, string inputFile2, string outputFile)
    {
        using (StreamReader reader1 = new StreamReader(inputFile1))
        using (StreamReader reader2 = new StreamReader(inputFile2))
        using (StreamWriter writer = new StreamWriter(outputFile))
        {
            string line1 = ReadNextEntry(reader1, out bool isFileEnd1);
            string line2 = ReadNextEntry(reader2, out bool isFileEnd2);

            while (!isFileEnd1 || !isFileEnd2)
            {
                if (line1 == null)
                {
                    writer.WriteLine(line2);
                    AppendRemainingEntries(reader2, writer);
                    line1 = ReadNextEntry(reader1, out isFileEnd1);
                    line2 = ReadNextEntry(reader2, out isFileEnd2);
                }
                else if (line2 == null)
                {
                    writer.WriteLine(line1);
                    AppendRemainingEntries(reader1, writer);
                    line1 = ReadNextEntry(reader1, out isFileEnd1);
                    line2 = ReadNextEntry(reader2, out isFileEnd2);
                }
                else
                {
                    if (int.Parse(line1) <= int.Parse(line2))
                    {
                        writer.WriteLine(line1);
                        line1 = ReadNextEntry(reader1, out isFileEnd1);
                    }
                    else
                    {
                        writer.WriteLine(line2);
                        line2 = ReadNextEntry(reader2, out isFileEnd2);
                    }
                }
            }

            if (!isFileEnd1) AppendRemainingEntries(reader1, writer);
            if (!isFileEnd2) AppendRemainingEntries(reader2, writer);
        }
    }

    // Чтение следующего числа или ---
    static string ReadNextEntry(StreamReader reader, out bool isEndOfFile)
    {
        string line = reader.ReadLine();
        isEndOfFile = line == null;

        return line == "---" ? null : line;
    }

    // Запись оставшихся
    static void AppendRemainingEntries(StreamReader reader, StreamWriter writer)
    {
        char[] buffer = new char[8192];
        StringBuilder remainingEntries = new StringBuilder();
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            if (line == "---") break;

            remainingEntries.AppendLine(line);

            if (remainingEntries.Length >= buffer.Length)
            {
                writer.Write(remainingEntries.ToString());
                remainingEntries.Clear();
            }
        }

        if (remainingEntries.Length > 0)
        {
            writer.Write(remainingEntries.ToString());
        }
    }

    static bool IsEmptyFile(string filePath)
    {
        return new FileInfo(filePath).Length == 0;
    }
}
