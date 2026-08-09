using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class CreateLoanTicketViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người mượn")]
        [Display(Name = "Tên người mượn")]
        public string BorrowerName { get; set; } = null!;


        [Required(ErrorMessage = "Vui lòng thêm ít nhất một sách")]
        public List<CreateLoanDetailViewModel> Details { get; set; }
            = new List<CreateLoanDetailViewModel>();
    }
}