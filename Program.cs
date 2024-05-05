using Microsoft.EntityFrameworkCore;

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

    public class ProductContext : DbContext // Поменял название на Product
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: "Konstantin_MSK");
        }
    }

    public class Program
    {
        static void Main()
        {
            using (var context = new ProductContext())
            {
                // Создание нового товара
                var productsToAdd = new List<Product>
                {
                    new Product { Name = "Мертвые души", Price = 50, QuantityInStock = 10 },
                    new Product { Name = "Евгений Онегин", Price = 75, QuantityInStock = 20 },
                    new Product { Name = "Тихий Дон", Price = 45, QuantityInStock = 50 },
                    new Product { Name = "Четвертая высота", Price = 80, QuantityInStock = 3 }
                };

                context.Products.AddRange(productsToAdd);
                context.SaveChanges();

                // Чтение всех товаров
                var allProducts = context.Products.ToList();
                foreach (var product in allProducts)
                {
                    Console.WriteLine($"ID товара: {product.ProductId}, Название: {product.Name}, Цена: {product.Price}");
                }

                // Обновление товара
                //var productToUpdate = context.Products.FirstOrDefault(p => p.Name == "Евгений Онегин");
                //if (productToUpdate != null)
                //{
                    //productToUpdate.Price = 100;
                    //context.SaveChanges();

                    // Вывод измененной цены
                   // Console.WriteLine($"Измененная цена для товара \"{productToUpdate.Name}\": {productToUpdate.Price}");
                //}

                // Удаление товара
                var productToDelete = context.Products.FirstOrDefault(p => p.ProductId == 2);
                if (productToDelete != null)
                {
                    context.Products.Remove(productToDelete);
                    context.SaveChanges();
                }

                // Пользователь выбрал Мертвые души и Тихий Дон)
                var selectedProductIds = new List<int> { 1, 3, 4 };

                // Создаем новый заказ
                var newOrder = new Order
                {
                    OrderItems = new List<OrderItem>(),
                    DeliveryDate = DateTime.Now.AddDays(3) // Срок доставки
                };

                // Добавляем выбранные товары в заказ
                foreach (var productId in selectedProductIds)
                {
                    var productToAdd = context.Products.FirstOrDefault(p => p.ProductId == productId);
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

                var producs = context.Products.Where(p => productsIds.Contains(p.ProductId)).Select(x => new
                {
                    x.Price,
                    x.ProductId
                }).ToDictionary(x => x.ProductId);

                // Общая стоимость товара. Считаем через словарь, а не через прохождение всего списка товаров
                decimal totalOrderCost = newOrder.OrderItems.Sum(item => item.QuantityOrdered * producs.GetValueOrDefault(item.ProductId)?.Price ?? 0);

                // Сохраняем заказ
                context.Orders.Add(newOrder);
                context.SaveChanges();

                // Инфорамция о заказе
                Console.WriteLine($"Order ID: {newOrder.OrderId}");
                Console.WriteLine($"Total Order Cost: {totalOrderCost:C}");

                // Обновление количества товаров на складе
                foreach (var orderItem in newOrder.OrderItems)
                {
                    var productToUpdate = context.Products.FirstOrDefault(p => p.ProductId == orderItem.ProductId);
                    if (productToUpdate != null)
                    {
                        productToUpdate.QuantityInStock -= orderItem.QuantityOrdered;
                    }
                }
                context.SaveChanges();
            }
        
        }
    }
}
