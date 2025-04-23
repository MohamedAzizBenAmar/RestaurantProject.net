using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using restaurant.Data;
using restaurant.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant.Controllers
{
    public class ProductController : Controller
    {
        private Repository<Product> products;
        private Repository<Ingredient> ingredients;
        private Repository<Category> categories;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            products = new Repository<Product>(context);
            ingredients = new Repository<Ingredient>(context);
            categories = new Repository<Category>(context);
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string searchName, int? categoryId, int? ingredientId)
        {
            ViewBag.Categories = await categories.GetAllAsync();
            ViewBag.Ingredients = await ingredients.GetAllAsync();

            var queryOptions = new QueryOptions<Product>
            {
                Includes = "Category,ProductIngredients.Ingredient"
            };

            var productsList = await products.GetAllAsync();

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

            return View(productsList);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchName, int? categoryId, int? ingredientId)
        {
            var productsList = await products.GetAllAsync();

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

            return PartialView("_ProductList", productsList);
        }

        [HttpGet]
        public async Task<IActionResult> AddEdit(int id)
        {
            ViewBag.Ingredients = await ingredients.GetAllAsync();
            ViewBag.Categories = await categories.GetAllAsync();
            if (id == 0)
            {
                ViewBag.Operation = "Add";
                return View(new Product());
            }
            else
            {
                Product product = await products.GetByIdAsync(id, new QueryOptions<Product>
                {
                    Includes = "ProductIngredients.Ingredient, Category"
                });
                ViewBag.Operation = "Edit";
                return View(product);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(Product product, int[] ingredientIds, int catId)
        {
            ViewBag.Ingredients = await ingredients.GetAllAsync();
            ViewBag.Categories = await categories.GetAllAsync();

            if (ModelState.IsValid)
            {
                // Handle image upload
                if (product.ImageFile != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var maxFileSize = 5 * 1024 * 1024;
                    var extension = Path.GetExtension(product.ImageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("ImageFile", "Only JPG, JPEG, PNG, or GIF files are allowed.");
                        return View(product);
                    }

                    if (product.ImageFile.Length > maxFileSize)
                    {
                        ModelState.AddModelError("ImageFile", "Image file size must be less than 5MB.");
                        return View(product);
                    }

                    // Delete old image if updating
                    if (product.ProductId != 0)
                    {
                        var existingProduct = await products.GetByIdAsync(product.ProductId, new QueryOptions<Product>());
                        if (!string.IsNullOrEmpty(existingProduct.ImageUrl) && existingProduct.ImageUrl != "https://via.placeholder.com/150")
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", existingProduct.ImageUrl);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                    }

                    // Save new image
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = uniqueFileName;
                }

                if (product.ProductId == 0)
                {
                    product.CategoryId = catId;
                    product.ProductIngredients = new List<ProductIngredient>();
                    foreach (int id in ingredientIds)
                    {
                        product.ProductIngredients.Add(new ProductIngredient { IngredientId = id });
                    }
                    await products.AddAsync(product);
                    return RedirectToAction("Index", "Product");
                }
                else
                {
                    var existingProduct = await products.GetByIdAsync(product.ProductId, new QueryOptions<Product> { Includes = "ProductIngredients" });
                    if (existingProduct == null)
                    {
                        ModelState.AddModelError("", "Product not found.");
                        return View(product);
                    }

                    // Update fields
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;
                    existingProduct.CategoryId = catId;

                    // Preserve existing ImageUrl if no new image is uploaded
                    if (product.ImageFile != null)
                    {
                        existingProduct.ImageUrl = product.ImageUrl;
                    }
                    // Else, existingProduct.ImageUrl remains unchanged

                    // Update ingredients
                    existingProduct.ProductIngredients.Clear();
                    foreach (int id in ingredientIds)
                    {
                        existingProduct.ProductIngredients.Add(new ProductIngredient { IngredientId = id });
                    }

                    try
                    {
                        await products.UpdateAsync(existingProduct);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error: {ex.GetBaseException().Message}");
                        return View(product);
                    }
                }
                return RedirectToAction("Index", "Product");
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await products.GetByIdAsync(id, new QueryOptions<Product>());
                if (product != null && !string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "https://via.placeholder.com/150")
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", product.ImageUrl);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                await products.DeleteAsync(id);
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("", "Product not found.");
                return RedirectToAction("Index");
            }
        }
    }
}