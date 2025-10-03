namespace IntegrarMapa;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();   // si el XAML compila, esto existe
        MainPage = new MainPage();
    }
}
