using System;
using System.Collections.Generic;

namespace LibraryAdvanced.Models;

public partial class LoanDetail
{
    public int LoanTicketId { get; set; }

    public int BookId { get; set; }

    public int Quantity { get; set; }

    // Số lượng sách đã trả
    public int ReturnedQuantity { get; set; } = 0;

    public virtual Book Book { get; set; } = null!;

    public virtual LoanTicket LoanTicket { get; set; } = null!;
}