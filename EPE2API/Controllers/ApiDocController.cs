using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using IOFile = System.IO.File;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiDocController : ControllerBase
    {
        private readonly IActionDescriptorCollectionProvider _provider;

        public ApiDocController(IActionDescriptorCollectionProvider provider)
        {
            _provider = provider;
        }
        private static readonly ConcurrentDictionary<string, XDocument> _xmlDocCache = new();

        private static string? GetXmlSummary(MethodInfo? method)
        {
            if (method?.DeclaringType?.Assembly is not { } assembly) return null;

            var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
            if (!System.IO.File.Exists(xmlPath)) return null; // ensure <GenerateDocumentationFile>true</GenerateDocumentationFile> in .csproj

            try
            {
                var doc = _xmlDocCache.GetOrAdd(xmlPath, path => XDocument.Load(path));
                var memberName = GetXmlMemberName(method);
                var summary = doc.Descendants("member")
                    .FirstOrDefault(m => (string?)m.Attribute("name") == memberName)
                    ?.Element("summary")?.Value;

                return string.IsNullOrWhiteSpace(summary)
                    ? null
                    : string.Join(" ", summary.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetXmlMemberName(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var paramTypes = parameters.Length > 0
                ? "(" + string.Join(",", parameters.Select(p => GetTypeName(p.ParameterType))) + ")"
                : "";
            return $"M:{method.DeclaringType!.FullName}.{method.Name}{paramTypes}";
        }

        private static string GetTypeName(Type type)
        {
            if (!type.IsGenericType) return type.FullName ?? type.Name;
            var genericArgs = string.Join(",", type.GetGenericArguments().Select(GetTypeName));
            var name = type.GetGenericTypeDefinition().FullName!.Split('`')[0];
            return $"{name}{{{genericArgs}}}";
        }

        private static string GenerateHeuristicDescription(string actionName, string httpMethod, IEnumerable<string> paramNames)
        {
            var lower = actionName.ToLowerInvariant();
            var verb = httpMethod.ToUpperInvariant();
            var hasIdParam = paramNames.Any(p => p.ToLowerInvariant().Contains("id"));

            string description = lower switch
            {
                _ when lower.Contains("download") || lower.Contains("export") => "Downloads a generated file",
                _ when lower.Contains("upload") || lower.Contains("import") => "Uploads or imports data",
                _ when lower.Contains("search") || lower.Contains("filter") => "Searches or filters records",
                _ when lower.Contains("create") || lower.Contains("add") => "Creates a new record",
                _ when lower.Contains("update") || lower.Contains("edit") => "Updates an existing record",
                _ when lower.Contains("delete") || lower.Contains("remove") => "Deletes a record",
                _ when verb == "GET" && hasIdParam => "Retrieves a specific record by its identifier",
                _ when verb == "GET" => "Retrieves a list of records",
                _ when verb == "POST" => "Creates a new record",
                _ when verb == "PUT" || verb == "PATCH" => "Updates an existing record",
                _ when verb == "DELETE" => "Deletes a record",
                _ => $"Handles the '{actionName}' operation"
            };

            var paramList = paramNames.ToList();
            return paramList.Any()
                ? $"{description} (params: {string.Join(", ", paramList)})"
                : description;
        }


        [HttpGet("download")]
        public IActionResult DownloadExcel()
        {
            var solutionName = Assembly.GetExecutingAssembly().GetName().Name ?? "API Reference";

            var allEndpoints = _provider.ActionDescriptors.Items
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .Select(e =>
                {
                    var method = e.ActionConstraints?
                        .OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>()
                        .FirstOrDefault()?.HttpMethods.FirstOrDefault() ?? "GET";

                    var paramNames = e.Parameters.Select(p => p.Name ?? "").Where(n => n.Length > 0);

                    var description = GetXmlSummary(e.MethodInfo)
                        ?? GenerateHeuristicDescription(e.ActionName, method, paramNames);

                    return new
                    {
                        Controller = e.ControllerName,
                        Action = e.ActionName,
                        Method = method,
                        Route = $"/{e.AttributeRouteInfo?.Template ?? e.ControllerName + "/" + e.ActionName}",
                        Description = description
                    };
                })
                .OrderBy(e => e.Controller)
                .ThenBy(e => e.Route)
                .ToList();

            // Group by controller
            var grouped = allEndpoints
                .GroupBy(e => e.Controller)
                .OrderBy(g => g.Key)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("API Endpoints");

            // ── Palette ───────────────────────────────────────────────────────────────
            var colorDocumentTitle = XLColor.FromHtml("#0F2942");
            var colorSectionBg = XLColor.FromHtml("#1E3A5F");
            var colorSectionFg = XLColor.White;
            var colorColHeaderBg = XLColor.FromHtml("#2E6DA4");
            var colorColHeaderFg = XLColor.White;
            var colorRowEven = XLColor.FromHtml("#FFFFFF");
            var colorRowOdd = XLColor.FromHtml("#F0F4F8");
            var colorText = XLColor.FromHtml("#1C2833");

            var methodColors = new Dictionary<string, (XLColor bg, XLColor fg)>
            {
                ["GET"] = (XLColor.FromHtml("#D5F5E3"), XLColor.FromHtml("#1E8449")),
                ["POST"] = (XLColor.FromHtml("#EBF5FB"), XLColor.FromHtml("#1A5276")),
                ["PUT"] = (XLColor.FromHtml("#FEF9E7"), XLColor.FromHtml("#9A7D0A")),
                ["DELETE"] = (XLColor.FromHtml("#FDEDEC"), XLColor.FromHtml("#922B21")),
                ["PATCH"] = (XLColor.FromHtml("#F4ECF7"), XLColor.FromHtml("#6C3483")),
            };

            string[] colHeaders = { "#", "Method", "Route", "Action", "Description" };
            int[] colWidths = { 5, 12, 40, 22, 50 };
            // Columns: A=#, B=Method, C=Route, D=Action, E=Description  (5 cols total)
            const int TOTAL_COLS = 5;

            // ── Document title row ────────────────────────────────────────────────────
            int currentRow = 1;
            var titleCell = ws.Range(currentRow, 1, currentRow, TOTAL_COLS).Merge();
            titleCell.Value = $"  {solutionName}  |  API Reference  |  {grouped.Count} Controllers  |  {allEndpoints.Count} Endpoints  |  Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 13;
            titleCell.Style.Font.FontColor = XLColor.White;
            titleCell.Style.Fill.BackgroundColor = colorDocumentTitle;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(currentRow).Height = 30;
            currentRow++;

            // ── One section per controller ────────────────────────────────────────────
            foreach (var group in grouped)
            {
                // Spacer between sections (skip before very first section)
                if (currentRow > 2)
                {
                    ws.Row(currentRow).Height = 8;
                    currentRow++;
                }

                // Section header — controller name + count
                var sectionCell = ws.Range(currentRow, 1, currentRow, TOTAL_COLS).Merge();
                sectionCell.Value = $"  {group.Key}Controller   ({group.Count()} endpoint{(group.Count() == 1 ? "" : "s")})";
                sectionCell.Style.Font.Bold = true;
                sectionCell.Style.Font.FontSize = 11;
                sectionCell.Style.Font.FontColor = colorSectionFg;
                sectionCell.Style.Fill.BackgroundColor = colorSectionBg;
                sectionCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(currentRow).Height = 22;
                currentRow++;

                // Column headers for this section
                for (int c = 0; c < colHeaders.Length; c++)
                {
                    var hCell = ws.Cell(currentRow, c + 1);
                    hCell.Value = colHeaders[c];
                    hCell.Style.Font.Bold = true;
                    hCell.Style.Font.FontSize = 9;
                    hCell.Style.Font.FontColor = colorColHeaderFg;
                    hCell.Style.Fill.BackgroundColor = colorColHeaderBg;
                    hCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    hCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                ws.Row(currentRow).Height = 18;
                currentRow++;

                // Data rows
                int rowIndex = 0;
                foreach (var e in group)
                {
                    var rowBg = rowIndex % 2 == 0 ? colorRowEven : colorRowOdd;
                    var (methodBg, methodFg) = methodColors.GetValueOrDefault(e.Method,
                        (XLColor.FromHtml("#D6E4F0"), XLColor.FromHtml("#1E3A5F")));

                    object[] values = { rowIndex + 1, e.Method, e.Route, e.Action, e.Description };

                    for (int c = 0; c < values.Length; c++)
                    {
                        var cell = ws.Cell(currentRow, c + 1);
                        cell.Value = values[c] is int i ? (XLCellValue)i : (XLCellValue)(string)values[c]!;

                        bool isMethodCol = c == 1;
                        cell.Style.Fill.BackgroundColor = isMethodCol ? methodBg : rowBg;
                        cell.Style.Font.FontColor = isMethodCol ? methodFg : colorText;
                        cell.Style.Font.Bold = isMethodCol;
                        cell.Style.Font.FontSize = 9;
                        cell.Style.Alignment.Horizontal = c <= 1
                            ? XLAlignmentHorizontalValues.Center
                            : XLAlignmentHorizontalValues.Left;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Alignment.WrapText = c >= 2;
                    }

                    ws.Row(currentRow).Height = 22;
                    currentRow++;
                    rowIndex++;
                }
            }

            // ── Column widths ─────────────────────────────────────────────────────────
            for (int c = 0; c < colWidths.Length; c++)
                ws.Column(c + 1).Width = colWidths[c];

            // ── Thin border around each used cell ────────────────────────────────────
            var usedRange = ws.RangeUsed();
            if (usedRange != null)
            {
                usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#B0BEC5");
                usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#CFD8DC");
            }

            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"API_Reference_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }
    }
}
