namespace LibraryAdvanced.ViewModels
{
    public class LoanTicketListViewModel
    {
        public int Id { get; set; }
        public string BorrowerName { get; set; } = null!;
        public DateTime? BorrowDate { get; set; }
        public string? Status { get; set; }
        public int TotalQuantity { get; set; }

        public List<LoanDetailItemViewModel> LoanDetails { get; set; } = new();
    }

    public class LoanDetailItemViewModel
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = null!;
        public int Quantity { get; set; }
    }
}