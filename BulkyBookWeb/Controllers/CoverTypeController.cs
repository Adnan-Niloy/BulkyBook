using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers
{
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var coverTypeList = _unitOfWork.CoverTypeRepository.GetAll();
            return View(coverTypeList);
        }

        //GET
        public IActionResult Create()
        {
            return View();
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType coverType)
        {
            if (!ModelState.IsValid)
                return View(coverType);

            _unitOfWork.CoverTypeRepository.Add(coverType);
            _unitOfWork.Save();
            TempData["success"] = "Cover Type created successfully!";
            return RedirectToAction("Index");
        }

        //GET
        public IActionResult Edit(int? id)
        {
            if (id == null || id.Value == 0)
                return NotFound();

            var coverTypeFromDb = _unitOfWork.CoverTypeRepository.GetFirstOrDefault(c => c.Id == id);

            if (coverTypeFromDb == null)
                return NotFound();

            return View(coverTypeFromDb);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType coverType)
        {
            if (!ModelState.IsValid)
                return View(coverType);

            _unitOfWork.CoverTypeRepository.Update(coverType);
            _unitOfWork.Save();
            TempData["success"] = "Cover Type updated successfully!";
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
