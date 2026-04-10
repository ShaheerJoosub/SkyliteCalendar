namespace SkyliteCalendar.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#185FA5"; // display colour for calendar

        public ICollection<User>? Users { get; set; }
        public ICollection<Meeting>? Meetings { get; set; }
    }
}
