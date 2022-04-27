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
            else
            {

            }


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

                using var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create);
                file.CopyTo(fileStreams);

                productViewModel.Product.ImageUrl = @"\images\products\" + fileName + extension;
            }

            _unitOfWork.ProductRepository.Add(productViewModel.Product);
            _unitOfWork.Save();
            TempData["success"] = "Product Created Successfully!";
            return RedirectToAction("Index");

        }

        //GET
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null || id.Value == 0)
                return NotFound();

            var coverTypeFromDb = _unitOfWork.CoverTypeRepository.GetFirstOrDefault(c => c.Id == id);

            if (coverTypeFromDb == null)
                return NotFound();

            return View(coverTypeFromDb);
        }

        //POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var coverType = _unitOfWork.CoverTypeRepository.GetFirstOrDefault(c => c.Id == id);

            if (coverType == null)
                return NotFound();

            _unitOfWork.CoverTypeRepository.Remove(coverType);
            _unitOfWork.Save();
            TempData["success"] = "Cover Type deleted successfully!";
            return RedirectToAction("Index");

        }
    }
}
