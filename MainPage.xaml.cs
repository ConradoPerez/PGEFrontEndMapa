using Mapsui;
using Mapsui.Tiling;
using Mapsui.Projections;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using System.Collections.ObjectModel;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;


namespace IntegrarMapa;

public partial class MainPage : ContentPage
{
    private bool modoAgregar = false;
    private MemoryLayer pinLayer;
    private ObservableCollection<Incidencia> incidencias;

    public MainPage()
    {
        InitializeComponent();

        incidencias = new ObservableCollection<Incidencia>();
        IncidenciasList.ItemsSource = incidencias;

        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Capa de pines
        pinLayer = new MemoryLayer
        {
            Name = "Incidencias",
            Features = new List<IFeature>(),
            IsMapInfoLayer = true
        };
        map.Layers.Add(pinLayer);

        // Vista inicial → Buenos Aires
        var (x, y) = SphericalMercator.FromLonLat(-58.3816, -34.6037);
        var center = new MPoint(x, y);
        map.Home = n => n.CenterOn(center);

        mapControl.Map = map;

        // Capturar clicks en el mapa
        mapControl.Info += OnMapInfo;
    }

    private void OnAgregarIncidenciaClicked(object sender, EventArgs e)
    {
        modoAgregar = true;
    }

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        if (!modoAgregar) return;
        if (e.MapInfo?.WorldPosition is MPoint pos)
        {
            // Crear pin visual
            var nuevaIncidencia = new PointFeature(pos);
            nuevaIncidencia.Styles.Add(new SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Brush(Mapsui.Styles.Color.Red),
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            });

            (pinLayer.Features as List<IFeature>)?.Add(nuevaIncidencia);
            mapControl.Refresh();

            // Agregar a la lista
            incidencias.Add(new Incidencia
            {
                Titulo = $"Incidencia #{incidencias.Count + 1}",
                Coordenadas = $"Lat: {pos.Y:0.0000}, Lon: {pos.X:0.0000}"
            });

            modoAgregar = false;
        }
    }
    private async void OnIncidenciaMenuClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Incidencia incidencia)
        {
            string nuevoNombre = await DisplayPromptAsync(
                "Editar incidencia",
                "Nuevo nombre:",
                initialValue: incidencia.Titulo);

            if (!string.IsNullOrWhiteSpace(nuevoNombre))
            {
                incidencia.Titulo = nuevoNombre;

                // 🔄 Forzar refresco en la lista
                var index = incidencias.IndexOf(incidencia);
                incidencias.RemoveAt(index);
                incidencias.Insert(index, incidencia);
            }
        }
    }

}

public class Incidencia
{
    public string Titulo { get; set; }
    public string Coordenadas { get; set; }
}
