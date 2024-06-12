/**
 * MIT License
 * Copyright (c) 2024 Ricky Brundritt
 * https://github.com/rbrundritt/AzureMapsWPFControl
 */

using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Overlays;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media;

namespace Azure.Maps.WPF
{
    /// <summary>
    /// An Azure Maps powered version of the Bing Maps WPF control.
    /// </summary>
    public partial class AzureMapsControl: Map
    {
        internal const string RenderServiceAPIVersion = "2024-04-01";

        private const string AerialLabelTilesetId = "microsoft.base.hybrid.road";
        private static bool CultureDependancyCallbackSet = false;
        private string AzureMapsKey = string.Empty;

        private string LastSetAzureMapsTilesetId = string.Empty;
        private string LastSetMapCulture = string.Empty;
        private bool AerialLabelsShown = false;

        private MapTileLayer? BaseMapTileLayer = null;
        private MapTileLayer? BaseMapAerialLabelsTileLayer = null;

        private Copyright? CopyrightControl = null;

        private ObservableCollection<string> CopyrightAttributes = null;

        private static HttpClient SharedAttributesClient = new HttpClient()
        {
            BaseAddress = new Uri("https://atlas.microsoft.com")
        };

        private static bool ClientHeadersAdded = false;

        /// <summary>
        /// An Azure Maps powered version of the Bing Maps WPF control.
        /// </summary>
        public AzureMapsControl(): base()
        {
            this.Loaded += AzureMapsControl_Loaded;
            this.LoadingError += AzureMapControl_LoadingError;
            this.ModeChanged += AzureMapControl_ModeChanged;

            //Update copyrights after the map moves.
            this.ViewChangeEnd += (s, e) =>
            {
                UpdateCopyrights();
            };  
            
            //Need to override the Bing Maps culture dependancy property to trigger a callback when the culture changes.
            if (!CultureDependancyCallbackSet)
            {
                //Add a callback for when the culture property changes.
                MapCore.CultureProperty.OverrideMetadata(typeof(AzureMapsControl), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCulturePropertyChanged)));
                CultureDependancyCallbackSet = true;
            }

