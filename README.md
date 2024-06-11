# Azure Maps WPF Control

On May 29th, 22024 Microsoft announced that the [Bing Maps for Enterprise platform would be retiring](https://www.microsoft.com/en-us/maps/bing-maps/discontinued-services), and recommended moving to Azure Maps. 

The Bing Maps WPF SDK was actually deprecated many years ago, however, since it used the same production services of Bing Maps as their Web SDK, it continued to work. The original download page for the SDK is long gone (links in the docs are dead), but their is a NuGet package still around that was last updated in 2015. Because of this, there are still some apps out there using the Bing Maps WPF SDK. 

The goal of this project is to provide an easy way to port over an app using the Bing Maps WPF SDK to use Azure Maps instead. In all, this solution can have your app using Azure Maps services instead of Bing Maps services in as few as 5 easy steps.

## Important Note

This is a hack! It does several undocumented things with the Bing Maps WPF control. Given that the Bing Maps WPF control was deprecated years ago, and not recieved any updates since then, that means it hasn't had any security or stability improvements made either. Given that the NuGet package could be removed at any time without warning, consider this a temporary solution. 

Additionally, this solution has hardcoded URLs to specific versions of the Azure Maps services. Versions may increase over time, and the code may need to be updated accordingly to keep working. Azure Maps will likely send an email notice if you are using an older version of a service that needs to be updated. This solutions uses the `Map tile` and `Map Attribution` services of the [Azure Maps REST Render services](https://learn.microsoft.com/en-us/rest/api/maps/render).

Current Render service version used by this solution: `2022-08-01`

## Supported features

- **Localization support** - Most of the culture codes by Bing Maps work directly with Azure Maps. This solution is passes the culture code as is to Azure Maps and relies on it to determine how to fallback. As Azure Maps adds support for additional culture codes, this solution will automatically improve with it.
- **Copyright support** - To align with the terms of service, this solution dynamically updates the copyrights displayed in the bottom right corner of the map as required. 
- **Bing logo removed** - Azure Maps terms of use do not require a logo to be displayed, and there is no need for a Bing logo when using Azure Maps data, so this solution removes it help keep the map a bit cleaner.
- **Support for multiple maps instances** - Load two or more maps, with different cultures without any issues. 

## How to use this in your app

1. Get an [Azure Maps subscription key](https://learn.microsoft.com/en-us/azure/azure-maps/how-to-manage-authentication). 
2. Go into the `sample\BingMapsWPF_AzureMapsTiles\BingMapsWPF_AzureMapsTiles` folder of this project and copy and paste the `AzureMapsControl.cs` file into your app. [Here is a direct link to the file](https://github.com/rbrundritt/AzureMapsWPFControl/blob/main/sample/BingMapsWPF_AzureMapsTiles/BingMapsWPF_AzureMapsTiles/AzureMapsControl.cs).
3. In the XAML file where you add the map, add this namespace `xmlns:azmaps="clr-namespace:Azure.Maps.WPF"`
4. Locate the XAML for the map control in your app, it looks something like `<m:Map`, and replace this part of the XAML with `<azmaps:AzureMapsControl`. Note that the namespace name (`m`) in your app may be different.
5. Update the Bing Maps key used in the `CredentialsProvider` with your Azure Maps key.

Here is a simple example of how it should look:

```XAML
<Window x:Class="BingMapsWPF_AzureMapsTiles.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:m="clr-namespace:Microsoft.Maps.MapControl.WPF;assembly=Microsoft.Maps.MapControl.WPF"
        xmlns:azmaps="clr-namespace:Azure.Maps.WPF"
        xmlns:local="clr-namespace:BingMapsWPF_AzureMapsTiles"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800">
    <Grid>
        <azmaps:AzureMapsControl x:Name="MyMap" CredentialsProvider="<Your Azure Maps Key>"/>
    </Grid>
</Window>
```

## Known limitation

This solution overrides the map controls `Mode` property (map style) and changes it to `MercatorMode` which prevents requests to Bing Maps tiles from being made. This is done for performance reasons. However, it's possible that your app may have logic that checks the `Mode` property, and this would likely break some functionality of your app. A common scenario where this type of logic is often added is to a button that toggles the map mode/style between road and aerial. To keep things simple, for the apps where overriding the `Mode` would cause an issue, a property has been added to the Azure Maps control that disables this behaviour. Simply add `AllowMapModeOverride="False"` to the maps XAML. For example:

```xaml
<azmaps:AzureMapsControl x:Name="MyMap" AllowMapModeOverride="False" CredentialsProvider="<Your Azure Maps Key>"/>
```

## License

MIT

## Resources

- [Bing Maps WPF SDK documentation](https://learn.microsoft.com/en-us/previous-versions/bing/wpf-control/hh750210(v%3dmsdn.10))
- [Bing Maps WPF NuGet package](https://www.nuget.org/packages/Microsoft.Maps.MapControl.WPF)
- [Azure Maps documentation](https://learn.microsoft.com/en-us/azure/azure-maps/)

## Alternative solutions

This is one solution for those using the Bing Maps WPF SDK. There are two other common alternative solutions.

1. Use the Azure Maps Web SDK in a `WebView` control. This requires using a combination of c# and JavaScript code, but provides you with a very rich set of map capabilities.
2. Use a 3rd party (commerical) or open-source native map control for WPF. Here are few:
   - [Maps UI](https://github.com/Mapsui/Mapsui) – Fairly popular open source Native Map SDK.
   - [MapLibre Native](https://github.com/maplibre/maplibre-native) – MapLibre is the underlying rendering engine of the Azure Maps Web SDK and also the recommended control to use for iOS and Android so this is a good option to consider. Until recently there wasn’t much work around Windows but it looks like this has changed. Microsoft is a sponsor of the MapLibre community.
   - [XAML Map Control](https://github.com/ClemensFischer/XAML-Map-Control) – I just came across this one and it appears to have some API interface alignment with the UWP map control. I don’t know much about it, but worth a look.
   - [GreatMaps](https://github.com/radioman/greatmaps) – An older open source project focused on WPF and WinForm apps. 
  