using System.Collections;
using System.Collections.Specialized;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace MothballMobile.UI.Controls;

public partial class ImageCarousel
{
	private const double DefaultCarouselHeight = 220d;
	private const double FallbackAspectRatio = 16d / 9d;

	private INotifyCollectionChanged? observedCollection;
	private readonly Dictionary<string, double> aspectRatioCache = new(StringComparer.Ordinal);
	private int sizingRequestId;

	public ImageCarousel()
	{
		InitializeComponent();
		ApplyToCarousel();
	}

	public static readonly BindableProperty ImagePathsProperty =
		BindableProperty.Create(
			nameof(ImagePaths),
			typeof(IEnumerable),
			typeof(ImageCarousel),
			default(IEnumerable),
			propertyChanged: static (bindable, _, __) =>
			{
				if (bindable is ImageCarousel control)
					control.ApplyToCarousel();
			});

	public static readonly BindableProperty CornerRadiusProperty =
		BindableProperty.Create(nameof(CornerRadius), typeof(float), typeof(ImageCarousel), 10f);

	public static readonly BindableProperty ShowCounterProperty =
		BindableProperty.Create(
			nameof(ShowCounter),
			typeof(bool),
			typeof(ImageCarousel),
			false,
			propertyChanged: static (bindable, _, __) =>
			{
				if (bindable is ImageCarousel control)
					control.UpdateCounter();
			});

	public static readonly BindableProperty UseDynamicAspectRatioProperty =
		BindableProperty.Create(
			nameof(UseDynamicAspectRatio),
			typeof(bool),
			typeof(ImageCarousel),
			false,
			propertyChanged: static (bindable, _, __) =>
			{
				if (bindable is ImageCarousel control)
					control.UpdateCarouselHeight();
			});

	public IEnumerable? ImagePaths
	{
		get => (IEnumerable?)GetValue(ImagePathsProperty);
		set => SetValue(ImagePathsProperty, value);
	}

	public float CornerRadius
	{
		get => (float)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	public bool ShowCounter
	{
		get => (bool)GetValue(ShowCounterProperty);
		set => SetValue(ShowCounterProperty, value);
	}

	public bool UseDynamicAspectRatio
	{
		get => (bool)GetValue(UseDynamicAspectRatioProperty);
		set => SetValue(UseDynamicAspectRatioProperty, value);
	}

	protected override void OnPropertyChanged(string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName is nameof(HeightRequest) or nameof(WidthRequest))
			UpdateCarouselHeight();
	}

	void ApplyToCarousel()
	{
		DetachObservedCollection();
		Carousel.ItemsSource = ImagePaths;
		AttachObservedCollection();
		UpdateCounter();
		UpdateCarouselHeight();
	}

	void Carousel_OnPositionChanged(object? sender, PositionChangedEventArgs e)
	{
		UpdateCounter();
		UpdateCarouselHeight();
	}

	void AttachObservedCollection()
	{
		if (ImagePaths is INotifyCollectionChanged ncc)
		{
			observedCollection = ncc;
			observedCollection.CollectionChanged += OnImagePathsCollectionChanged;
		}
	}

	void DetachObservedCollection()
	{
		if (observedCollection is null)
			return;

		observedCollection.CollectionChanged -= OnImagePathsCollectionChanged;
		observedCollection = null;
	}

	void OnImagePathsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		UpdateCounter();
		UpdateCarouselHeight();
	}

	void UpdateCounter()
	{
		var total = CountImages(ImagePaths);

		if (!ShowCounter || total <= 1)
		{
			CounterLabel.IsVisible = false;
			CounterLabel.Text = string.Empty;
			return;
		}

		var position = Carousel.Position;
		if (position < 0 || position >= total)
		{
			position = 0;
			Carousel.Position = 0;
		}

		CounterLabel.IsVisible = true;
		CounterLabel.Text = $"{position + 1}/{total}";
	}

	static int CountImages(IEnumerable? source)
	{
		if (source is null)
			return 0;

		var count = 0;
		foreach (var _ in source)
		{
			count++;
		}

		return count;
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		UpdateCarouselHeight();
	}

	void UpdateCarouselHeight()
	{
		if (!UseDynamicAspectRatio)
			return;

		var width = Carousel.Width > 0 ? Carousel.Width : Width;
		if (width <= 0)
		{
			if (Carousel.HeightRequest <= 0)
				Carousel.HeightRequest = DefaultCarouselHeight;

			return;
		}

		var imagePath = GetImagePathAt(Carousel.Position);
		if (string.IsNullOrWhiteSpace(imagePath))
		{
			ApplyCarouselHeight(width / FallbackAspectRatio);
			return;
		}

		if (aspectRatioCache.TryGetValue(imagePath, out var cached))
		{
			ApplyCarouselHeight(width / cached);
			return;
		}

		var reader = GetImageMetadataReader();
		if (reader is null)
		{
			ApplyCarouselHeight(width / FallbackAspectRatio);
			return;
		}

		_ = UpdateCarouselHeightAsync(reader, imagePath, width, ++sizingRequestId);
	}

	async Task UpdateCarouselHeightAsync(IImageMetadataReader reader, string imagePath, double width, int requestId)
	{
		ImageDimensions? dimensions;
		try
		{
			dimensions = await reader.ReadDimensionsAsync(imagePath);
		}
		catch
		{
			dimensions = null;
		}

		var aspectRatio = dimensions?.AspectRatio ?? FallbackAspectRatio;
		aspectRatioCache[imagePath] = aspectRatio;

		if (requestId != sizingRequestId || GetImagePathAt(Carousel.Position) != imagePath)
			return;

		ApplyCarouselHeight(width / aspectRatio);
	}

	void ApplyCarouselHeight(double height)
	{
		if (Math.Abs(Carousel.HeightRequest - height) > 0.5)
			Carousel.HeightRequest = height;
	}

	string? GetImagePathAt(int position)
	{
		if (position < 0 || ImagePaths is null)
			return null;

		var index = 0;
		foreach (var item in ImagePaths)
		{
			if (index == position)
				return item as string;

			index++;
		}

		return null;
	}

	IImageMetadataReader? GetImageMetadataReader()
		=> Handler?.MauiContext?.Services.GetService<IImageMetadataReader>()
			?? Application.Current?.Handler?.MauiContext?.Services.GetService<IImageMetadataReader>();

	void Image_OnSizeChanged(object? sender, EventArgs e)
	{
		if (sender is not Image image)
			return;

		if (image.Width <= 0 || image.Height <= 0)
			return;

		image.Clip = new RoundRectangleGeometry
		{
			CornerRadius = CornerRadius,
			Rect = new Rect(0, 0, image.Width, image.Height)
		};
	}

	protected override void OnHandlerChanging(HandlerChangingEventArgs args)
	{
		if (args.NewHandler is null)
		{
			DetachObservedCollection();
		}

		base.OnHandlerChanging(args);
	}
}
