namespace LibraryAdvanced.ViewModels
{
    public class LoanTicketListViewModel
    {
        public int Id { get; set; }

        public string BorrowerName { get; set; } = null!;

        // Ngày mượn
        public DateTime? BorrowDate { get; set; }

        // Hạn trả
        public DateTime? DueDate { get; set; }

        // Ngày thực tế trả
        public DateTime? ReturnedDate { get; set; }

        public string? Status { get; set; }

        public int TotalQuantity { get; set; }

        public List<LoanDetailItemViewModel> LoanDetails { get; set; }
            = new();
    }


    public class LoanDetailItemViewModel
    {
        public int BookId { get; set; }

        public string BookTitle { get; set; } = null!;

        public int Quantity { get; set; }
    }
}