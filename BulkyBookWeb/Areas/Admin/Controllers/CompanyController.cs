using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        //GET
        public IActionResult Upsert(int? id)
        {
            var company = new Company();

            if (id == null || id.Value == 0)
            {
                return View(company);
            }

            company = _unitOfWork.CompanyRepository.GetFirstOrDefault(c => c.Id == id);
            return View(company);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpSert(Company company)
        {
            if (!ModelState.IsValid)
                return View(company);

            if (company.Id == 0)
            {
                _unitOfWork.CompanyRepository.Add(company);
            }
            else
            {
                _unitOfWork.CompanyRepository.Update(company);
            }

            _unitOfWork.Save();
            TempData["success"] = "Company Created Successfully!";
            return RedirectToAction("Index");
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _unitOfWork.CompanyRepository.GetAll();
            return Json(new { data = companyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var company = _unitOfWork.CompanyRepository.GetFirstOrDefault(c => c.Id == id);

            if (company == null)
                return Json(new { success = false, message = "Error while deleting" });

            _unitOfWork.CompanyRepository.Remove(company);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
