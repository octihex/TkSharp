using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;

namespace TkSharp.DevTools.Views;

public partial class ShellView : AppWindow
{
    public ShellView()
    {
        InitializeComponent();
        
        Bitmap bitmap = new(AssetLoader.Open(new Uri("avares://TkSharp.DevTools/Assets/icon.ico")));
        Icon = bitmap.CreateScaledBitmap(new PixelSize(48, 48));
    }
}