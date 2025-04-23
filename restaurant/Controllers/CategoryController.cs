using Microsoft.AspNetCore.Mvc;
using restaurant.Data;
using restaurant.Models;
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
            var categories = await _categories.GetAllAsync();
            return View(categories);
        }

        // GET: Category/Search
        [HttpGet]
        public async Task<IActionResult> Search(string searchName)
        {
            var categories = await _categories.GetAllAsync();

            if (!string.IsNullOrEmpty(searchName))
            {
                categories = categories.Where(c => c.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase));
            }

            return PartialView("_CategoryList", categories);
        }

        // POST: Category/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind("Name")] Category category)
        {
            if (ModelState.IsValid)
            {
                category.Products = new List<Product>();
                await _categories.AddAsync(category);
                return Json(new { success = true });
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
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
                    return Json(new { success = false, message = "Category not found." });
                }
                if (category.Products != null && category.Products.Any())
                {
                    return Json(new { success = false, message = "Cannot edit category because it is linked to products." });
                }
                return Json(new { success = true, category = new { category.CategoryId, category.Name } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error retrieving category: {ex.Message}" });
            }
        }

        // POST: Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("CategoryId,Name")] Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                if (ModelState.ContainsKey("CategoryId") && ModelState["CategoryId"].Errors.Any())
                {
                    return Json(new { success = false, message = "Invalid or missing Category ID." });
                }
                return Json(new { success = false, errors = errors });
            }
            try
            {
                var existingCategory = await _categories.GetByIdAsync(category.CategoryId, new QueryOptions<Category> { Includes = "Products" });
                if (existingCategory == null)
                {
                    return Json(new { success = false, message = "Category not found." });
                }
                if (existingCategory.Products != null && existingCategory.Products.Any())
                {
                    return Json(new { success = false, message = "Cannot edit category because it is linked to products." });
                }
                existingCategory.Name = category.Name;
                await _categories.UpdateAsync(existingCategory);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating category: {ex.Message}" });
            }
        }

        // POST: Category/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _categories.GetByIdAsync(id, new QueryOptions<Category> { Includes = "Products" });
                if (category == null)
                {
                    TempData["Error"] = "Category not found.";
                    return RedirectToAction("Index");
                }
                if (category.Products != null && category.Products.Any())
                {
                    TempData["Error"] = "Cannot delete category because it is linked to products.";
                    return RedirectToAction("Index");
                }
                await _categories.DeleteAsync(id);
                TempData["Success"] = "Category deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Cannot delete category: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}