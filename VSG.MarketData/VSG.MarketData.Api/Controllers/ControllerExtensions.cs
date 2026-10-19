using Microsoft.AspNetCore.Mvc;

namespace VSG.MarketData.Api.Extensions;

public static class ControllerExtensions
{
    public static IActionResult Response(this ControllerBase controller, int statusCode, string? message = null)
    {
        return message is null ? controller.StatusCode(statusCode)
                               : controller.StatusCode(statusCode, message);
    }

    public static IActionResult Response(this ControllerBase controller, int statusCode, object responseBody)
    {
        return controller.StatusCode(statusCode, responseBody);
    }
}