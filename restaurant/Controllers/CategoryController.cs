using Microsoft.AspNetCore.Mvc;
using restaurant.Data;
using restaurant.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant.Controllers
{
    public class CategoryController : Controller
    {
        private readonly Repository<Category> _categories;

        public CategoryController(ApplicationDbContext context)
        {
            _categories = new Repository<Category>(context);
        }

        // GET: Category/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _categories.GetAllAsync();
                Console.WriteLine($"Index: Loaded {categories.Count()} categories");
                return View(categories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Index error: {ex.Message}");
                TempData["Error"] = "Error loading categories.";
                return View(Enumerable.Empty<Category>());
            }
        }

        // GET: Category/Search
        [HttpGet]
        public async Task<IActionResult> Search(string searchName)
        {
            try
            {
                var categories = await _categories.GetAllAsync();
                Console.WriteLine($"Search: Loaded {categories.Count()} categories, searchName: {searchName}");
                if (!string.IsNullOrEmpty(searchName))
                {
                    categories = categories.Where(c => c.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase));
                }
                return PartialView("_CategoryList", categories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                return StatusCode(500, "Error loading categories.");
            }
        }

        // POST: Category/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind("Name")] Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingCategory = (await _categories.GetAllAsync())
                        .FirstOrDefault(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase));
                    if (existingCategory != null)
                    {
                        Console.WriteLine($"Add: Category name '{category.Name}' already exists");
                        return Json(new { success = false, errors = new[] { "Category name already exists." } });
                    }
                    category.Products = new List<Product>();
                    await _categories.AddAsync(category);
                    Console.WriteLine($"Add: Added category '{category.Name}'");
                    TempData["Success"] = "Category added successfully.";
                    return Json(new { success = true });
                }
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine($"Add error: Invalid model, errors: {string.Join(", ", errors)}");
                return Json(new { success = false, errors });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Add error: {ex.Message}");
                return Json(new { success = false, errors = new[] { "Error adding category." } });
            }
        }

        // GET: Category/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _categories.GetByIdAsync(id, new QueryOptions<Category> { Includes = "Products" });
                if (category == null)
                {
                    Console.WriteLine($"Edit GET: Category ID {id} not found");
                    return Json(new { success = false, message = "Category not found." });
                }
                if (category.Products != null && category.Products.Any())
                {
                    Console.WriteLine($"Edit GET: Category ID {id} has linked products");
                    return Json(new { success = false, message = "Cannot edit category because it is linked to products." });
                }
                Console.WriteLine($"Edit GET: Loaded category ID {id}, Name: {category.Name}");
                return Json(new { success = true, category = new { categoryId = category.CategoryId, name = category.Name } });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Edit GET error: {ex.Message}");
                return Json(new { success = false, message = $"Error retrieving category: {ex.Message}" });
            }
        }

        // POST: Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("CategoryId,Name")] Category category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    if (ModelState.ContainsKey("CategoryId") && ModelState["CategoryId"].Errors.Any())
                    {
                        Console.WriteLine($"Edit POST: Invalid CategoryId");
                        return Json(new { success = false, message = "Invalid or missing Category ID." });
                    }
                    Console.WriteLine($"Edit POST: Invalid model, errors: {string.Join(", ", errors)}");
                    return Json(new { success = false, errors });
                }
                var existingCategory = await _categories.GetByIdAsync(category.CategoryId, new QueryOptions<Category> { Includes = "Products" });
                if (existingCategory == null)
                {
                    Console.WriteLine($"Edit POST: Category ID {category.CategoryId} not found");
                    return Json(new { success = false, message = "Category not found." });
                }
                if (existingCategory.Products != null && existingCategory.Products.Any())
                {
                    Console.WriteLine($"Edit POST: Category ID {category.CategoryId} has linked products");
                    return Json(new { success = false, message = "Cannot edit category because it is linked to products." });
                }
                existingCategory.Name = category.Name;
                await _categories.UpdateAsync(existingCategory);
                Console.WriteLine($"Edit POST: Updated category ID {category.CategoryId}, Name: {category.Name}");
                TempData["Success"] = "Category updated successfully.";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Edit POST error: {ex.Message}");
                return Json(new { success = false, message = $"Error updating category: {ex.Message}" });
            }
        }

        // GET: Category/DeleteCheck
        [HttpGet]
        public async Task<IActionResult> DeleteCheck(int id)
        {
            try
            {
                var category = await _categories.GetByIdAsync(id, new QueryOptions<Category> { Includes = "Products" });
                if (category == null)
                {
                    Console.WriteLine($"DeleteCheck GET: Category ID {id} not found");
                    return Json(new { success = false, message = "Category not found." });
                }
                if (category.Products != null && category.Products.Any())
                {
                    Console.WriteLine($"DeleteCheck GET: Category ID {id} has linked products");
                    return Json(new { success = false, message = "Cannot delete category because it is linked to products." });
                }
                Console.WriteLine($"DeleteCheck GET: Loaded category ID {id}, Name: {category.Name}");
                return Json(new { success = true, category = new { categoryId = category.CategoryId, name = category.Name } });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteCheck GET error: {ex.Message}");
                return Json(new { success = false, message = $"Error retrieving category: {ex.Message}" });
            }
        }

        // POST: Category/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([Bind("id")] int id)
        {
            try
            {
                var category = await _categories.GetByIdAsync(id, new QueryOptions<Category> { Includes = "Products" });
                if (category == null)
                {
                    Console.WriteLine($"Delete POST: Category ID {id} not found");
                    TempData["Error"] = "Category not found.";
                    return Json(new { success = false, message = "Category not found." });
                }
                if (category.Products != null && category.Products.Any())
                {
                    Console.WriteLine($"Delete POST: Category ID {id} has linked products");
                    TempData["Error"] = "Cannot delete category because it is linked to products.";
                    return Json(new { success = false, message = "Cannot delete category because it is linked to products." });
                }
                await _categories.DeleteAsync(id);
                Console.WriteLine($"Delete POST: Deleted category ID {id}");
                TempData["Success"] = "Category deleted successfully.";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete POST error: {ex.Message}");
                TempData["Error"] = $"Cannot delete category: {ex.Message}";
                return Json(new { success = false, message = $"Error deleting category: {ex.Message}" });
            }
        }
    }
}