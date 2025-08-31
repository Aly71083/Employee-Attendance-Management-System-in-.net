using ClosedXML.Excel;
using DATA;
using DATA.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace FirstDemoProject.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ================== INDEX ==================
        public IActionResult Index(int? employeeId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.attn.AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            if (fromDate.HasValue)
                query = query.Where(a => a.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Date <= toDate.Value);

            // Use projection with safe navigation
            var model = query
                .Include(a => a.Employee)
                .AsEnumerable() // Switch to LINQ to Objects
                .Select(a => new AttendanceDataViewModel
                {
                    AttendanceId = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee?.Name ?? "",
                    Date = a.Date,
                    Status = a.Status
                })
                .ToList();

            ViewBag.Employees = _context.emp.ToList();

            return View(model);
        }

        // ================== DETAILS ==================
        public IActionResult Details(int? id)
        {
            if (id == null) return NotFound();

            var attendance = _context.attn
                .Include(a => a.Employee)
                .FirstOrDefault(a => a.Id == id);

            if (attendance == null) return NotFound();

            return View(attendance);
        }

        // ================== CREATE ==================
        public IActionResult Create()
        {
            var employees = _context.emp.ToList();
            if (!employees.Any())
            {
                TempData["Error"] = "No employees found. Please add an employee first.";
                return RedirectToAction("Index", "Employee");
            }

            ViewBag.Employees = new SelectList(employees, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Attendance att)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Employees = new SelectList(_context.emp, "Id", "Name", att.EmployeeId);
                return View(att);
            }

            _context.attn.Add(att);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // ================== EDIT ==================
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var attendance = _context.attn.Find(id);
            if (attendance == null) return NotFound();

            ViewBag.Employees = new SelectList(_context.emp, "Id", "Name", attendance.EmployeeId);
            return View(attendance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Attendance model)
        {
            try
            {
                if (id != model.Id) return NotFound();

                
                // Fetch the existing entity from the database
                var attendance = _context.attn.Find(id);
                if (attendance == null) return NotFound();

                // Update only the fields you want
                attendance.Status = model.Status;         // crucial
                attendance.Date = model.Date;             // optional
                attendance.EmployeeId = model.EmployeeId; // optional
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                ViewBag.Employees = new SelectList(_context.emp, "Id", "Name", model.EmployeeId);
               
            }
            

            return RedirectToAction(nameof(Index));
        }


        // ================== DELETE ==================
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var attendance = _context.attn
                .Include(a => a.Employee)
                .FirstOrDefault(a => a.Id == id);

            if (attendance == null) return NotFound();

            return View(attendance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var attendance = _context.attn.Find(id);
            if (attendance != null)
            {
                _context.attn.Remove(attendance);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        // ================== EXPORT TO EXCEL ==================
        public IActionResult ExportToExcel(int? employeeId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.attn.AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            if (fromDate.HasValue)
                query = query.Where(a => a.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Date <= toDate.Value);

            var data = query
                .Include(a => a.Employee)
                .AsEnumerable()
                .Select(a => new
                {
                    Employee = a.Employee?.Name ?? "",
                    Date = a.Date.ToString("yyyy-MM-dd"),
                    Status = a.Status
                })
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Attendance");
                worksheet.Cell(1, 1).Value = "Employee";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Status";

                for (int i = 0; i < data.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = data[i].Employee;
                    worksheet.Cell(i + 2, 2).Value = data[i].Date;
                    worksheet.Cell(i + 2, 3).Value = data[i].Status;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content,
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "Attendance.xlsx");
                }
            }
        }
    }
}
