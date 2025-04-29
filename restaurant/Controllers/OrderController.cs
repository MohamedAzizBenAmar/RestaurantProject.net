using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using restaurant.Data;
using restaurant.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private Repository<Product> _products;
        private Repository<Order> _orders;
        private Repository<Category> _categories;
        private Repository<Ingredient> _ingredients;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _products = new Repository<Product>(context);
            _orders = new Repository<Order>(context);
            _categories = new Repository<Category>(context);
            _ingredients = new Repository<Ingredient>(context);
        }
        

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel") ?? new OrderViewModel
            {
                OrderItems = new List<OrderItemViewModel>(),
                Products = await _products.GetAllAsync()
            };
            ViewBag.Categories = await _categories.GetAllAsync();
            ViewBag.Ingredients = await _ingredients.GetAllAsync();
            return View(model);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddItem(int prodId, int prodQty)
        {
            // Check if the user is an admin
            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admins cannot add items to the cart.";
                return RedirectToAction("Create");
            }

            var product = await _context.Products.FindAsync(prodId);
            if (product == null)
            {
                return NotFound();
            }

            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel") ?? new OrderViewModel
            {
                OrderItems = new List<OrderItemViewModel>(),
                Products = await _products.GetAllAsync()
            };

            var existingItem = model.OrderItems.FirstOrDefault(oi => oi.ProductId == prodId);
            var totalQuantity = (existingItem?.Quantity ?? 0) + prodQty;

            if (totalQuantity > product.Stock)
            {
                ModelState.AddModelError("", $"Cannot add {prodQty} of {product.Name}. Only {product.Stock} available in stock.");
                ViewBag.Categories = await _categories.GetAllAsync();
                ViewBag.Ingredients = await _ingredients.GetAllAsync();
                return View("Create", model);
            }

            if (existingItem != null)
            {
                existingItem.Quantity = totalQuantity;
            }
            else
            {
                model.OrderItems.Add(new OrderItemViewModel
                {
                    ProductId = product.ProductId,
                    Price = product.Price,
                    Quantity = prodQty,
                    ProductName = product.Name
                });
            }

            model.TotalAmount = model.OrderItems.Sum(oi => oi.Price * oi.Quantity);
            HttpContext.Session.Set("OrderViewModel", model);
            return RedirectToAction("Create");
        }


        [HttpGet]
        public async Task<IActionResult> Search(string searchName, int? categoryId, int? ingredientId)
        {
            try
            {
                var queryOptions = new QueryOptions<Product>
                {
                    Includes = "Category,ProductIngredients.Ingredient"
                };
                var productsList = await _products.GetAllAsync();

                if (!string.IsNullOrEmpty(searchName))
                {
                    productsList = productsList.Where(p => p.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase));
                }

                if (categoryId.HasValue)
                {
                    productsList = productsList.Where(p => p.CategoryId == categoryId.Value);
                }

                if (ingredientId.HasValue)
                {
                    productsList = productsList.Where(p => p.ProductIngredients.Any(pi => pi.IngredientId == ingredientId.Value));
                }

                return PartialView("_OrderList", productsList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                return StatusCode(500, "Error loading products.");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");
            if (model == null || model.OrderItems.Count == 0)
            {
                return RedirectToAction("Create");
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateItem(int productId, int quantity)
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");
            if (model == null)
            {
                return RedirectToAction("Create");
            }

            var item = model.OrderItems.FirstOrDefault(oi => oi.ProductId == productId);
            if (item == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }
            if (quantity > product.Stock)
            {
                ModelState.AddModelError("", $"Cannot update quantity of {item.ProductName} to {quantity}. Only {product.Stock} available in stock.");
                return View("Cart", model);
            }

            if (quantity <= 0)
            {
                model.OrderItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            model.TotalAmount = model.OrderItems.Sum(oi => oi.Price * oi.Quantity);
            HttpContext.Session.Set("OrderViewModel", model);
            return RedirectToAction("Cart");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteItem(int productId)
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");
            if (model == null)
            {
                return RedirectToAction("Create");
            }

            var item = model.OrderItems.FirstOrDefault(oi => oi.ProductId == productId);
            if (item == null)
            {
                return NotFound();
            }

            model.OrderItems.Remove(item);
            model.TotalAmount = model.OrderItems.Sum(oi => oi.Price * oi.Quantity);
            HttpContext.Session.Set("OrderViewModel", model);
            return RedirectToAction("Cart");
        }
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");
            if (model == null || model.OrderItems.Count == 0)
            {
                return RedirectToAction("Create");
            }

            Order order = new Order
            {
                OrderDate = DateTime.Now,
                TotalAmount = model.TotalAmount,
                UserId = _userManager.GetUserId(User),
                Status = OrderStatus.Pending
            };

            foreach (var item in model.OrderItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });

                var product = await _context.Products.FindAsync(item.ProductId);
                product.Stock -= item.Quantity;
                await _products.UpdateAsync(product);
            }

            await _orders.AddAsync(order);
            HttpContext.Session.Remove("OrderViewModel");
            return RedirectToAction("ViewOrders");
        }

        [HttpGet]
        public async Task<IActionResult> ViewOrders()
        {
            var userId = _userManager.GetUserId(User);
            var userOrders = await _orders.GetAllByIdAsync(userId, "UserId", new QueryOptions<Order>
            {
                Includes = "OrderItems.Product"
            });
            return View(userOrders);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _orders.GetByIdAsync(orderId, new QueryOptions<Order> { Includes = "OrderItems.Product" });
            if (order == null || order.UserId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            if (order.Status == OrderStatus.Completed)
            {
                TempData["Error"] = "Completed orders cannot be canceled.";
                return RedirectToAction("ViewOrders");
            }

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Only pending orders can be canceled.";
                return RedirectToAction("ViewOrders");
            }

            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    await _products.UpdateAsync(product);
                }
            }

            order.Status = OrderStatus.Canceled;
            await _orders.UpdateAsync(order);
            TempData["Success"] = "Order canceled successfully.";
            return RedirectToAction("ViewOrders");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageOrders()
        {
            var allOrders = await _orders.GetAllAsync();
            return View(allOrders);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _orders.GetByIdAsync(orderId, new QueryOptions<Order> { Includes = "OrderItems.Product" });
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("ManageOrders");
            }

            if (!Enum.TryParse<OrderStatus>(status, out var newStatus))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction("ManageOrders");
            }

            // If status is changing to Canceled, restore stock
            if (newStatus == OrderStatus.Canceled && order.Status != OrderStatus.Canceled)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        await _products.UpdateAsync(product);
                    }
                }
            }
            // If status is changing from Canceled to another status, deduct stock
            else if (order.Status == OrderStatus.Canceled && newStatus != OrderStatus.Canceled)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        if (product.Stock < item.Quantity)
                        {
                            TempData["Error"] = $"Cannot update order status. Insufficient stock for {item.Product.Name}.";
                            return RedirectToAction("ManageOrders");
                        }
                        product.Stock -= item.Quantity;
                        await _products.UpdateAsync(product);
                    }
                }
            }

            order.Status = newStatus;
            await _orders.UpdateAsync(order);
            TempData["Success"] = "Order status updated successfully.";
            return RedirectToAction("ManageOrders");
        }
    }
}