// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.2 | Owner: P1 | Status: DONE
// What exists: ViewModels for Material Order details, snapshots, status audit trail, and actions
// What is left: // TODO(T3.4): Integrate into unified status history timeline component
// Depends on:  T1.1 (MaterialOrder, MaterialOrderLine, StatusHistory), T1.2 (IStatusHistoryService)
// Used by:     OrdersController.Details, Views/Orders/Details.cshtml
// How to test: Navigate to /Orders/Details/{id} as Buyer or Supplier; verify appropriate action buttons and history
// Notes:       Enforces state machine transitions (Part 4.4) and record-level authorization (BR-07)
// ═══════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.Orders;

public class OrderDetailsViewModel
{
    public MaterialOrder Order { get; set; } = null!;
    public IEnumerable<StatusHistory> History { get; set; } = Enumerable.Empty<StatusHistory>();

    public bool IsBuyer { get; set; }
    public bool IsSupplier { get; set; }

    public bool CanCancel => IsBuyer && Order.Status == MaterialOrderStatus.PendingReview;
    public bool CanConfirm => IsSupplier && Order.Status == MaterialOrderStatus.PendingReview;
    public bool CanReject => IsSupplier && Order.Status == MaterialOrderStatus.PendingReview;
    public bool CanDispatch => IsSupplier && Order.Status == MaterialOrderStatus.Confirmed;
    public bool CanDeliver => IsSupplier && Order.Status == MaterialOrderStatus.Dispatched;

    public OrderRejectViewModel RejectForm { get; set; } = new();
}

public class OrderRejectViewModel
{
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Rejection reason is required (Part 4.4).")]
    [MaxLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters.")]
    [Display(Name = "Reason for Rejection")]
    public string Reason { get; set; } = string.Empty;
}
