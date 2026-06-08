using System.Globalization;
using FluentPdf.Domain.Content;
using SkiaSharp;

namespace FluentPdf.Adapters.Shared;

/// <summary>
/// Renders an agnostic <see cref="ChartBlock"/> to a PNG with SkiaSharp. Both adapters embed
/// the resulting image at the chart's intrinsic size, so a bar/line/pie chart looks
/// <em>identical</em> in the QuestPDF and iText output — the chart is drawn once, here.
/// </summary>
public static class SkiaChartRenderer
{
    private const float Scale = 2f; // supersample for crisp output when scaled to PDF points.

    private static readonly SKTypeface Regular = SKTypeface.FromData(SKData.CreateCopy(EmbeddedFonts.Regular));
    private static readonly SKTypeface Bold = SKTypeface.FromData(SKData.CreateCopy(EmbeddedFonts.Bold));

    /// <summary>Renders the chart to PNG bytes sized <c>Width*Scale × Height*Scale</c> pixels.</summary>
    public static byte[] RenderPng(ChartBlock chart)
    {
        var width = (int)Math.Round(chart.Width * Scale);
        var height = (int)Math.Round(chart.Height * Scale);

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var plot = DrawChrome(canvas, chart, width, height, out var titleHeight, out var legendHeight);

        switch (chart.Type)
        {
            case ChartType.Pie:
                DrawPie(canvas, chart, plot);
                break;
            case ChartType.Line:
                DrawAxes(canvas, chart, plot, out var lineScale);
                DrawLines(canvas, chart, plot, lineScale);
                break;
            default:
                DrawAxes(canvas, chart, plot, out var barScale);
                DrawBars(canvas, chart, plot, barScale);
                break;
        }

        DrawLegend(canvas, chart, width, height, legendHeight, titleHeight);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKRect DrawChrome(
        SKCanvas canvas,
        ChartBlock chart,
        int width,
        int height,
        out float titleHeight,
        out float legendHeight)
    {
        titleHeight = 0f;

        if (!string.IsNullOrEmpty(chart.Title))
        {
            using var titlePaint = TextPaint(Bold, 13f * Scale, "111111");
            titleHeight = 24f * Scale;
            DrawText(canvas, chart.Title!, width / 2f, 17f * Scale, titlePaint, SKTextAlign.Center);
        }

        legendHeight = 22f * Scale;

        var left = (chart.Type == ChartType.Pie ? 12f : 52f) * Scale;
        var right = (chart.Type == ChartType.Pie ? width / 2f : 14f * Scale);
        var top = titleHeight + (8f * Scale);
        var bottom = height - legendHeight - (chart.Type == ChartType.Pie ? 0f : 18f * Scale);

        return new SKRect(left, top, width - right, bottom);
    }

    private static void DrawAxes(SKCanvas canvas, ChartBlock chart, SKRect plot, out ValueScale scale)
    {
        var (min, max) = Range(chart);
        scale = new ValueScale(min, max, plot);

        using var axis = new SKPaint { Color = Hex("999999"), StrokeWidth = 1f * Scale, IsAntialias = true };
        using var grid = new SKPaint { Color = Hex("EEEEEE"), StrokeWidth = 1f * Scale, IsAntialias = true };
        using var label = TextPaint(Regular, 9f * Scale, "666666");

        const int ticks = 4;
        for (var i = 0; i <= ticks; i++)
        {
            var value = min + ((max - min) * i / ticks);
            var y = scale.Y(value);
            canvas.DrawLine(plot.Left, y, plot.Right, y, grid);
            DrawText(canvas, Format(value), plot.Left - (6f * Scale), y + (3f * Scale), label, SKTextAlign.Right);
        }

        canvas.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, axis);
        canvas.DrawLine(plot.Left, scale.Y(0), plot.Right, scale.Y(0), axis);

        using var category = TextPaint(Regular, 9f * Scale, "444444");
        var slot = plot.Width / chart.Categories.Count;
        for (var i = 0; i < chart.Categories.Count; i++)
        {
            var x = plot.Left + (slot * (i + 0.5f));
            DrawText(canvas, chart.Categories[i], x, plot.Bottom + (13f * Scale), category, SKTextAlign.Center);
        }
    }

    private static void DrawBars(SKCanvas canvas, ChartBlock chart, SKRect plot, ValueScale scale)
    {
        var slot = plot.Width / chart.Categories.Count;
        var groupWidth = slot * 0.72f;
        var barWidth = groupWidth / chart.Series.Count;
        var baseline = scale.Y(0);

        for (var s = 0; s < chart.Series.Count; s++)
        {
            using var paint = new SKPaint { Color = SeriesColor(s), IsAntialias = true };
            var series = chart.Series[s];

            for (var c = 0; c < series.Values.Count; c++)
            {
                var x = plot.Left + (slot * c) + ((slot - groupWidth) / 2f) + (barWidth * s);
                var y = scale.Y(series.Values[c]);
                canvas.DrawRect(x, Math.Min(y, baseline), barWidth - (2f * Scale), Math.Abs(baseline - y), paint);
            }
        }
    }

