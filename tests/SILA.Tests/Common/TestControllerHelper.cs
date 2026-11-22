// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T4.1 | Owner: P4 (QA Lead) | Status: DONE
// What exists: Controller test helper for configuring HttpContext, User Claims, and TempData
// What is left: // TODO(T4.4): Include QA test suite in final project documentation
// Depends on:  ASP.NET Core MVC ControllerContext
// Used by:     AuthorizationNegativeTests and controller-level verification
// How to test: Tested via AuthorizationNegativeTests in SILA.Tests
// Notes:       Attaches simulated ClaimsPrincipal and TempDictionary to controllers
// ═══════════════════════════════════════════════════════

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SILA.Tests.Common;

public static class TestControllerHelper
{
    public static void AttachUserContext(Controller controller, string userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var tempDataProvider = new DummyTempDataProvider();
        controller.TempData = new TempDataDictionary(httpContext, tempDataProvider);
    }

    private class DummyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}

