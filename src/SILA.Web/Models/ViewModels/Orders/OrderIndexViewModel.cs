// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T3.2 | Owner: P1 | Status: DONE
// What exists: ViewModel for listing Material Orders (Buyer purchases or Supplier sales) with status filtering
// What is left: // TODO(T3.4): Wire into unified Sent/Received requests and orders dashboard
// Depends on:  T1.1 (MaterialOrder, MaterialOrderStatus)
// Used by:     OrdersController.Index, Views/Orders/Index.cshtml
// How to test: Visit /Orders as Buyer or Supplier; test status filter tabs and count badges
// Notes:       BR-07: Displays only orders belonging to the user's organization
// ═══════════════════════════════════════════════════════

using SILA.Web.Models.Entities;
using SILA.Web.Models.Enums;

namespace SILA.Web.Models.ViewModels.Orders;

public class OrderIndexViewModel
{
    public IEnumerable<MaterialOrder> Orders { get; set; } = Enumerable.Empty<MaterialOrder>();
    public bool IsSupplier { get; set; }
    public MaterialOrderStatus? CurrentStatus { get; set; }

    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int DispatchedCount { get; set; }
    public int DeliveredCount { get; set; }
    public int RejectedCount { get; set; }
    public int CancelledCount { get; set; }
}
