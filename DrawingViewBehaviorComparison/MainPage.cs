using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
#if IOS
using Foundation;
using UIKit;
#endif

namespace DrawingViewBehaviorComparison
{
    public class MainPage : ContentPage
    {
        private const string DrawViewFileName = "output.png";

        private DrawingView _drawView;

        public MainPage()
        {
            _drawView = CreateDrawingView();
            Content = _drawView;

            ToolbarItems.Add(new ToolbarItem("Save", "save.png", OnSave));
            ToolbarItems.Add(new ToolbarItem("Clear", "trash.png", () => _drawView.Clear()));
        }

        private DrawingView CreateDrawingView() => new DrawingView
        {
            IsMultiLineModeEnabled = true,
            ShouldClearOnFinish = false,
            BackgroundColor = Colors.Gray,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            LineColor = Colors.Red,
        };

        private async void OnSave()
        {
            // Defaulting to half the size of an Android image in 3:4 aspect ratio. Feel free to change and experiment.
            var desiredImageSize = new Size(1512, 2016);

            using var stream = await DrawingViewService.GetImageStream(
                ImageLineOptions.FullCanvas(
                    _drawView.Lines,
                    desiredImageSize,
                    Brush.Gray,
                    new Size(_drawView.Width, _drawView.Height)
                )
            );

#if WINDOWS
            // Place image in user's Documents folder on Windows. 
            var outputFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), DrawViewFileName);
            using var file = File.OpenWrite(outputFileName);
            await stream.CopyToAsync(file);
            file.Close();
#endif
#if ANDROID || IOS
            // Place image in gallery/camera roll on Android/iOS for simpler debugging
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            SavePictureService.SavePicture(ms.ToArray(), DrawViewFileName);
#endif

            await DisplayAlert("Success", "Image has been saved successfully.", "OK");
        }
    }
}

#if ANDROID
public static class SavePictureService
{
    public static bool SavePicture(byte[] arr, string imageName)
    {
        var contentValues = new Android.Content.ContentValues();
        contentValues.Put(Android.Provider.MediaStore.MediaColumns.DisplayName, imageName);
        contentValues.Put(Android.Provider.MediaStore.MediaColumns.MimeType, "image/jpeg");
        contentValues.Put(Android.Provider.MediaStore.MediaColumns.RelativePath, Android.OS.Environment.DirectoryPictures);
        try
        {
            var uri = Platform.CurrentActivity.ContentResolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, contentValues);
            var output = Platform.CurrentActivity.ContentResolver.OpenOutputStream(uri);
            output.Write(arr, 0, arr.Length);
            output.Flush();
            output.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
        return true;
    }
}
#endif
#if IOS
public static class SavePictureService
{
    public static bool SavePicture(byte[] image, string imageName)
    {
        // True if no issue, false if error occurs
        bool status = true;
        var imageData = NSData.FromArray(image);
        var uiImage = UIImage.LoadFromData(imageData);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            uiImage.SaveToPhotosAlbum((img, error) =>
            {
                status = error == null;
            });
        });
        return status;
    }
}
#endif