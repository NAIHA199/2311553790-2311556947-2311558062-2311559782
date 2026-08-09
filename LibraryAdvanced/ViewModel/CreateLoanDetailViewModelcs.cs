using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class CreateLoanDetailViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sách")]
        [Display(Name = "Chọn sách")]
        public int BookId { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Số lượng phải lớn hơn 0"
        )]
        [Display(Name = "Số lượng mượn")]
        public int Quantity { get; set; } = 1;
    }
}