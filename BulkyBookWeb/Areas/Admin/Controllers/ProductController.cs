using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        //GET
        public IActionResult Upsert(int? id)
        {
            var productViewModel = new ProductViewModel
            {
                Product = new Product(),
                CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverTypeRepository.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            if (id == null || id.Value == 0)
            {
                return View(productViewModel);
            }

            productViewModel.Product = _unitOfWork.ProductRepository.GetFirstOrDefault(c => c.Id == id);
            return View(productViewModel);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel productViewModel, IFormFile? file)
        {
            if (!ModelState.IsValid)
                return View(productViewModel);

            var wwwRootPath = _webHostEnvironment.WebRootPath;

            if (file != null)
            {
                var fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, @"images\products");
                var extension = Path.GetExtension(file.FileName);

                if (!string.IsNullOrWhiteSpace(productViewModel.Product.ImageUrl))
                {
                    var oldImagePath = Path.Combine(wwwRootPath, productViewModel.Product.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                using var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create);
                file.CopyTo(fileStreams);

                productViewModel.Product.ImageUrl = @"\images\products\" + fileName + extension;
            }

            if (productViewModel.Product.Id == 0)
            {
                _unitOfWork.ProductRepository.Add(productViewModel.Product);
            }
            else
            {
                _unitOfWork.ProductRepository.Update(productViewModel.Product);
            }

            _unitOfWork.Save();
            TempData["success"] = "Product Created Successfully!";
            return RedirectToAction("Index");

        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitOfWork.ProductRepository.GetAll("Category,CoverType");
            return Json(new { data = productList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var product = _unitOfWork.ProductRepository.GetFirstOrDefault(c => c.Id == id);

            if (product == null)
                return Json(new { success = false, message = "Error while deleting" });

            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.ProductRepository.Remove(product);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
