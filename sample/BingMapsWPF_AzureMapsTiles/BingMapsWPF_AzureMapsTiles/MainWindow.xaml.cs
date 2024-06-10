using Microsoft.Maps.MapControl.WPF;
using System.Windows;
using System.Windows.Controls;

namespace BingMapsWPF_AzureMapsTiles
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RoadMode_Clicked(object sender, RoutedEventArgs e)
        {
            MyMap.Mode = new RoadMode();
        }

        private void AerialMode_Clicked(object sender, RoutedEventArgs e)
        {
            MyMap.Mode = new AerialMode(false);
        }
        private void AerialLabelsMode_Clicked(object sender, RoutedEventArgs e)
        {
            MyMap.Mode = new AerialMode(true);
        }

        private void ToggleMapMode_Clicked(object sender, RoutedEventArgs e)
        {
            if (MyMap.Mode.ToString() == "Microsoft.Maps.MapControl.WPF.RoadMode")
            {
                //Set the map mode to Aerial with labels
                MyMap.Mode = new AerialMode(true);
            }
            else if (MyMap.Mode.ToString() == "Microsoft.Maps.MapControl.WPF.AerialMode")
            {
                //Set the map mode to RoadMode
                MyMap.Mode = new RoadMode();
            }
        }

        private void CultureChange_Clicked(object sender, RoutedEventArgs e)
        {
            MyMap.Culture = (string)(sender as Button).Content;
        }
    }
}