using System;
using System.Collections.Generic;
using System.Linq;

namespace DeliveryApp
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine(message);
    }

    public interface ISposibOplaty
    {
        void Oplatity(decimal suma);
    }

    public class OplataKartoyu : ISposibOplaty
    {
        private readonly ILogger _logger;
        public OplataKartoyu(ILogger logger) => _logger = logger;

        public void Oplatity(decimal suma) 
            => _logger.Log($"[Оплата] Карткою успішно сплачено: {suma} грн.");
    }

    public class OplataGotivkoyu : ISposibOplaty
    {
        private readonly ILogger _logger;
        public OplataGotivkoyu(ILogger logger) => _logger = logger;

        public void Oplatity(decimal suma) 
            => _logger.Log($"[Оплата] Готівкою успішно сплачено: {suma} грн.");
    }

    public class Order
    {
        public int Nomer { get; }
        public string Opys { get; }
        public double Vaga { get; }
        public double Vidstan { get; }

        public Order(int nomer, string opys, double vaga, double vidstan)
        {
            Nomer = nomer;
            Opys = opys;
            Vaga = vaga;
            Vidstan = vidstan;
        }
    }

    public abstract class Transport
    {
        public string Nazva { get; }
        public double MaxVaga { get; }
        public bool Zaynyatiy { get; private set; }
        public Order PotochneZamovlennya { get; private set; }

        protected Transport(string nazva, double maxVaga)
        {
            Nazva = nazva;
            MaxVaga = maxVaga;
            Zaynyatiy = false;
            PotochneZamovlennya = null;
        }

        public abstract decimal CalculateCost(Order zam);

        public virtual bool CanTake(Order zam)
        {
            if (Zaynyatiy) return false;
            return zam.Vaga <= MaxVaga;
        }

        public void PryznachytyZamovlennya(Order zam)
        {
            if (!CanTake(zam))
            {
                throw new InvalidOperationException("Транспорт не може взяти це замовлення.");
            }
            PotochneZamovlennya = zam;
            Zaynyatiy = true;
        }

        public void ZvilnytyTransport()
        {
            PotochneZamovlennya = null;
            Zaynyatiy = false;
        }
    }

    public class Bike : Transport
    {
        public Bike(double maxVaga) : base("Велосипед", maxVaga) { }

        public override decimal CalculateCost(Order zam) => 20m + (5m * (decimal)zam.Vidstan);

        public override bool CanTake(Order zam) => base.CanTake(zam) && zam.Vidstan <= 10;
    }

    public class Avtomobil : Transport
    {
        public Avtomobil(double maxVaga) : base("Автомобіль", maxVaga) { }

        public override decimal CalculateCost(Order zam) 
            => 50m + (8m * (decimal)zam.Vidstan) + (1m * (decimal)zam.Vaga);
    }

    public class Vantajivka : Transport
    {
        public Vantajivka(double maxVaga) : base("Вантажівка", maxVaga) { }

        public override decimal CalculateCost(Order zam) 
            => 200m + (15m * (decimal)zam.Vidstan) + (0.5m * (decimal)zam.Vaga);
    }

    public class DeliveryService
    {
        private readonly List<Transport> _spysokTransportu = new List<Transport>();
        private readonly List<Order> _aktyvniZamovlennya = new List<Order>();
        private readonly List<string> _istoriya = new List<string>();
        private readonly ILogger _logger;

        public DeliveryService(ILogger logger) => _logger = logger;

        public void DodatyTransport(Transport t) => _spysokTransportu.Add(t);
        public void DodatyOrder(Order z) => _aktyvniZamovlennya.Add(z);

        public IReadOnlyList<Order> GetAktyvniZamovlennya() => _aktyvniZamovlennya.AsReadOnly();
        public IReadOnlyList<string> GetIstoriya() => _istoriya.AsReadOnly();

        public List<(Transport Transport, decimal Cost)> OtrymatyDostupniyTransport(Order zam)
        {
            var rezult = new List<(Transport, decimal)>();
            foreach (var t in _spysokTransportu)
            {
                if (t.CanTake(zam))
                {
                    rezult.Add((t, t.CalculateCost(zam)));
                }
            }
            return rezult;
        }

        public bool PryznachytyНайкращийТранспорт(Order zam)
        {
            if (!_aktyvniZamovlennya.Contains(zam))
            {
                _logger.Log($"[Помилка] Замовлення №{zam.Nomer} не знайдено в системі.");
                return false;
            }

            var dostupniy = OtrymatyDostupniyTransport(zam);

            if (dostupniy.Count == 0)
            {
                _logger.Log($"[Помилка] Немає вільного або підходящого транспорту для замовлення №{zam.Nomer}.");
                return false;
            }

            // Автоматичний пошук транспорту з мінімальною вартістю
            var naikrashchiy = dostupniy.OrderBy(x => x.Cost).First().Transport;

            naikrashchiy.PryznachytyZamovlennya(zam);
            _logger.Log($"[Система] Замовлення №{zam.Nomer} автоматично призначено на найвигідніший транспорт: {naikrashchiy.Nazva}.");
            return true;
        }

        public void Dostavyty(int orderId, ISposibOplaty oplata)
        {
            Transport t = _spysokTransportu.Find(x => x.PotochneZamovlennya?.Nomer == orderId);
            
            if (t == null)
            {
                _logger.Log($"[Помилка] Не знайдено активного транспорту для замовлення №{orderId}.");
                return;
            }

            Order zam = t.PotochneZamovlennya;
            decimal vartist = t.CalculateCost(zam);

            _logger.Log($"\n[Система] {t.Nazva} доставляє замовлення №{zam.Nomer}...");
            oplata.Oplatity(vartist);

            _aktyvniZamovlennya.Remove(zam);
            _istoriya.Add($"Замовлення №{zam.Nomer} '{zam.Opys}' доставлено на {t.Nazva}. Вартість: {vartist} грн.");
            
            t.ZvilnytyTransport();
            _logger.Log($"[Система] Замовлення №{zam.Nomer} виконано. Транспорт ({t.Nazva}) знову вільний.");
        }
    }

    // Новий клас, який повністю відповідає за відображення інтерфейсу користувача (UI)
    public class ConsoleUI
    {
        private readonly ILogger _logger;

        public ConsoleUI(ILogger logger) => _logger = logger;

        public void PokazatyVartistDlyaZamovlennya(Order zam, List<(Transport Transport, decimal Cost)> dostupniy)
        {
            _logger.Log($"\n--- Доступний transport для замовлення №{zam.Nomer} ({zam.Opys}): ---");
            if (dostupniy.Count == 0)
            {
                _logger.Log("Немає вільного або відповідного транспорту.");
                return;
            }
            foreach (var item in dostupniy)
            {
                _logger.Log($"- {item.Transport.Nazva}: Вартість = {item.Cost} грн.");
            }
        }

        public void PokazatyStatus(DeliveryService sluzhba)
        {
            _logger.Log("\n================ СТАТУС СИСТЕМИ ================");
            _logger.Log("--- Активні замовлення: ---");
            var aktyvni = sluzhba.GetAktyvniZamovlennya();
            if (aktyvni.Count == 0) _logger.Log("Немає.");
            foreach (var z in aktyvni) _logger.Log($"- №{z.Nomer}: {z.Opys}");

            _logger.Log("\n--- Історія доставок: ---");
            var istoriya = sluzhba.GetIstoriya();
            if (istoriya.Count == 0) _logger.Log("Порожньо.");
            foreach (var zapis in istoriya) _logger.Log(zapis);
            _logger.Log("=================================================");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();
            DeliveryService sluzhba = new DeliveryService(logger);
            ConsoleUI ui = new ConsoleUI(logger);

            sluzhba.DodatyTransport(new Bike(12));
            sluzhba.DodatyTransport(new Avtomobil(150));
            sluzhba.DodatyTransport(new Vantajivka(4000));

            Order z1 = new Order(1, "Піца", 2, 5);
            Order z2 = new Order(2, "Пральна машина", 60, 15);
            Order z3 = new Order(3, "Цегла", 500, 20);

            sluzhba.DodatyOrder(z1);
            sluzhba.DodatyOrder(z2);
            sluzhba.DodatyOrder(z3);

            // Виводимо вартість через шар UI, отримуючи чисті дані з сервісу
            ui.PokazatyVartistDlyaZamovlennya(z1, sluzhba.OtrymatyDostupniyTransport(z1));
            ui.PokazatyVartistDlyaZamovlennya(z2, sluzhba.OtrymatyDostupniyTransport(z2));

            // Автоматичний розумний підбір транспорту службою доставки
            sluzhba.PryznachytyНайкращийТранспорт(z1); 
            ui.PokazatyStatus(sluzhba);
            sluzhba.Dostavyty(1, new OplataKartoyu(logger));

            sluzhba.PryznachytyНайкращийТранспорт(z2); // Автоматично вибере автомобіль (найдешевший з підходящих)
            sluzhba.Dostavyty(2, new OplataGotivkoyu(logger));

            ui.PokazatyStatus(sluzhba);
            
            Console.ReadLine();
        }
    }
}
