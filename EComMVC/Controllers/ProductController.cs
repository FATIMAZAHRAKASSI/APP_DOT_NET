using Azure.Core;
using EComMVC.Data;
using EComMVC.Models;
using EComMVC.Models.BO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace EComMVC.Controllers
{
    public class ProductController : Controller
    {
        [BindProperty]
        public IFormFile ImageFile { get; set; }

        private readonly DBContextConnection _dbContextConnection;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ProductController> _logger;
        private readonly IMemoryCache _memoryCache;

        public ProductController(DBContextConnection dB, IWebHostEnvironment hostingEnvironment, ILogger<ProductController> logger,IMemoryCache memoryCache)
        {
            this._dbContextConnection = dB;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            this._memoryCache=memoryCache;
        }


        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }



        [HttpPost]
        public IActionResult addProduct(AddProductViewModel request)
        {
            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = DateTime.Now.Ticks + Path.GetExtension(ImageFile.FileName);

                    var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images");
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    Directory.CreateDirectory(uploadsFolder);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyToAsync(fileStream);
                    }

                    var prod = new Produit()
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Price = request.Price,
                        Image = "/images/" + fileName
                    };

                    this._dbContextConnection.Add(prod);
                    this._dbContextConnection.SaveChanges();

                    _logger.LogInformation($"Produit {request.Name} ajouté avec succès");

                }
                //rafraichir la chache pour prendre en consideration les informations ajoutées
                RafraichirCache();
                return RedirectToAction("Index2");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'ajout du produit : {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index(string filterName)
        {
            if (User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string userEmail = User.FindFirst(ClaimTypes.Name).Value;

                
                var panier = _dbContextConnection.paniers.Where(p => p.IdUser == userId).ToList();
                int numberProductInPanier = panier.Count;


                if (!_memoryCache.TryGetValue("key", out ProductPanierModel viewModel))
                {


                    var produits = _dbContextConnection.produits.ToList();

                    if (!string.IsNullOrEmpty(filterName))
                    {
                        produits = produits.Where(p => p.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                     viewModel = new ProductPanierModel
                    {
                        Produits = produits,
                        NumberProductInPanier = numberProductInPanier,
                        IdUser = userId
                    };
                    _memoryCache.Set("key", viewModel, TimeSpan.FromMinutes(10));
                    _logger.LogInformation($"les donnes sont lus a partir de la base de donnees");

                }
                else if (!String.IsNullOrEmpty(filterName))
                {
                    var produits = _dbContextConnection.produits.ToList();

                    if (!string.IsNullOrEmpty(filterName))
                    {
                        produits = produits.Where(p => p.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    viewModel = new ProductPanierModel
                    {
                        Produits = produits,
                        NumberProductInPanier = numberProductInPanier,
                        IdUser = userId
                    };
                    _logger.LogInformation($"les donnes sont lus a partir de la base de donnees pour le filtre");

                }
                else
                {
                    viewModel.NumberProductInPanier = numberProductInPanier;
                    _logger.LogInformation($"les donnes sont lus a partir de la cache");
                }
                return View(viewModel);
            }
            else
            {
                return RedirectToAction("FormAuth", "User");
            }
        }





        [HttpGet]
        public async Task<IActionResult> Index2(string filterName)
        {


            if (User.Identity.IsAuthenticated)
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                string userEmail = User.FindFirst(ClaimTypes.Name).Value;


                var panier = _dbContextConnection.paniers.Where(p => p.IdUser == userId).ToList();
                int numberProductInPanier = panier.Count;


                if (!_memoryCache.TryGetValue("keyA", out List<Produit> produits))
                {


                     produits = _dbContextConnection.produits.ToList();

                    if (!string.IsNullOrEmpty(filterName))
                    {
                        produits = produits.Where(p => p.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    
                    _memoryCache.Set("keyA", produits, TimeSpan.FromMinutes(10));
                    _logger.LogInformation($"les donnes sont lus a partir de la base de donnees");

                }
                else if (!String.IsNullOrEmpty(filterName))
                {
                     produits = _dbContextConnection.produits.ToList();

                    if (!string.IsNullOrEmpty(filterName))
                    {
                        produits = produits.Where(p => p.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    
                    _logger.LogInformation($"les donnes sont lus a partir de la base de donnees pour le filtre");

                }
                 else
                {
                    _logger.LogInformation($"les donnes sont lus a partir de la cache");
                }
                return View(produits);

            }
            else
            {
                return RedirectToAction("FormAuth", "User");
            }
        }



        [HttpGet]
        public async Task<IActionResult> updateForm(int id)
        {
            var livre = await _dbContextConnection.produits.FirstOrDefaultAsync(x => x.Id == id);
            if (livre != null)
            {
                var viewproduit = new UpdateProduitModel()
                {
                    Id = livre.Id,
                    //recupere mes autres champs
                    Name = livre.Name,
                    Description = livre.Description,
                    Price = livre.Price,
                };
                return View(viewproduit);
            }
            return RedirectToAction("Index2","Product");
        }


        [HttpPost]
        public async Task<IActionResult> update(UpdateProduitModel updatedlivreviewmodel)
        {
            var produit = await _dbContextConnection.produits.FindAsync(updatedlivreviewmodel.Id);
            if (produit != null)
            {
                produit.Name = updatedlivreviewmodel.Name;
                produit.Description = updatedlivreviewmodel.Description;
                produit.Price = updatedlivreviewmodel.Price;
                await _dbContextConnection.SaveChangesAsync();
                RafraichirCache();
                return RedirectToAction("Index2","Product");
            }
            return Problem("le produit n'existe pas");
        }



        [HttpPost]
        public async Task<IActionResult> delete(UpdateProduitModel livresupp)
        {
            var livre = await _dbContextConnection.produits.FindAsync(livresupp.Id);
            if (livre != null)
            {
                _dbContextConnection.produits.Remove(livre);
                await _dbContextConnection.SaveChangesAsync();
                RafraichirCache();

            }return RedirectToAction("Index2", "Product");

        }


        public IActionResult RafraichirCache()
        {
            // Effacez le cache pour forcer une nouvelle récupération des données lors de la prochaine demande
            _memoryCache.Remove("keyA");
            _memoryCache.Remove("key");
            _logger.LogInformation($"rafraichir les donnees pour un ajout");
            return RedirectToAction("Index");
        }





    }
}



