using System;
using System.Collections.Generic;

namespace Q2.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string City { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
