using SkiaSharp;

namespace DemotivatorService;

public static class Extensions
{
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