            //Add headers to the shared http client.
            if (!ClientHeadersAdded)
            {
                SharedAttributesClient.DefaultRequestHeaders.Add("Ms-Am-Request-Origin", "MapControl");
                SharedAttributesClient.DefaultRequestHeaders.Add("Map-Agent", $"MapControl/{RenderServiceAPIVersion} (WPF)");
                ClientHeadersAdded = true;
            }
        }

        /// <summary>
        /// Handler for when the map culture property changes. 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnCulturePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var map = (d as AzureMapsControl);
            if(map != null)
            {
                map.ConvertMapMode().Wait(); 
            }
        }

        /// <summary>
        /// Species if the Bing Maps map modes can be overriden when adding Azure Maps as a base map.
        /// When true, the map mode will be set to MercatorMode, which will remove the Bing Maps tile layer and prevent failed requests to Bing Maps tile services (good for performance).
        /// However, if your application has any logic that checks the map mode later, it will always see MercatorMode and not the Bing Maps map mode you set, which may break logic. 
        /// You can set this value to false to allow the Bing Maps mode to persist.
        /// A common scenario where this occurs is if a single button is used to toggle the map style. 
        /// </summary>
        public bool AllowMapModeOverride { get; set; } = true;

        /// <summary>
        /// When the map is loaded, get the Azure Maps key from the credentials provider and set the initial map mode/style.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AzureMapsControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.CredentialsProvider.GetCredentials((key) =>
            {
                //Store the Azure Maps key.
                AzureMapsKey = key.ApplicationId;

                //Set the initial map mode/style. 
                ConvertMapMode();
            });

            //Look for the logo control and hide it since Azure Maps is being used and not Bing Maps. Azure Maps does not require a logo to be shown. 
            var logoControl = FindControlInVisualTree(this, "Microsoft.Maps.MapControl.WPF.Overlays.Logo");
            if(logoControl != null)
            {
                (logoControl as Logo).Visibility = Visibility.Collapsed;
            }
      
            //Find the copyright control so we can populate it later.
            var cc = FindControlInVisualTree(this, "Microsoft.Maps.MapControl.WPF.Overlays.Copyright");
            if (cc != null)
            {
                CopyrightControl = cc as Copyright;

                var itemsControl = FindControlInVisualTree(CopyrightControl, "System.Windows.Controls.ItemsControl");
                if (itemsControl != null)
                {
                    CopyrightAttributes = new ObservableCollection<string>();
                    (itemsControl as System.Windows.Controls.ItemsControl).ItemsSource = CopyrightAttributes;
                }

                UpdateCopyrights();
            }
        }

        /// <summary>
        /// Event handler for when an error occurs in the loading of the map control. 
        /// This is used to handle credential errors, and to capture the Azure Maps key that might have been passed into the credentials provider.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AzureMapControl_LoadingError(object? sender, Microsoft.Maps.MapControl.WPF.LoadingErrorEventArgs e)
        {
            if(e.LoadingException.Message.Contains("Microsoft.Maps.MapControl.WPF.Core.CredentialsInvalidException"))
            {
                e.Handled = true;

                //Give the map a moment to generate a credentials error message, then remove it.
                Task.Run(() =>
                {
                    Thread.Sleep(20); // delay

                    //Get back onto the main thread.
                    Dispatcher.Invoke(() =>
                    {
                        //Loop through the children of the map control to find the error message.
                        for(int i = 0; i < this.Children.Count; i++)
                        {
                            //Check to see if the child is an instance of a "Microsoft.Maps.MapControl.WPF.Overlays.LoadingErrorMessage".
                            if (this.Children[i] is Microsoft.Maps.MapControl.WPF.Overlays.LoadingErrorMessage)
                            {
                                //Remove the error message.
                                this.Children.Remove(this.Children[i]);
                                break;
                            }
                        }
                    });
                });
            }
        }

        /// <summary>
        /// Helper function to find a control in the visual tree of the map control recursively.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="controlName"></param>
        /// <returns></returns>
        private DependencyObject FindControlInVisualTree(DependencyObject obj, string controlName)
        {
            if (obj.GetType().ToString().Equals(controlName))
            {
                return obj;
            }

            var numElms = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < numElms; i++)
            {
                var result = FindControlInVisualTree(VisualTreeHelper.GetChild(obj, i), controlName);
                if(result != null)
                {
                    return result;
                }   
            }

            return null;
        }

        /// <summary>
        /// Converts the Bing Maps map mode to an Azure Maps map style.
        /// </summary>
        internal async Task ConvertMapMode()
        {
            if (!string.IsNullOrWhiteSpace(AzureMapsKey))
            {
                bool cultureChanged = !this.Culture.Equals(LastSetMapCulture);

                string azureMapsTilesetId = string.Empty;

                switch (this.Mode)
                {
                    case RoadMode roadMode:
                        azureMapsTilesetId = "microsoft.base.road";
                        AerialLabelsShown = false;
                        break;
                    case AerialMode aerialMode:
                        azureMapsTilesetId = "microsoft.imagery";
                        AerialLabelsShown = aerialMode.Labels;
                        break;
                    default:
                    case MercatorMode mercatorMode:
                        //Do nothing if the mode is not one of the Bing Maps modes. Mercator mode is generally used for custom tile sources.
                        break;
                }

                bool updateCopyrights = LastSetAzureMapsTilesetId != azureMapsTilesetId;

                //Load the Azure Maps tiles.
                if(!string.IsNullOrEmpty(azureMapsTilesetId) || 
                   (!string.IsNullOrEmpty(LastSetAzureMapsTilesetId) && cultureChanged))
                {
                    if (!string.IsNullOrEmpty(azureMapsTilesetId))
                    {
                        LastSetAzureMapsTilesetId = azureMapsTilesetId;
                    }

                    LastSetMapCulture = this.Culture;

                    //Check to see if the user has allowed the map mode to be overridden.
                    if (AllowMapModeOverride)
                    {
                        //Set the map mode to Mercator to remove the Bing Maps tiles.
                        this.Mode = new MercatorMode();
                    }

                    //Create a tile source for Azure Maps tiles.
                    var azureMapsTileSource = new AzureMapsTileSource(AzureMapsKey, LastSetAzureMapsTilesetId, LastSetMapCulture);

                    //Check to see if a tile layer has been created for the Azure Maps tiles.
                    if(BaseMapTileLayer == null)
                    {
                        //Create a tile layer for the Azure Maps tiles.
                        BaseMapTileLayer = new MapTileLayer()
                        {
                            TileSource = azureMapsTileSource,
                            Visibility = Visibility.Visible
                        };
                        //Insert the Azure Maps tile layer as the first child of the map control so that it is rendered below all other layers.
                        this.Children.Insert(0, BaseMapTileLayer);
                    }
                    else
                    {
                        //Update the tile source of the existing tile layer.
                        BaseMapTileLayer.TileSource = azureMapsTileSource;
                    }

                    var azureMapsLabelTileSource = new AzureMapsTileSource(AzureMapsKey, AerialLabelTilesetId, LastSetMapCulture);// "microsoft.base.hybrid.darkgrey"

                    //Check to see if a tile layer has been created for the Azure Maps label tiles.
                    if (BaseMapAerialLabelsTileLayer == null)
                    {
                        //Create a tile layer for the Azure Maps label tiles.
                        BaseMapAerialLabelsTileLayer = new MapTileLayer()
                        {
                            TileSource = azureMapsLabelTileSource
                        };

                        //Insert the Azure Maps tile layer as the second child of the map control so that it is rendered just above the base map tile layer.
                        this.Children.Insert(1, BaseMapAerialLabelsTileLayer);                        
                    } 
                    else 
                    {
                        if (cultureChanged)
                        {
                            BaseMapAerialLabelsTileLayer.TileSource = azureMapsLabelTileSource;
                        }
                    }
                }

                if(BaseMapAerialLabelsTileLayer != null)
                {
                    BaseMapAerialLabelsTileLayer.Visibility = AerialLabelsShown ? Visibility.Visible : Visibility.Collapsed;
                }

                if (AerialLabelsShown)
                {
                    //Bug workaround: For some reason the label layer is not always appearing when switching between layers. Updating layout seems to fix this.
                    this.UpdateLayout();
                }

                if(updateCopyrights)
                {
                    //Update the copyrights.
                    await UpdateCopyrights();
                }
            }
        }

        /// <summary>
        /// Event handler for when the map mode changes. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AzureMapControl_ModeChanged(object? sender, MapEventArgs e)
        {
            ConvertMapMode();
        }

        /// <summary>
        /// Update copyrights by making a request to the Azure Maps attribution service.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateCopyrights()
        {
            if (CopyrightAttributes != null)
            {
                //https://atlas.microsoft.com/map/attribution?api-version=2022-08-01&tilesetId={tilesetId}&zoom={zoom}&bounds={bounds}

                var zoom = Math.Round(this.ZoomLevel);
                var bounds = string.Format("{0:0.#####},{1:0.#####},{2:0.#####},{3:0.#####}", this.BoundingRectangle.West, this.BoundingRectangle.South, this.BoundingRectangle.East, this.BoundingRectangle.North);
                
                var msftAttribute = "© " + DateTime.Now.Year.ToString() + " Microsoft";
                var attributes = new List<string>() { msftAttribute };

                //Get attributions for the base map style.
                var basemapAttributes = await SharedAttributesClient.GetStringAsync($"/map/attribution?api-version={RenderServiceAPIVersion}&tilesetId={LastSetAzureMapsTilesetId}&zoom={zoom}&bounds={bounds}&subscription-key={AzureMapsKey}");
                AddCopyrightAttributes(basemapAttributes, attributes);

                //If labels are shown with aerial imagery, get attributions for the label style.
                if (AerialLabelsShown)
                {
                    var labelsAttributes = await SharedAttributesClient.GetStringAsync($"/map/attribution?api-version={RenderServiceAPIVersion}&tilesetId={AerialLabelTilesetId}&zoom={zoom}&bounds={bounds}&subscription-key={AzureMapsKey}");
                    AddCopyrightAttributes(labelsAttributes, attributes);
                }

                //Check to see if the attributes are the same as those already in the Copyright attributes container.
                //If they are, do nothing as no need to update the UI. 
                if (!attributes.SequenceEqual(CopyrightAttributes))
                {
                    CopyrightAttributes.Clear();

                    foreach(var attribute in attributes)
                    {
                        CopyrightAttributes.Add(attribute);
                    }
                }
            }
        }

        /// <summary>
        /// Takes a JSON response from the Azure Maps attribution service and extracts the copyright attributions.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="attributeContainer"></param>
        private void AddCopyrightAttributes(string response, List<string> attributeContainer)
        {
            if (!string.IsNullOrWhiteSpace(response))
            {
                //Remove any HTML tags from the attribute, and convert "\u0026copy; to ©.
                response = System.Text.RegularExpressions.Regex.Replace(response, "<.*?>", string.Empty).Replace("\u0026copy;", "©");
                var json = JsonNode.Parse(response) as JsonObject;

                if (json != null)
                {
                    //Get the "copyrights" property as an array of string.
                    var attributions = json["copyrights"] as JsonArray;
                    if (attributions != null)
                    {
                        foreach (var a in attributions)
                        {
                            if (a != null)
                            {
                                var val = a.ToString();
                                if (!string.IsNullOrWhiteSpace(val) && !attributeContainer.Contains(val))
                                {
                                    attributeContainer.Add(a.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A tile source for Azure Maps tiles.
    /// </summary>
    public class AzureMapsTileSource : TileSource
    {
        private const string AzureMapsTileUrl = "https://atlas.microsoft.com/map/tile?api-version=" + AzureMapsControl.RenderServiceAPIVersion + "&tilesetId={tilesetId}&zoom={z}&x={x}&y={y}&subscription-key={AzureMapsKey}&view=Auto&tileSize=256&language={language}";
        private string AzureMapsKey = string.Empty;

        public AzureMapsTileSource(string azureMapsKey, string tilesetId, string cultureCode = "en-US")
        {
            AzureMapsKey = azureMapsKey;

            //Currently passing Bing Maps culture code to Azure Maps language code blindly. 
            //Considered having a lookup table but that would quickly become out of date as Azure Maps supports more languages.
            //Doing testing, it appears that there is good culture code mapping within the service already and it falls back gracefully.
            UriFormat = AzureMapsTileUrl.Replace("{AzureMapsKey}", AzureMapsKey).Replace("{tilesetId}", tilesetId).Replace("{language}", cultureCode);
        }

        /// <summary>
        /// Need to override the GetUri method to pass in the x, y, and zoom level to the Azure Maps tile URL.
        /// Bing Maps WPF by default only sets {quadkey}.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoomLevel"></param>
        /// <returns></returns>
        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            return new Uri(UriFormat.
                        Replace("{x}", x.ToString()).
                        Replace("{y}", y.ToString()).
                        Replace("{z}", zoomLevel.ToString()));
        }
    }
}