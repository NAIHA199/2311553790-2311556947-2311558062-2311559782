using System;
using System.Collections.Generic;

namespace LibraryAdvanced.Models;

public partial class LoanTicket
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string BorrowerName { get; set; } = null!;

    public DateTime? BorrowDate { get; set; }

    public string? Status { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<LoanDetail> LoanDetails { get; set; } = new List<LoanDetail>();
}
