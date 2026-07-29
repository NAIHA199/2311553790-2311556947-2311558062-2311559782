namespace LibraryAdvanced.ViewModels
{
    public class LoanTicketListViewModel
    {
        public int Id { get; set; }
        public string BorrowerName { get; set; } = null!;
        public DateTime? BorrowDate { get; set; }
        public string? Status { get; set; }
        public int TotalQuantity { get; set; }
    }
}