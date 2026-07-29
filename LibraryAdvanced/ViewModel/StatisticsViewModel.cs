namespace LibraryAdvanced.ViewModel
{
    public class StatisticsViewModel
    {
        public int TotalActiveBorrowedBooks { get; set; }
        public List<TopBookViewModel> Top3Books { get; set; } = new List<TopBookViewModel>();
    }
}
