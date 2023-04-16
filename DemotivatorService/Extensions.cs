using System.Diagnostics;
using System.Text;
using SkiaSharp;

namespace DemotivatorService;

public static class Extensions
{
	internal const int MaxSize = 512;
	internal const int MinSize = 200;
	internal const int UpperSize = 42;
	internal const int LowerSize = 21;
	internal const float BorderThickness = 4.2f;

	internal static readonly SKFont Arial = new(SKTypeface.FromFamilyName("Arial"), LowerSize);
	internal static readonly SKFont Times = new(SKTypeface.FromFamilyName("Times New Roman"), UpperSize);

	internal static readonly SKPaint UpperPaint = new()
	{
		Color = SKColors.White,
		TextSize = UpperSize,
		TextAlign = SKTextAlign.Center,
		IsAntialias = true
	};
	internal static readonly SKPaint LowerPaint = new()
	{
		Color = SKColors.White,
		TextSize = LowerSize,
		TextAlign = SKTextAlign.Center,
		IsAntialias = true
	};
	internal static readonly SKPaint BorderPaint = new()
	{
		Color = SKColors.White, StrokeWidth = BorderThickness, IsAntialias = true, Style = SKPaintStyle.Stroke
	};
	
	internal static void AddText(this SKCanvas canvas, ref SKSize canvasSize, ref TextData upperTextData, ref TextData lowerTextData, string[]? upperText = null, string[]? lowerText = null)
	{
		if (upperText != null) canvas.DrawText(Times, ref canvasSize, ref upperTextData, upperText);
		if (lowerText != null) canvas.DrawText(Arial, ref canvasSize, ref lowerTextData, lowerText);
	}

	internal static void AddBorder(this SKCanvas canvas, ref SKRect imageRect)
	{
		var borderRect = SKRect.Create(imageRect.Location, imageRect.Size);
		canvas.DrawRect(borderRect, BorderPaint);
	}

	internal static void DrawText(this SKCanvas canvas, SKFont font, ref SKSize canvasSize, ref TextData data, string[] text)
	{
		var y = data.TextY;
		var x = data.TextX;
		var paint = data.Paint;
		var lineHeight = paint.FontSpacing;
		var maxWidth = canvasSize.Width * .87f;

		foreach (var line in text)
		{
			var words = line.Split(' ');
			var currentLine = new StringBuilder();
			foreach (var word in words)
			{
				var current = currentLine.ToString();
				var width = paint.MeasureText(current + ' ' + word);
				if (width > maxWidth)
				{
					canvas.DrawText(current, x, y, paint);
					y += lineHeight;
					currentLine.Clear();
				}

				currentLine.Append(word).Append(' ');
			}

			canvas.DrawText(currentLine.ToString(), x, y, font, paint);
			y += lineHeight;
		}
	}
	
	internal static string[] WrapText(string text, SKPaint paint, float width)
	{
		var lines = new LinkedList<string>();
		if (string.IsNullOrWhiteSpace(text)) return lines.ToArray();

		var words = text.Split(' ');
		var currentLine = new StringBuilder();
		foreach (var word in words)
		{
			var current = currentLine.ToString();
			if (currentLine.Length > 0 && paint.MeasureText(current + ' ' + word) > width)
			{
				lines.AddLast(current);
				currentLine.Clear();
			}

			currentLine.Append(word).Append(' ');
		}

		lines.AddLast(currentLine.ToString());
		return lines.ToArray();
	}
	
	public static SKBitmap ResizeImage(this SKBitmap sourceBitmap, int maxSize, int minSize)
	{
		var (width, height) = (sourceBitmap.Width, sourceBitmap.Height);
		if (width <= maxSize && height <= maxSize) return sourceBitmap;

		if (width > maxSize || height > maxSize)
			(width, height) = Resize(width, height, maxSize);
		else if (width < minSize || height < minSize)
			(width, height) = Resize(width, height, minSize);

		sourceBitmap = sourceBitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High);

		return sourceBitmap;
	}

	public static (int width, int height) Resize(int width, int height, int size)
	{
		var aspectRatio = (float)width / height;

		if (width >= height)
		{
			width = size;
			height = (int)MathF.Round(size / aspectRatio, 0);
		}
		else
		{
			height = size;
			width = (int)MathF.Round(size * aspectRatio, 0);
		}

		return (width, height);
	}

	public static SKRect AddImage(this SKCanvas canvas, SKBitmap sourceBitmap)
	{
		var imageRect = SKRect.Create(sourceBitmap.Width * 0.15f, sourceBitmap.Height * 0.07f, sourceBitmap.Width,
			sourceBitmap.Height);
		canvas.DrawBitmap(sourceBitmap, imageRect);

		return imageRect;
	}

	public static SKSurface CreateBlank(this SKSize canvasSize)
	{
		var surface = SKSurface.Create(new SKImageInfo((int)canvasSize.Width, (int)canvasSize.Height));
		var canvas = surface.Canvas;
		canvas.Clear(SKColors.Black);
		
		return surface;
	}
}