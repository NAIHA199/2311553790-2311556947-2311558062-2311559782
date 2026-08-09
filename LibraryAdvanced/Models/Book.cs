using System;
using System.Collections.Generic;

namespace LibraryAdvanced.Models;

public partial class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Author { get; set; } = null!;

    public int AvailableQuantity { get; set; }

    public int CategoryId { get; set; }

    public string? ImagePath { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<LoanDetail> LoanDetails { get; set; } = new List<LoanDetail>();
}
