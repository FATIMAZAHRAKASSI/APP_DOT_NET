﻿using EComMVC.Data;
using EComMVC.Models;
using EComMVC.Models.BO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EComMVC.Controllers
{
    public class PanierController : Controller
    {
        private readonly DBContextConnection _dbContextConnection;
        private readonly ILogger<PanierController> _logger;
        private readonly IMemoryCache _memoryCache;


        public PanierController(DBContextConnection dB, ILogger<PanierController> logger,IMemoryCache memoryCache)

        {
            this._dbContextConnection = dB;
            this._logger = logger;
            this._memoryCache = memoryCache;
        }

        [HttpPost]
        public IActionResult AjouterAuPanier(int idProduit, int iduser)
        {
            try
            {
                var existeDeja = _dbContextConnection.paniers
                    .Any(p => p.IdUser == iduser && p.IdProduct == idProduit);

                if (existeDeja)
                {

                    _logger.LogWarning($"Le produit {idProduit} existe déjà dans le panier de l'utilisateur {iduser}");
                }
                else
                {
                    var nouveauPanier = new Panier
                    {
                        IdProduct = idProduit,
                        IdUser = iduser
                    };

                    _dbContextConnection.paniers.Add(nouveauPanier);
                    _dbContextConnection.SaveChanges();
                    _logger.LogInformation($"Produit {idProduit} ajouté au panier de l'utilisateur {iduser}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'ajout au panier : {ex.Message}");
            }
            return RedirectToAction("Index", "Product");
        }


        [HttpGet]
        public IActionResult Index(int iduser)
        {
            try
            {

                    var panier = _dbContextConnection.paniers.Where(p => p.IdUser == iduser).ToList();

                    var productIDs = panier.Select(p => p.IdProduct).ToList();

                ProductPanierModel produits = new ProductPanierModel();
                produits.Produits = new List<Produit>();

                    foreach (int idp in productIDs)
                    {
                        var produit = _dbContextConnection.produits.FirstOrDefault(p => p.Id == idp);
                        if (produit != null)
                        {
                            produits.Produits.Add(produit);
                        }
                    }
                    produits.IdUser = iduser;
                  

                _logger.LogInformation($"Affichage du panier de l'utilisateur {iduser}");

                return View(produits);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'affichage du panier : {ex.Message}");
                return RedirectToAction("Index", "Product");
            }
        }


        [HttpPost]
        public async Task<IActionResult> delete(int idProduit,int iduser)
        {
            var panierExist = _dbContextConnection.paniers.FirstOrDefault(p => p.IdUser == iduser && p.IdProduct == idProduit);
            if (panierExist != null)
            {
                _dbContextConnection.paniers.Remove(panierExist);
                await _dbContextConnection.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Product");
        }

        
        public async Task<IActionResult> delete2(int idProduit)
        {
            var panierExist = _dbContextConnection.paniers.FirstOrDefault(p => p.IdProduct == idProduit);
            if (panierExist != null)
            {
                _dbContextConnection.paniers.Remove(panierExist);
                await _dbContextConnection.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Product");
        }

    }
}
