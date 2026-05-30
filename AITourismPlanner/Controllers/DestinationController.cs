using Microsoft.AspNetCore.Mvc;
using AITourismPlanner.Models;
using AITourismPlanner.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AITourismPlanner.Controllers
{
    public class DestinationController : Controller
    {
        private readonly IDestinationService _destinationService;

        public DestinationController(IDestinationService destinationService)
        {
            _destinationService = destinationService;
        }

        public async Task<IActionResult> Index(string category = null)
        {
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = new[] { "All", "Mountains", "Valleys", "Historical", "Urban", "Beach", "Adventure" };

            List<DestinationApiModel> destinations;

            if (string.IsNullOrEmpty(category) || category == "All")
            {
                destinations = await _destinationService.GetAllDestinationsAsync();
            }
            else
            {
                destinations = await _destinationService.GetDestinationsByCategoryAsync(category);
            }

            return View(destinations);
        }

        public async Task<IActionResult> Details(string name)
        {
            if (string.IsNullOrEmpty(name))
                return RedirectToAction("Index");

            var destination = await _destinationService.GetDestinationDetailAsync(name);

            if (destination == null)
                return RedirectToAction("Index");

            return View(destination);
        }

        public async Task<IActionResult> Search(string q)
        {
            var destinations = await _destinationService.SearchDestinationsAsync(q);
            ViewBag.SearchTerm = q;
            return View(destinations);
        }

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string term)
        {
            var suggestions = await _destinationService.GetAutocompleteSuggestionsAsync(term);
            return Json(suggestions);
        }
    }
}