using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyliteCalendar.Data;
using SkyliteCalendar.Models;

namespace SkyliteCalendar.Controllers
{
    public class MeetingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MeetingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var meetings = _context.Meetings
                .Include(m => m.Department)
                .Include(m => m.CreatedBy)
                .Include(m => m.Attendees).ThenInclude(a => a.User)
                .OrderBy(m => m.StartTime)
                .ToList();
            return View(meetings);
        }

        public IActionResult Create()
        {
            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.Name), "Id", "Name");
            ViewBag.Users = _context.Users.Include(u => u.Department).OrderBy(u => u.FullName).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Meeting meeting, int[]? selectedUsers, int createdByUserId)
        {
            meeting.CreatedByUserId = createdByUserId > 0 ? createdByUserId : 1;

            // Remove navigation property validation errors
            ModelState.Remove("CreatedBy");
            ModelState.Remove("Attendees");
            ModelState.Remove("Department");

            if (ModelState.IsValid)
            {
                _context.Meetings.Add(meeting);
                _context.SaveChanges();

                if (selectedUsers != null)
                {
                    foreach (var userId in selectedUsers)
                        _context.MeetingAttendees.Add(new MeetingAttendee { MeetingId = meeting.Id, UserId = userId });
                    _context.SaveChanges();
                }

                TempData["Success"] = $"Meeting '{meeting.Title}' created.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.Name), "Id", "Name", meeting.DepartmentId);
            ViewBag.Users = _context.Users.Include(u => u.Department).OrderBy(u => u.FullName).ToList();
            return View(meeting);
        }

        public IActionResult Edit(int id)
        {
            var meeting = _context.Meetings
                .Include(m => m.Attendees)
                .FirstOrDefault(m => m.Id == id);
            if (meeting == null) return NotFound();

            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.Name), "Id", "Name", meeting.DepartmentId);
            ViewBag.Users = _context.Users.Include(u => u.Department).OrderBy(u => u.FullName).ToList();
            ViewBag.SelectedUsers = meeting.Attendees?.Select(a => a.UserId).ToList() ?? new List<int>();
            return View(meeting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Meeting meeting, int[]? selectedUsers, int createdByUserId)
        {
            meeting.CreatedByUserId = createdByUserId > 0 ? createdByUserId : 1;
            ModelState.Remove("CreatedBy");
            ModelState.Remove("Attendees");
            ModelState.Remove("Department");

            if (ModelState.IsValid)
            {
                _context.Meetings.Update(meeting);

                var existing = _context.MeetingAttendees.Where(a => a.MeetingId == id).ToList();
                _context.MeetingAttendees.RemoveRange(existing);

                if (selectedUsers != null)
                {
                    foreach (var userId in selectedUsers)
                        _context.MeetingAttendees.Add(new MeetingAttendee { MeetingId = id, UserId = userId });
                }

                _context.SaveChanges();
                TempData["Success"] = "Meeting updated.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.Name), "Id", "Name", meeting.DepartmentId);
            ViewBag.Users = _context.Users.Include(u => u.Department).OrderBy(u => u.FullName).ToList();
            return View(meeting);
        }

        public IActionResult Details(int id)
        {
            var meeting = _context.Meetings
                .Include(m => m.Department)
                .Include(m => m.CreatedBy)
                .Include(m => m.Attendees).ThenInclude(a => a.User).ThenInclude(u => u!.Department)
                .FirstOrDefault(m => m.Id == id);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var meeting = _context.Meetings.Find(id);
            if (meeting != null)
            {
                _context.Meetings.Remove(meeting);
                _context.SaveChanges();
                TempData["Success"] = "Meeting deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // API endpoint for the live display board — returns today's meetings as JSON
        [HttpGet]
        public IActionResult GetMeetingsJson(string? date)
        {
            DateTime target = string.IsNullOrEmpty(date)
                ? DateTime.Today
                : DateTime.Parse(date).Date;

            var meetings = _context.Meetings
                .Include(m => m.Department)
                .Include(m => m.CreatedBy)
                .Include(m => m.Attendees).ThenInclude(a => a.User)
                .Where(m => m.StartTime.Date == target)
                .OrderBy(m => m.StartTime)
                .Select(m => new {
                    m.Id,
                    m.Title,
                    m.Description,
                    StartTime = m.StartTime.ToString("HH:mm"),
                    EndTime   = m.EndTime.ToString("HH:mm"),
                    StartHour = m.StartTime.Hour + m.StartTime.Minute / 60.0,
                    EndHour   = m.EndTime.Hour   + m.EndTime.Minute   / 60.0,
                    Department     = m.Department == null ? "" : m.Department.Name,
                    DepartmentColor = m.Department == null ? "#185FA5" : m.Department.ColorHex,
                    CreatedBy  = m.CreatedBy == null ? "" : m.CreatedBy.FullName,
                    ExternalAttendees = m.ExternalAttendees ?? "",
                    Attendees  = m.Attendees == null ? new List<string>() :
                                 m.Attendees.Where(a => a.User != null)
                                            .Select(a => a.User!.FullName).ToList()
                })
                .ToList();

            return Json(meetings);
        }

        // API endpoint returning all meetings for a week (for week view)
        [HttpGet]
        public IActionResult GetWeekMeetingsJson(string? weekStart)
        {
            DateTime start = string.IsNullOrEmpty(weekStart)
                ? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek)
                : DateTime.Parse(weekStart).Date;
            DateTime end = start.AddDays(7);

            var meetings = _context.Meetings
                .Include(m => m.Department)
                .Include(m => m.CreatedBy)
                .Include(m => m.Attendees).ThenInclude(a => a.User)
                .Where(m => m.StartTime.Date >= start && m.StartTime.Date < end)
                .OrderBy(m => m.StartTime)
                .Select(m => new {
                    m.Id,
                    m.Title,
                    m.Description,
                    Date       = m.StartTime.ToString("yyyy-MM-dd"),
                    DayOfWeek  = (int)m.StartTime.DayOfWeek,
                    StartTime  = m.StartTime.ToString("HH:mm"),
                    EndTime    = m.EndTime.ToString("HH:mm"),
                    StartHour  = m.StartTime.Hour + m.StartTime.Minute / 60.0,
                    EndHour    = m.EndTime.Hour   + m.EndTime.Minute   / 60.0,
                    Department     = m.Department == null ? "" : m.Department.Name,
                    DepartmentColor = m.Department == null ? "#185FA5" : m.Department.ColorHex,
                    CreatedBy  = m.CreatedBy == null ? "" : m.CreatedBy.FullName,
                    ExternalAttendees = m.ExternalAttendees ?? "",
                    Attendees  = m.Attendees == null ? new List<string>() :
                                 m.Attendees.Where(a => a.User != null)
                                            .Select(a => a.User!.FullName).ToList()
                })
                .ToList();

            return Json(meetings);
        }
    }
}
