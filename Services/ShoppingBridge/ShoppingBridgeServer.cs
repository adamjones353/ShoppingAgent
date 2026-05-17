using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ShoppingAgent.Contracts;
using ShoppingAgent.Repositories;
using ShoppingAgent.Services;

namespace ShoppingAgent.Services.ShoppingBridge;

public sealed class ShoppingBridgeServer(
    IShoppingBridgeState bridgeState,
    IShoppingListService shoppingLists,
    IProductMappingRepository productMappings) : BackgroundService
{
    public const string BaseUrl = "http://127.0.0.1:51234/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(BaseUrl);
        try
        {
            listener.Start();
        }
        catch
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleAsync(context), stoppingToken);
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        AddCors(context.Response);
        if (context.Request.HttpMethod == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            context.Response.Close();
            return;
        }

        try
        {
            var path = context.Request.Url?.AbsolutePath.Trim('/').ToLowerInvariant() ?? "";
            switch (path)
            {
                case "health":
                    await WriteJsonAsync(context, new { ok = true, app = "ShoppingAgent" });
                    return;
                case "current-item":
                    await WriteJsonAsync(context, new { item = bridgeState.GetCurrentItem() });
                    return;
                case "next-item":
                    await WriteJsonAsync(context, new { item = bridgeState.MoveNext() });
                    return;
                case "item-added":
                    await HandleItemAddedAsync(context);
                    return;
                default:
                    context.Response.StatusCode = 404;
                    await WriteJsonAsync(context, new { error = "Unknown endpoint" });
                    return;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await WriteJsonAsync(context, new { error = ex.Message });
        }
    }

    private async Task HandleItemAddedAsync(HttpListenerContext context)
    {
        var current = bridgeState.GetCurrentItem();
        if (current is null)
        {
            context.Response.StatusCode = 400;
            await WriteJsonAsync(context, new { error = "No current item" });
            return;
        }

        var body = await ReadBodyAsync(context.Request);
        var request = JsonSerializer.Deserialize<ItemAddedRequest>(body, JsonOptions) ?? new ItemAddedRequest();

        await shoppingLists.PatchItemAsync(current.Id, true, null);
        if (request.SaveAsPreferred && current.IngredientId is not null && !string.IsNullOrWhiteSpace(request.ProductUrl))
        {
            await productMappings.SavePreferredMappingAsync(new ProductMappingRequest(
                current.IngredientId.Value,
                "Tesco",
                string.IsNullOrWhiteSpace(request.ProductName) ? current.Name : request.ProductName,
                current.SearchTerm,
                request.ProductUrl,
                "",
                "Saved from Chrome extension"));
        }

        await WriteJsonAsync(context, new { ok = true, item = bridgeState.MoveNext() });
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        return await reader.ReadToEndAsync();
    }

    private static async Task WriteJsonAsync(HttpListenerContext context, object value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        context.Response.ContentType = "application/json";
        context.Response.ContentEncoding = Encoding.UTF8;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    private static void AddCors(HttpListenerResponse response)
    {
        response.Headers["Access-Control-Allow-Origin"] = "*";
        response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
    }

    private sealed class ItemAddedRequest
    {
        public string ProductName { get; set; } = "";
        public string ProductUrl { get; set; } = "";
        public bool SaveAsPreferred { get; set; }
    }
}
