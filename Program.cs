using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver; // Добавляем драйвер для работы с MongoDB
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Product
{
    public class Product
    {

        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public DateTime DeliveryDate { get; set; }
    }

    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantityOrdered { get; set; }
    }

    //public class ProductContext : DbContext
    //{
    //    public DbSet<Product> Products { get; set; }
    //    public DbSet<Order> Orders { get; set; }

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    {
    //        optionsBuilder.UseInMemoryDatabase(databaseName: "Konstantin_MSK");
    //    }
    //}

    public class ProductContext2
    {
        private readonly IMongoDatabase _database = null;


        public ProductContext2()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            if (client != null) _database = client.GetDatabase("Konstantin_MSK");
        }

        public IMongoCollection<Product> Products
        {
            get
            {
                BsonClassMap.RegisterClassMap<Product>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(c => c.ProductId).SetIdGenerator(StringObjectIdGenerator.Instance);
                });
                return _database.GetCollection<Product>("Product");
            }
        }

        public IMongoCollection<Order> Orders
        {
            get
            {
                return _database.GetCollection<Order>("Order");
            }
        }
    }

    public class Program
    {
        static void Main()
        {
            var context = new ProductContext2();

            // Создание нового товара
            var productsToAdd = new List<Product>
        {
            new Product { Name = "Мертвые души", Price = 50, QuantityInStock = 10 },
            new Product { Name = "Евгений Онегин", Price = 75, QuantityInStock = 20 },
            new Product { Name = "Тихий Дон", Price = 45, QuantityInStock = 50 },
            new Product { Name = "Четвертая высота", Price = 80, QuantityInStock = 3 }
        };

            context.Products.InsertMany(productsToAdd);

            // Чтение всех товаров
            var allProducts = context.Products.Find(_ => true).ToList();
            foreach (var product in allProducts)
            {
                Console.WriteLine($"ID товара: {product.ProductId}, Название: {product.Name}, Цена: {product.Price}");
            }

            // Удаление товара
            var productToDelete = context.Products.Find(p => p.ProductId == 2).FirstOrDefault();
            if (productToDelete != null)
            {
                context.Products.DeleteOne(product => product.ProductId == productToDelete.ProductId);
            }

            // Пользователь выбрал Мертвые души и Тихий Дон)
            var selectedProductIds = new List<int> { 1, 3, 4 };

            // Создаем новый заказ
            var newOrder = new Order
            {
                OrderItems = new List<OrderItem>(),
                DeliveryDate = DateTime.Now.AddDays(3)
            };

            // Добавляем выбранные товары в заказ
            foreach (var productId in selectedProductIds)
            {
                var productToAdd = context.Products.Find(p => p.ProductId == productId).FirstOrDefault();
                if (productToAdd != null)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = productToAdd.ProductId,
                        ProductName = productToAdd.Name,
                        QuantityOrdered = 1 // Добавляем по одной еденице товара
                    };
                    newOrder.OrderItems.Add(orderItem);
                }
            }

            var productsIds = newOrder.OrderItems.Select(oi => oi.ProductId).ToList();

            var producs = context.Products.Find(p => productsIds.Contains(p.ProductId)).ToList().Select(x => new
            {
                x.Price,
                x.ProductId
            }).ToDictionary(x => x.ProductId);

            // Общая стоимость товара, Через поиск по словарю, а не прохождение всего списка каждый раз
            decimal totalOrderCost = newOrder.OrderItems.Sum(item => item.QuantityOrdered * producs.GetValueOrDefault(item.ProductId)?.Price ?? 0);

            // Сохраняем заказ
            context.Orders.InsertOne(newOrder);

            // Инфорамция о заказе
            Console.WriteLine($"Order ID: {newOrder.OrderId}");
            Console.WriteLine($"Total Order Cost: {totalOrderCost:C}");

            // Обновление количества товаров на складе
            foreach (var orderItem in newOrder.OrderItems)
            {
                var productToUpdate = context.Products.Find(p => p.ProductId == orderItem.ProductId).FirstOrDefault();
                if (productToUpdate != null)
                {
                    productToUpdate.QuantityInStock -= orderItem.QuantityOrdered;
                    var filter = Builders<Product>.Filter.Eq("_id", productToUpdate.ProductId);
                    var update = Builders<Product>.Update.Set("QuantityInStock", productToUpdate.QuantityInStock);
                    context.Products.UpdateOne(filter, update);
                }
            }
        }
    }
}