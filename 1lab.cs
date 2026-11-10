using System;

namespace TriangleLab
{
    public class Triangle
    {
        public double SideA { get; }
        public double SideB { get; }
        public double SideC { get; }

        public Triangle(double a, double b, double c)
        {
            ValidateTriangleOrThrow(a, b, c);

            SideA = a;
            SideB = b;
            SideC = c;
        }

        private void ValidateTriangleOrThrow(double a, double b, double c)
        {
            if (!AreSidesPositive(a, b, c))
            {
                throw new ArgumentException("Всі сторони трикутника повинні бути строго більшими за 0!");
            }

            if (!DoesTriangleExist(a, b, c))
            {
                throw new ArgumentException("Трикутник із такими сторонами не може існувати (сума двох сторін має бути більшою за третю)!");
            }
        }

        public static bool AreSidesPositive(double a, double b, double c)
        {
            return a > 0 && b > 0 && c > 0;
        }

        public static bool DoesTriangleExist(double a, double b, double c)
        {
            return (a + b > c) && (a + c > b) && (b + c > a);
        }

        public double CalculatePerimeter()
        {
            return SideA + SideB + SideC;
        }

        public double CalculateSemiPerimeter()
        {
            return CalculatePerimeter() / 2;
        }

        public double CalculateArea()
        {
            double p = CalculateSemiPerimeter();
            return Math.Sqrt(p * (p - SideA) * (p - SideB) * (p - SideC));
        }

        public bool IsEquilateral()
        {
            return SideA == SideB && SideB == SideC;
        }

        public bool IsIsosceles()
        {
            return SideA == SideB || SideB == SideC || SideA == SideC;
        }

        public bool IsRightAngled()
        {
            double eps = 0.000001;
            
            bool condition1 = Math.Abs(SideA * SideA + SideB * SideB - SideC * SideC) < eps;
            bool condition2 = Math.Abs(SideA * SideA + SideC * SideC - SideB * SideB) < eps;
            bool condition3 = Math.Abs(SideB * SideB + SideC * SideC - SideA * SideA) < eps;

            return condition1 || condition2 || condition3;
        }

        public string DetermineTriangleType()
        {
            if (IsEquilateral())
            {
                return "Рівносторонній";
            }
            if (IsRightAngled())
            {
                return "Прямокутний";
            }
            if (IsIsosceles())
            {
                return "Рівнобедрений";
            }

            return "Довільний";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Лабораторна робота №1: ООП Трикутник ===");

            double a = ReadSide("a");
            double b = ReadSide("b");
            double c = ReadSide("c");

            try
            {
                Triangle triangle = new Triangle(a, b, c);
                PrintResults(triangle);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"\n[Помилка] {ex.Message}");
            }
        }

        static double ReadSide(string sideName)
        {
            while (true)
            {
                Console.Write($"Введіть довжину сторони {sideName}: ");
                if (double.TryParse(Console.ReadLine(), out double result))
                {
                    return result;
                }
                Console.WriteLine("Некоректне введення! Будь ласка, введіть число.");
            }
        }

        static void PrintResults(Triangle triangle)
        {
            Console.WriteLine("\n=== Результати обчислень ===");
            Console.WriteLine($"Периметр трикутника: {triangle.CalculatePerimeter():F2}");
            Console.WriteLine($"Площа трикутника (за Героном): {triangle.CalculateArea():F2}");
            Console.WriteLine($"Вид трикутника за сторонами: {triangle.DetermineTriangleType()}");
        }
    }
}
