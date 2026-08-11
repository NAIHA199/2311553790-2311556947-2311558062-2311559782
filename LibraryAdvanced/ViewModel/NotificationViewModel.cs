namespace LibraryAdvanced.ViewModels
{
    public class NotificationViewModel
    {
        public int LoanTicketId { get; set; }

        public string Message { get; set; } = null!;

        public string Type { get; set; } = null!;

        public DateTime? DueDate { get; set; }

        public int Days { get; set; }

        public int TotalQuantity { get; set; }
    }
}