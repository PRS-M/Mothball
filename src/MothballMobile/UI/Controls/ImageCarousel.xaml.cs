using System.Collections;
using Microsoft.Maui.Controls.Shapes;

namespace MothballMobile.UI.Controls;

public partial class ImageCarousel
{
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

	protected override void OnPropertyChanged(string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName is nameof(HeightRequest) or nameof(WidthRequest))
			ApplyToCarousel();
	}

	void ApplyToCarousel()
	{
		Carousel.ItemsSource = ImagePaths;
		Carousel.HeightRequest = HeightRequest;
		Carousel.WidthRequest = WidthRequest;
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
}
