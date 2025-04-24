using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using restaurant.Data;
using restaurant.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant.Controllers
{
    [Authorize(Roles = "Admin")]
    public class IngredientController : Controller
    {
        private readonly Repository<Ingredient> _ingredients;

        public IngredientController(ApplicationDbContext context)
        {
            _ingredients = new Repository<Ingredient>(context);
        }

        // GET: Ingredient/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var ingredients = await _ingredients.GetAllAsync();
                Console.WriteLine($"Index: Loaded {ingredients.Count()} ingredients");
                return View(ingredients);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Index error: {ex.Message}");
                TempData["Error"] = "Error loading ingredients.";
                return View(Enumerable.Empty<Ingredient>());
            }
        }

        // GET: Ingredient/Search
        [HttpGet]
        public async Task<IActionResult> Search(string searchName)
        {
            try
            {
                var ingredients = await _ingredients.GetAllAsync();
                Console.WriteLine($"Search: Loaded {ingredients.Count()} ingredients, searchName: {searchName}");
                if (!string.IsNullOrEmpty(searchName))
                {
                    ingredients = ingredients.Where(i => i.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase));
                }
                return PartialView("_IngredientList", ingredients);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                return StatusCode(500, "Error loading ingredients.");
            }
        }

        // GET: Ingredient/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ingredient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Ingredient ingredient)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingIngredient = (await _ingredients.GetAllAsync())
                        .FirstOrDefault(i => i.Name.Equals(ingredient.Name, StringComparison.OrdinalIgnoreCase));
                    if (existingIngredient != null)
                    {
                        Console.WriteLine($"Create: Ingredient name '{ingredient.Name}' already exists");
                        return Json(new { success = false, errors = new[] { "Ingredient name already exists." } });
                    }
                    ingredient.ProductIngredients = new List<ProductIngredient>();
                    await _ingredients.AddAsync(ingredient);
                    Console.WriteLine($"Create: Added ingredient '{ingredient.Name}'");
                    TempData["Success"] = "Ingredient added successfully.";
                    return Json(new { success = true });
                }
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine($"Create error: Invalid model, errors: {string.Join(", ", errors)}");
                return Json(new { success = false, errors });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create error: {ex.Message}");
                return Json(new { success = false, errors = new[] { "Error adding ingredient." } });
            }
        }

        // GET: Ingredient/Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var ingredient = await _ingredients.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" });
                if (ingredient == null)
                {
                    Console.WriteLine($"Details GET: Ingredient ID {id} not found");
                    return Json(new { success = false, message = "Ingredient not found." });
                }
                Console.WriteLine($"Details GET: Loaded ingredient ID {id}, Name: {ingredient.Name}");
                return Json(new
                {
                    success = true,
                    ingredient = new
                    {
                        ingredientId = ingredient.IngredientId,
                        name = ingredient.Name,
                        products = ingredient.ProductIngredients?.Select(pi => pi.Product?.Name).Where(n => n != null).ToList() ?? new List<string>()
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Details GET error: {ex.Message}");
                return Json(new { success = false, message = $"Error retrieving ingredient: {ex.Message}" });
            }
        }

        // GET: Ingredient/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var ingredient = await _ingredients.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" });
                if (ingredient == null)
                {
                    Console.WriteLine($"Edit GET: Ingredient ID {id} not found");
                    return Json(new { success = false, message = "Ingredient not found." });
                }
                if (ingredient.ProductIngredients != null && ingredient.ProductIngredients.Any())
                {
                    Console.WriteLine($"Edit GET: Ingredient ID {id} has linked products");
                    return Json(new { success = false, message = "Cannot edit ingredient because it is linked to products." });
                }
                Console.WriteLine($"Edit GET: Loaded ingredient ID {id}, Name: {ingredient.Name}");
                return Json(new { success = true, ingredient = new { ingredientId = ingredient.IngredientId, name = ingredient.Name } });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Edit GET error: {ex.Message}");
                return Json(new { success = false, message = $"Error retrieving ingredient: {ex.Message}" });
            }
        }

        // POST: Ingredient/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("IngredientId,Name")] Ingredient ingredient)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    if (ModelState.ContainsKey("IngredientId") && ModelState["IngredientId"].Errors.Any())
                    {
                        Console.WriteLine($"Edit POST: Invalid IngredientId");
                        return Json(new { success = false, message = "Invalid or missing Ingredient ID." });
                    }
                    Console.WriteLine($"Edit POST: Invalid model, errors: {string.Join(", ", errors)}");
                    return Json(new { success = false, errors });
                }
                var existingIngredient = await _ingredients.GetByIdAsync(ingredient.IngredientId, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" });
                if (existingIngredient == null)
                {
                    Console.WriteLine($"Edit POST: Ingredient ID {ingredient.IngredientId} not found");
                    return Json(new { success = false, message = "Ingredient not found." });
                }
                if (existingIngredient.ProductIngredients != null && existingIngredient.ProductIngredients.Any())
                {
                    Console.WriteLine($"Edit POST: Ingredient ID {ingredient.IngredientId} has linked products");
                    return Json(new { success = false, message = "Cannot edit ingredient because it is linked to products." });
                }
                existingIngredient.Name = ingredient.Name;
                await _ingredients.UpdateAsync(existingIngredient);
                Console.WriteLine($"Edit POST: Updated ingredient ID {ingredient.IngredientId}, Name: {ingredient.Name}");
                TempData["Success"] = "Ingredient updated successfully.";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Edit POST error: {ex.Message}");
                return Json(new { success = false, message = $"Error updating ingredient: {ex.Message}" });
            }
        }

        // GET: Ingredient/DeleteCheck
        [HttpGet]
        public async Task<IActionResult> DeleteCheck(int id)
        {
            try
            {
                var ingredient = await _ingredients.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" });
                if (ingredient == null)
                {
                    Console.WriteLine($"DeleteCheck GET: Ingredient ID {id} not found");
                    return Json(new { success = false, message = "Ingredient not found." });
                }
                if (ingredient.ProductIngredients != null && ingredient.ProductIngredients.Any())
                {
                    Console.WriteLine($"DeleteCheck GET: Ingredient ID {id} has linked products");
                    return Json(new { success = false, message = "Cannot delete ingredient because it is linked to products." });
                }
                Console.WriteLine($"DeleteCheck GET: Loaded ingredient ID {id}, Name: {ingredient.Name}");
                return Json(new { success = true, ingredient = new { ingredientId = ingredient.IngredientId, name = ingredient.Name } });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteCheck GET error: {ex.Message}");
                return Json(new { success = false, message = $"Error retrieving ingredient: {ex.Message}" });
            }
        }

        // POST: Ingredient/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([Bind("id")] int id)
        {
            try
            {
                var existingIngredient = await _ingredients.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" });
                if (existingIngredient == null)
                {
                    Console.WriteLine($"Delete POST: Ingredient ID {id} not found");
                    TempData["Error"] = "Ingredient not found.";
                    return Json(new { success = false, message = "Ingredient not found." });
                }
                if (existingIngredient.ProductIngredients != null && existingIngredient.ProductIngredients.Any())
                {
                    Console.WriteLine($"Delete POST: Ingredient ID {id} has linked products");
                    TempData["Error"] = "Cannot delete ingredient because it is linked to products.";
                    return Json(new { success = false, message = "Cannot delete ingredient because it is linked to products." });
                }
                await _ingredients.DeleteAsync(id);
                Console.WriteLine($"Delete POST: Deleted ingredient ID {id}");
                TempData["Success"] = "Ingredient deleted successfully.";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete POST error: {ex.Message}");
                TempData["Error"] = $"Cannot delete ingredient: {ex.Message}";
                return Json(new { success = false, message = $"Error deleting ingredient: {ex.Message}" });
            }
        }
    }
}