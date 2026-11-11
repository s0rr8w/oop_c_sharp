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

    public interface IPaymentMethod
    {
        void Pay(decimal amount);
    }

    public class CardPayment : IPaymentMethod
    {
        private readonly ILogger _logger;
        public CardPayment(ILogger logger) => _logger = logger;

        public void Pay(decimal amount) 
            => _logger.Log($"[Payment] Paid successfully by card: {amount} UAH.");
    }

    public class CashPayment : IPaymentMethod
    {
        private readonly ILogger _logger;
        public CashPayment(ILogger logger) => _logger = logger;

        public void Pay(decimal amount) 
            => _logger.Log($"[Payment] Paid successfully by cash: {amount} UAH.");
    }

    public class Order
    {
        public int Number { get; }
        public string Description { get; }
        public double Weight { get; }
        public double Distance { get; }

        public Order(int number, string description, double weight, double distance)
        {
            Number = number;
            Description = description;
            Weight = weight;
            Distance = distance;
        }
    }

    public abstract class Transport
    {
        public string Name { get; }
        public double MaxWeight { get; }
        public bool IsBusy { get; private set; }
        public Order CurrentOrder { get; private set; }

        protected Transport(string name, double maxWeight)
        {
            Name = name;
            MaxWeight = maxWeight;
            IsBusy = false;
            CurrentOrder = null;
        }

        public abstract decimal CalculateCost(Order order);

        public virtual bool CanTake(Order order)
        {
            if (IsBusy) return false;
            return order.Weight <= MaxWeight;
        }

        public void AssignOrder(Order order)
        {
            if (!CanTake(order))
            {
                throw new InvalidOperationException("Transport cannot take this order.");
            }
            CurrentOrder = order;
            IsBusy = true;
        }

        public void ReleaseTransport()
        {
            CurrentOrder = null;
            IsBusy = false;
        }
    }

    public class Bike : Transport
    {
        public Bike(double maxWeight) : base("Bike", maxWeight) { }

        public override decimal CalculateCost(Order order) => 20m + (5m * (decimal)order.Distance);

        public override bool CanTake(Order order) => base.CanTake(order) && order.Distance <= 10;
    }

    public class Car : Transport
    {
        public Car(double maxWeight) : base("Car", maxWeight) { }

        public override decimal CalculateCost(Order order) 
            => 50m + (8m * (decimal)order.Distance) + (1m * (decimal)order.Weight);
    }

    public class Truck : Transport
    {
        public Truck(double maxWeight) : base("Truck", maxWeight) { }

        public override decimal CalculateCost(Order order) 
            => 200m + (15m * (decimal)order.Distance) + (0.5m * (decimal)order.Weight);
    }

    public class DeliveryService
    {
        private readonly List<Transport> _transportList = new List<Transport>();
        private readonly List<Order> _activeOrders = new List<Order>();
        private readonly List<string> _history = new List<string>();
        private readonly ILogger _logger;

        public DeliveryService(ILogger logger) => _logger = logger;

        public void AddTransport(Transport t) => _transportList.Add(t);
        public void AddOrder(Order o) => _activeOrders.Add(o);

        public IReadOnlyList<Order> GetActiveOrders() => _activeOrders.AsReadOnly();
        public IReadOnlyList<string> GetHistory() => _history.AsReadOnly();

        public List<(Transport Transport, decimal Cost)> GetAvailableTransport(Order order)
        {
            var result = new List<(Transport, decimal)>();
            foreach (var t in _transportList)
            {
                if (t.CanTake(order))
                {
                    result.Add((t, t.CalculateCost(order)));
                }
            }
            return result;
        }

        public bool AssignBestTransport(Order order)
        {
            if (!_activeOrders.Contains(order))
            {
                _logger.Log($"[Error] Order #{order.Number} not found in the system.");
                return false;
            }

            var available = GetAvailableTransport(order);

            if (available.Count == 0)
            {
                _logger.Log($"[Error] No available or suitable transport for order #{order.Number}.");
                return false;
            }

            var bestTransport = available.OrderBy(x => x.Cost).First().Transport;

            bestTransport.AssignOrder(order);
            _logger.Log($"[System] Order #{order.Number} automatically assigned to the most profitable transport: {bestTransport.Name}.");
            return true;
        }

        public void Deliver(int orderId, IPaymentMethod payment)
        {
            Transport t = _transportList.Find(x => x.CurrentOrder?.Number == orderId);
            
            if (t == null)
            {
                _logger.Log($"[Error] Active transport for order #{orderId} not found.");
                return;
            }

            Order order = t.CurrentOrder;
            decimal cost = t.CalculateCost(order);

            _logger.Log($"\n[System] {t.Name} is delivering order #{order.Number}...");
            payment.Pay(cost);

            _activeOrders.Remove(order);
            _history.Add($"Order #{order.Number} '{order.Description}' delivered by {t.Name}. Cost: {cost} UAH.");
            
            t.ReleaseTransport();
            _logger.Log($"[System] Order #{order.Number} completed. Transport ({t.Name}) is free again.");
        }
    }

    public class ConsoleUI
    {
        private readonly ILogger _logger;

        public ConsoleUI(ILogger logger) => _logger = logger;

        public void ShowCostForOrder(Order order, List<(Transport Transport, decimal Cost)> available)
        {
            _logger.Log($"\n--- Available transport for order #{order.Number} ({order.Description}): ---");
            if (available.Count == 0)
            {
                _logger.Log("No free or suitable transport available.");
                return;
            }
            foreach (var item in available)
            {
                _logger.Log($"- {item.Transport.Name}: Cost = {item.Cost} UAH.");
            }
        }

        public void ShowStatus(DeliveryService service)
        {
            _logger.Log("\n================ SYSTEM STATUS ================");
            _logger.Log("--- Active orders: ---");
            var active = service.GetActiveOrders();
            if (active.Count == 0) _logger.Log("None.");
            foreach (var o in active) _logger.Log($"- #{o.Number}: {o.Description}");

            _logger.Log("\n--- Delivery history: ---");
            var history = service.GetHistory();
            if (history.Count == 0) _logger.Log("Empty.");
            foreach (var record in history) _logger.Log(record);
            _logger.Log("=================================================");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();
            DeliveryService service = new DeliveryService(logger);
            ConsoleUI ui = new ConsoleUI(logger);

            service.AddTransport(new Bike(12));
            service.AddTransport(new Car(150));
            service.AddTransport(new Truck(4000));

            Order o1 = new Order(1, "Pizza", 2, 5);
            Order o2 = new Order(2, "Washing Machine", 60, 15);
            Order o3 = new Order(3, "Bricks", 500, 20);

            service.AddOrder(o1);
            service.AddOrder(o2);
            service.AddOrder(o3);

            ui.ShowCostForOrder(o1, service.GetAvailableTransport(o1));
            ui.ShowCostForOrder(o2, service.GetAvailableTransport(o2));

            service.AssignBestTransport(o1); 
            ui.ShowStatus(service);
            service.Deliver(1, new CardPayment(logger));

            service.AssignBestTransport(o2); 
            service.Deliver(2, new CashPayment(logger));

            ui.ShowStatus(service);
            
            Console.ReadLine();
        }
    }
}
