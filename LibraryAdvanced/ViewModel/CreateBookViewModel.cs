using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LibraryAdvanced.ViewModel
{
    public class CreateBookViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sách")]
        [Display(Name = "Tên sách")]
        public string Title { get; set; } = null!;


        [Required(ErrorMessage = "Vui lòng nhập tác giả")]
        [Display(Name = "Tác giả")]
        public string Author { get; set; } = null!;


        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng không hợp lệ")]
        [Display(Name = "Số lượng")]
        public int AvailableQuantity { get; set; }


        [Display(Name = "Ảnh bìa")]
        public IFormFile? ImageFile { get; set; }
    }
}