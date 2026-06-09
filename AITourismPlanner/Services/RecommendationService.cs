using AITourismPlanner.Data;
using AITourismPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public interface IRecommendationService
    {
        Task<List<Package>> GetPersonalizedRecommendations(int userId, int topN = 5);
        Task<List<Package>> GetSimilarPackages(int packageId, int topN = 5);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<int, List<double>> _userFactors;
        private readonly Dictionary<int, List<double>> _packageFactors;
        private readonly HashSet<int> _allUsers;
        private readonly HashSet<int> _allPackages;

        public RecommendationService(ApplicationDbContext context)
        {
            _context = context;
            _userFactors = new Dictionary<int, List<double>>();
            _packageFactors = new Dictionary<int, List<double>>();
            _allUsers = new HashSet<int>();
            _allPackages = new HashSet<int>();

            // Load trained model
            LoadModel();
        }

        private void LoadModel()
        {
            try
            {
                var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "recommendation_model.json");

                if (!File.Exists(jsonPath))
                {
                    Console.WriteLine("Model file not found. Using default recommendations.");
                    return;
                }

                var json = File.ReadAllText(jsonPath);
                dynamic model = JsonConvert.DeserializeObject(json);

                // Load user factors
                var userFactorsList = model.user_factors;
                var userToIdx = model.user_to_idx;

                foreach (var kvp in userToIdx)
                {
                    int userId = int.Parse(kvp.Name);
                    int idx = (int)kvp.Value;
                    _userFactors[userId] = ((Newtonsoft.Json.Linq.JArray)userFactorsList[idx]).ToObject<List<double>>();
                    _allUsers.Add(userId);
                }

                // Load package factors
                var packageFactorsList = model.package_factors;
                var idxToPackage = model.idx_to_package;

                foreach (var kvp in idxToPackage)
                {
                    int idx = int.Parse(kvp.Name);
                    int packageId = (int)kvp.Value;
                    _packageFactors[packageId] = ((Newtonsoft.Json.Linq.JArray)packageFactorsList[idx]).ToObject<List<double>>();
                    _allPackages.Add(packageId);
                }

                Console.WriteLine($"Model loaded: {_allUsers.Count} users, {_allPackages.Count} packages");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading model: {ex.Message}");
            }
        }

        public async Task<List<Package>> GetPersonalizedRecommendations(int userId, int topN = 5)
        {
            // 🌟 Fixed: 'userId' parameter now correctly passed to GetDefaultRecommendations
            if (!_userFactors.ContainsKey(userId))
                return await GetDefaultRecommendations(userId);

            var userVector = _userFactors[userId];
            var scores = new Dictionary<int, double>();

            foreach (var pkg in _allPackages)
            {
                if (!_packageFactors.ContainsKey(pkg)) continue;

                var packageVector = _packageFactors[pkg];
                var score = DotProduct(userVector, packageVector);
                scores[pkg] = score;
            }

            // Get packages user hasn't interacted with
            var userBookings = await _context.package_bookings
                .Where(b => b.user_id == userId)
                .Select(b => b.package_id)
                .ToListAsync();

            var recommendedIds = scores
                .Where(s => !userBookings.Contains(s.Key))
                .OrderByDescending(s => s.Value)
                .Take(topN)
                .Select(s => s.Key)
                .ToList();

            return await _context.packages
                .Where(p => recommendedIds.Contains(p.package_id))
                .ToListAsync();
        }

        public async Task<List<Package>> GetSimilarPackages(int packageId, int topN = 5)
        {
            if (!_packageFactors.ContainsKey(packageId))
                return new List<Package>();

            var packageVector = _packageFactors[packageId];
            var scores = new Dictionary<int, double>();

            foreach (var pkg in _allPackages)
            {
                if (pkg == packageId) continue;
                if (!_packageFactors.ContainsKey(pkg)) continue;

                var otherVector = _packageFactors[pkg];
                var score = DotProduct(packageVector, otherVector);
                scores[pkg] = score;
            }

            var recommendedIds = scores
                .OrderByDescending(s => s.Value)
                .Take(topN)
                .Select(s => s.Key)
                .ToList();

            // 🌟 Fixed: Removed duplicate .Where clause here
            return await _context.packages
                .Where(p => recommendedIds.Contains(p.package_id))
                .ToListAsync();
        }

        private async Task<List<Package>> GetDefaultRecommendations(int userId)
        {
            // Strategy 1: Check user preferences
            var preferences = await _context.user_preferences
                .FirstOrDefaultAsync(p => p.user_id == userId);

            if (preferences != null)
            {
                var prefPackages = await _context.packages
    .Where(p => p.is_active &&
                p.price_per_person <= preferences.preferred_budget * 1.2m) // 🌟 Fixed: Added 'm' to make it decimal
    .OrderByDescending(p => p.price_per_person)
    .Take(3)
    .ToListAsync();

                if (prefPackages.Any())
                    return prefPackages;
            }

            // Strategy 2: Most popular packages
            var popularPackages = await GetPopularPackages();
            if (popularPackages.Any())
                return popularPackages;

            // Strategy 3: Seasonal picks (Summer destinations pehle)
            // 🌟 Fixed: Added p.is_active check here as well
            var seasonalPackages = await _context.packages
                .Where(p => p.is_active &&
                            (p.destination_name == "Hunza" ||
                             p.destination_name == "Naran" ||
                             p.destination_name == "Skardu"))
                .Take(5)
                .ToListAsync();

            return seasonalPackages;
        }

        private async Task<List<Package>> GetPopularPackages()
        {
            var popularIds = await _context.package_bookings
                .GroupBy(b => b.package_id)
                .Select(g => new { PackageId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Select(x => x.PackageId)
                .ToListAsync();

            if (popularIds.Any())
            {
                return await _context.packages
                    .Where(p => popularIds.Contains(p.package_id) && p.is_active)
                    .ToListAsync();
            }

            return new List<Package>();
        }

        private double DotProduct(List<double> a, List<double> b)
        {
            double sum = 0;
            for (int i = 0; i < a.Count; i++)
                sum += a[i] * b[i];
            return sum;
        }
    }
}