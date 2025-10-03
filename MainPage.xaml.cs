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

        // Cargar el menú inicial
        MenuContainer.Content = CrearVistaMenu();
    }

    // =========================
    // 🚩 EVENTOS DE MENÚ
    // =========================
    private void OnAgregarIncidenciaClicked(object sender, EventArgs e)
    {
        modoAgregar = true;
        MenuContainer.Content = CrearVistaIncidencias(); // Cambiar a listado de incidencias
    }

    private void OnBuscarIncidenciaClicked(object sender, EventArgs e)
    {
        MenuContainer.Content = CrearVistaBusqueda();
    }

    private async void OnIrPerfilClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Perfil", "Aquí se mostraría el perfil del usuario.", "OK");
    }

    // =========================
    // 🚩 EVENTOS DEL MAPA
    // =========================
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
                Fill = new Brush(Color.Red),
                Outline = new Pen(Color.White, 2)
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

    // =========================
    // 🚩 VISTAS DEL MENÚ
    // =========================
    private View CrearVistaMenu()
{
    var btnAgregar = new Button { Text = "➕ Agregar incidencia", BackgroundColor = Colors.Red, TextColor = Colors.White, CornerRadius = 10 };
    btnAgregar.Clicked += OnAgregarIncidenciaClicked;

    var btnBuscar = new Button { Text = "🔎 Buscar incidencia", Margin = new Thickness(0,10,0,0) };
    btnBuscar.Clicked += OnBuscarIncidenciaClicked;

    var btnPerfil = new Button { Text = "👤 Ir a mi perfil" };
    btnPerfil.Clicked += OnIrPerfilClicked;

    return new VerticalStackLayout
    {
        Children =
        {
            new Label { Text = "📌 Menú", FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
            btnAgregar,
            btnBuscar,
            btnPerfil
        }
    };
}


    private View CrearVistaIncidencias()
    {
        var btnVolver = new Button { Text = "⬅ Volver al menú" };
        btnVolver.Clicked += (s, e) => MenuContainer.Content = CrearVistaMenu();

        return new VerticalStackLayout
        {
            Children =
        {
            new Label { Text = "📋 Incidencias", FontAttributes = FontAttributes.Bold, FontSize = 18, HorizontalOptions = LayoutOptions.Center },

            new CollectionView
            {
                ItemsSource = incidencias,
                ItemTemplate = new DataTemplate(() =>
                {
                    var frame = new Frame { BorderColor = Colors.Gray, CornerRadius = 8, Padding = 5, Margin = 5 };

                    var title = new Label { FontAttributes = FontAttributes.Bold };
                    title.SetBinding(Label.TextProperty, "Titulo");

                    var coords = new Label { FontSize = 12 };
                    coords.SetBinding(Label.TextProperty, "Coordenadas");

                    var button = new Button { Text = "⋮", FontSize = 18, BackgroundColor = Colors.Transparent };
                    button.SetBinding(Button.CommandParameterProperty, ".");
                    button.Clicked += OnIncidenciaMenuClicked;

                    var grid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        }
                    };

                    grid.Add(title, 0, 0);
                    grid.Add(button, 1, 0);

                    frame.Content = new VerticalStackLayout
                    {
                        Children = { grid, coords }
                    };

                    return frame;
                })
            },

            btnVolver
        }
        };
    }

    private View CrearVistaBusqueda()
    {
        var entry = new Entry { Placeholder = "Buscar por nombre..." };
        var date = new DatePicker();

        var btnBuscar = new Button { Text = "Buscar" };
        btnBuscar.Clicked += (s, e) =>
        {
            var query = entry.Text;
            var fecha = date.Date;
            DisplayAlert("Buscar", $"Nombre: {query}, Fecha: {fecha:dd/MM/yyyy}", "OK");
        };

        var btnVolver = new Button { Text = "⬅ Volver al menú" };
        btnVolver.Clicked += (s, e) => MenuContainer.Content = CrearVistaMenu();

        return new VerticalStackLayout
        {
            Children =
        {
            new Label { Text = "🔎 Buscar Incidencia", FontAttributes = FontAttributes.Bold, FontSize = 18, HorizontalOptions = LayoutOptions.Center },
            entry,
            date,
            btnBuscar,
            btnVolver
        }
        };
    }

}

public class Incidencia
{
    public string Titulo { get; set; }
    public string Coordenadas { get; set; }
}