    private static void DrawLines(SKCanvas canvas, ChartBlock chart, SKRect plot, ValueScale scale)
    {
        var slot = plot.Width / chart.Categories.Count;

        for (var s = 0; s < chart.Series.Count; s++)
        {
            var series = chart.Series[s];
            using var line = new SKPaint
            {
                Color = SeriesColor(s),
                StrokeWidth = 2f * Scale,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
            };
            using var dot = new SKPaint { Color = SeriesColor(s), IsAntialias = true };
            using var path = new SKPath();

            for (var c = 0; c < series.Values.Count; c++)
            {
                var x = plot.Left + (slot * (c + 0.5f));
                var y = scale.Y(series.Values[c]);

                if (c == 0)
                {
                    path.MoveTo(x, y);
                }
                else
                {
                    path.LineTo(x, y);
                }

                canvas.DrawCircle(x, y, 3f * Scale, dot);
            }

            canvas.DrawPath(path, line);
        }
    }

    private static void DrawPie(SKCanvas canvas, ChartBlock chart, SKRect plot)
    {
        var values = chart.Series[0].Values;
        var total = values.Sum();
        if (total <= 0d)
        {
            return;
        }

        var size = Math.Min(plot.Width, plot.Height);
        var rect = new SKRect(
            plot.Left,
            plot.MidY - (size / 2f),
            plot.Left + size,
            plot.MidY + (size / 2f));

        var start = -90f;
        for (var i = 0; i < values.Count; i++)
        {
            var sweep = (float)(values[i] / total * 360d);
            using var paint = new SKPaint { Color = SeriesColor(i), IsAntialias = true };
            using var path = new SKPath();
            path.MoveTo(rect.MidX, rect.MidY);
            path.ArcTo(rect, start, sweep, false);
            path.Close();
            canvas.DrawPath(path, paint);
            start += sweep;
        }
    }

    private static void DrawLegend(
        SKCanvas canvas,
        ChartBlock chart,
        int width,
        int height,
        float legendHeight,
        float titleHeight)
    {
        var pie = chart.Type == ChartType.Pie;
        var labels = pie ? chart.Categories : [.. chart.Series.Select(series => series.Name)];

        using var paint = TextPaint(Regular, 9.5f * Scale, "333333");
        var swatch = 9f * Scale;
        var gap = 6f * Scale;

        if (pie)
        {
            var x = (width / 2f) + (10f * Scale);
            var y = titleHeight + (20f * Scale);
            for (var i = 0; i < labels.Count; i++)
            {
                using var box = new SKPaint { Color = SeriesColor(i), IsAntialias = true };
                canvas.DrawRect(x, y - swatch + (2f * Scale), swatch, swatch, box);
                DrawText(canvas, labels[i], x + swatch + gap, y, paint, SKTextAlign.Left);
                y += 18f * Scale;
            }

            return;
        }

        var widths = labels.Select(l => paint.MeasureText(l) + swatch + gap + (16f * Scale)).ToArray();
        var totalWidth = widths.Sum();
        var cursor = (width - totalWidth) / 2f;
        var baseY = height - (7f * Scale);

        for (var i = 0; i < labels.Count; i++)
        {
            using var box = new SKPaint { Color = SeriesColor(i), IsAntialias = true };
            canvas.DrawRect(cursor, baseY - swatch, swatch, swatch, box);
            DrawText(canvas, labels[i], cursor + swatch + gap, baseY, paint, SKTextAlign.Left);
            cursor += widths[i];
        }
    }

    private static (double Min, double Max) Range(ChartBlock chart)
    {
        var max = double.MinValue;
        var min = double.MaxValue;

        foreach (var series in chart.Series)
        {
            foreach (var value in series.Values)
            {
                max = Math.Max(max, value);
                min = Math.Min(min, value);
            }
        }

        max = Math.Max(max, 0d);
        min = Math.Min(min, 0d);

        if (Math.Abs(max - min) < double.Epsilon)
        {
            max = min + 1d;
        }

        return (min, max);
    }

    private static SKColor SeriesColor(int index) =>
        Hex(RenderingTheme.Palette[index % RenderingTheme.Palette.Length]);

    private static SKColor Hex(string hex) => SKColor.Parse("#" + hex);

    private static SKPaint TextPaint(SKTypeface typeface, float size, string hex) => new()
    {
        Color = Hex(hex),
        TextSize = size,
        Typeface = typeface,
        IsAntialias = true,
    };

    private static void DrawText(SKCanvas canvas, string text, float x, float y, SKPaint paint, SKTextAlign align)
    {
        paint.TextAlign = align;
        canvas.DrawText(text, x, y, paint);
    }

    private static string Format(double value) =>
        Math.Abs(value) >= 1000d
            ? value.ToString("#,##0", CultureInfo.InvariantCulture)
            : value.ToString("0.#", CultureInfo.InvariantCulture);

    private sealed class ValueScale(double min, double max, SKRect plot)
    {
        public float Y(double value) =>
            (float)(plot.Bottom - ((value - min) / (max - min) * plot.Height));
    }
}
