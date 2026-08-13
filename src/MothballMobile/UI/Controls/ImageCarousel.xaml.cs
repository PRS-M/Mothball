using System.Collections;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Shapes;

namespace MothballMobile.UI.Controls;

public partial class ImageCarousel
{
	private INotifyCollectionChanged? observedCollection;

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

	protected override void OnPropertyChanged(string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName is nameof(HeightRequest) or nameof(WidthRequest))
			ApplyToCarousel();
	}

	void ApplyToCarousel()
	{
		DetachObservedCollection();
		Carousel.ItemsSource = ImagePaths;
		AttachObservedCollection();
		UpdateCounter();
	}

	void Carousel_OnPositionChanged(object? sender, PositionChangedEventArgs e)
	{
		UpdateCounter();
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
