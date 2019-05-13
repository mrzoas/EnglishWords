using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ConsoleApplication1
{
    class Program
    {
        static string pathToInputFile = @"lightTest.txt"; // Файл со множеством слов, для которого необходимо построить конечный автомат
        static string pathToFSMFile = @"FSM.txt"; // Файл, в котором будет сохранено описание конечного автомата

        static Char[] abc = "abcdefghijklmnopqrstuvwxyz'".ToCharArray();
        static bool[,] pairs = new bool[32767, 32767];

        struct oneRule
        {
            public byte ch;
            public int st, nSt;
            public oneRule(int state, byte symbol, int newState) { st = state; ch = symbol; nSt = newState; }
        }

        static int maxWordLength(string[] stringArray)
        {
            int max = 0;
            for (long i = 0; i < stringArray.Length; i++)
                max = (max > stringArray[i].Length) ? max : stringArray[i].Length;
            return max;
        }

        static void saveFSM(int[][] rules, int sizeOfStates, bool[] allowallowedState)
        {
            StreamWriter file = new System.IO.StreamWriter(pathToFSMFile);

            file.WriteLine(sizeOfStates);
            for (int i = 0; i < sizeOfStates; i++) file.Write(allowallowedState[i] ? "1" : "0");
            file.WriteLine();

            for (int i = 0; i < sizeOfStates; i++)
                for (int j = 0; j < 27; j++)
                    if (rules[j][i] != 0)
                        file.WriteLine("({1},{0})->{2}", abc[j], i, rules[j][i]);
            file.Close();
        }

        static int[][] makeFSM(string[] words, ref int sizeOfStates, ref bool[] allowallowedState)
        {
            int[][] rules = new int[27][]; // Таблица правил ([номер символа][состояние] -> новое состояние)
            for (int i = 0; i < 27; i++) rules[i] = new int[1600000];
            int size = 1;

            bool[] allowStates = new bool[1600000]; // Массив с допустимыми состояниями. Их не больше, чем слов

            char[] word; // Каждое конкретное слово в виде массива символов
            int state = 1; // Текущее состояние
            int CharCode; // Номер текущего символа в нашем алфавите
            for (int i = 0; i < words.Length; i++)
            {
                word = words[i].ToCharArray();
                state = 1;
                for (int j = 0; j < word.Length; j++)
                {
                    CharCode = Array.IndexOf(abc, word[j]); // Получаем код символа в нашем алфавите
                    if (rules[CharCode][state] == 0) // Если в таком состоянии этот символ ещё не встречался, то создаем для этого случая новое состояние
                    {
                        size++;
                        rules[CharCode][state] = size;
                    }
                    state = rules[CharCode][state]; // Переходим в это состояние
                    // Переходим к следующему символу слова
                }
                allowStates[state] = true; // Конечное состояние считаем допустимым
            }
            size++;

            rules = minimizeFSM(rules, ref size, ref allowStates); // Минимизация

            allowallowedState = allowStates;
            sizeOfStates = size;

            return rules;
        }

        static int[][] minimizeFSM(int[][] rules, ref int sizeOfStates, ref bool[] allowStates)
        {
            int size = sizeOfStates;
            
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    pairs[i, j] = ((allowStates[i] == true && allowStates[j] == false) || (allowStates[i] == false && allowStates[j] == true));

            bool found;
            do
            {
                found = false;
                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                        if (pairs[i, j] == false)
                            for (byte k = 0; k < 27; k++)
                                if (pairs[rules[k][i], rules[k][j]] == true)
                                {
                                    pairs[i, j] = true;
                                    found = true;
                                }
            }
            while (found);

            int[] eClass = new int[size];
            for (int i = 0; i < size; i++) eClass[i] = i;

            for (int i = 0; i < size; i++)
                for (int j = i + 1; j < size; j++)
                    if (pairs[i, j] == false)
                        eClass[j] = eClass[i];

            //for (int i = 0; i < size; i++) Console.Write(" {0}", eClass[i]);

            int countStates = 0;

            for (int i = 0; i < size; i++)
            {
                if (eClass[i] < countStates) continue;
                    int zamena = eClass[i];
                    for (int j = i; j < size; j++)
                    {
                        if (eClass[j] == zamena && zamena > countStates) eClass[j] = countStates;
                    }
               countStates++;
            }
            countStates++;
            //Console.WriteLine();

            //for (int i = 0; i < size; i++) Console.Write(" {0}", eClass[i]);

            bool[] allowedStates = new bool[600000];
            int[][] rulesNew = new int[27][]; // Таблица правил ([номер символа][состояние] -> новое состояние)
            for (int i = 0; i < 27; i++) rulesNew[i] = new int[600000];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < 27; j++)
                    rulesNew[j][eClass[i]] = eClass[rules[j][i]];
                allowedStates[eClass[i]] = allowStates[i];
            }
            size = countStates;

            sizeOfStates = size;
            allowStates = allowedStates;
            return rulesNew;
        }

        static int[][] addFSM(int[][] rules, ref int sizeOfStates, ref bool[] allowStates, int[][] rulesADD, int sizeOfStatesADD, bool[] allowStatesADD)
        {
            for (int i = sizeOfStates + 1; i < sizeOfStates + sizeOfStatesADD; i++)
            {
                for (int j = 0; j < 27; j++)
                {
                    rules[j][i] = rulesADD[j][i - sizeOfStates] + sizeOfStates;
                }
                allowStates[i] = allowStatesADD[i - sizeOfStates];
            }
            sizeOfStates = sizeOfStates + sizeOfStatesADD;

            return minimizeFSM(rules, ref sizeOfStates, ref allowStates);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Размер исходного файла: {0} байт", new FileInfo(pathToInputFile).Length);
            string[] readText = File.ReadAllLines(pathToInputFile); // получение массива слов из файла
            Console.WriteLine("Количество слов: {0}", readText.Length);            
            Console.WriteLine("Максимальная длина: {0}", maxWordLength(readText));

            for (long i = 0; i < readText.Length; i++) // перевод в нижний регистр
                readText[i] = readText[i].ToLower();
            string[] words = readText.Distinct().ToArray(); // получение оригинальных слов из входного массива
            Console.WriteLine("Количество оригинальных слов: {0}", words.Length);

            int[][] realRules = new int[27][]; // Таблица правил ([номер символа][состояние] -> новое состояние)
            for (int i = 0; i < 27; i++) realRules[i] = new int[1600000];
            int realSize = 1;

            bool[] realAllowedStates = new bool[1600000]; // Массив с допустимыми состояниями. Их не больше, чем слов

            realRules = makeFSM(readText, ref realSize, ref realAllowedStates);

            Console.WriteLine("Число состояний: {0}", realSize);

            int count = 0;
            for (int i = 0; i < realSize; i++) // подсчет/вывод правил
                for (int j = 0; j < 27; j++)
                    if (realRules[j][i] != 0)
                    {
                        count++;
                        //Console.WriteLine("({0},{1}->{2})", i, abc[j], realRules[j][i]);
                    }
            Console.WriteLine("Число правил: {0}", count);


            // Сравнение по словарю
            // readText[4] = "aaaaa"; // Добавление несуществующего слова в массив
            bool test = true;
            int state = 1;
            char[] word;
            for (int i = 0; i < readText.Length; i++)
            {
                state = 1;
                word = readText[i].ToCharArray();
                for (int j = 0; j < word.Length; j++)
                {
                    state = realRules[Array.IndexOf(abc, word[j])][state];
                    if (state == 0) test = false;
                }
                if (realAllowedStates[state] == false) test = false;
            }
            if (test) Console.WriteLine("Тест успешен");
            else Console.WriteLine("Не найдены некоторые слова");

            saveFSM(realRules, realSize, realAllowedStates);
        }
    }
}