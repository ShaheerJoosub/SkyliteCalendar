namespace SkyliteCalendar.Models
{
    public class Meeting
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        public string? ExternalAttendees { get; set; }  // comma-separated names

        public ICollection<MeetingAttendee>? Attendees { get; set; }
    }
}
