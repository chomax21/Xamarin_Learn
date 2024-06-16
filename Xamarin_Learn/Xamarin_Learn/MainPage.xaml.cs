using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using Xamarin.Forms;

namespace Xamarin_Learn
{
    public partial class MainPage : ContentPage
    {
        Label label;     // Определяем поле переменную Label
        Entry entry;     // Определяем поле переменную Entry
        Button button;   // Определяем поле переменную Button
        protected HttpClient HttpClient { get; set; } // Определяем свойство http клиента

        public MainPage()
        {
            InitializeComponent();
            
            // Будем использовать IHttpClitnFactory для определения клиента, он помогает избежать исчерпание сокетов
            // Используем DI            
            var services = new ServiceCollection();
            // Регистрируем в контейнере AddHttpClient
            services.AddHttpClient();
            // Создаем провайдер сервисов
            var serviceProvider = services.BuildServiceProvider();
            // Используем паттерн Service Locator для извлечения экземпляра клиента
            var client = serviceProvider.GetService<IHttpClientFactory>();
            // Создаем именованный клиент
            this.HttpClient = client.CreateClient("xamarin");

            // В StackLayout помещаются все объекты xamarin.forms, создаем его для этого
            StackLayout stackLayout = new StackLayout();

            // Создаем Label и помещаем его в поле переменную
            label = new Label
            {
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Text = "Здесь отобразиться ваш город и погода",
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            };

            // Создаем Button и помещаем его в поле переменную
            button = new Button
            {
                Text = "Нажать!",
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Button)),
                BorderWidth = 1,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.CenterAndExpand
            };

            // Создаем Entry и помещаем его в поле переменную
            entry = new Entry
            {
                PlaceholderColor = Color.AliceBlue,
                Placeholder = "Укажите ваш город!)"
            };

            // Регистрируем обработчик - метод для события нажатия на выше созданную кнопку
            button.Clicked += OnButtonClicked;

            // Помещаем все объекты xamarin.forms в наш StackLayout
            stackLayout.Children.Add(label);
            stackLayout.Children.Add(entry);
            stackLayout.Children.Add(button);
            Content = stackLayout;
        }

        // Наш метод обработчик нажатия
        public void OnButtonClicked(object sender, EventArgs e)
        {
            // Для ошибок
            try
            {
                // Создаем нужные нам переменные
                var mainCity = "Москва";
                // API ключ 2GIS получили заранее
                var twoGisApiKey = "6dbb19a9-d206-4c6b-a896-4469b67c789a";
                var lat = "";
                var lon = "";
                // Делаем GET запрос и получаем по названию города координаты
                var twoGisResponce = HttpClient.GetAsync($"https://catalog.api.2gis.com/3.0/items/geocode?q={entry.Text ?? mainCity}&fields=items.point&key={twoGisApiKey}");
                if (twoGisResponce.Result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Парсим данные
                    var responce = twoGisResponce.Result.Content.ReadAsStringAsync().Result;
                    var content = JObject.Parse(responce)["result"]["items"].First();
                    var point = content["point"];
                    lat = point["lat"].ToString().Substring(0, 7).Replace(',', '.'); ;
                    lon = point["lon"].ToString().Substring(0, 7).Replace(',', '.'); ;
                }
                else
                {
                    label.TextColor = Color.Red;
                    label.Text = "Не пришел ответ от 2GIS :(";
                }
                
                // Используем координаты с 2GIS для API Yandex для получения погоды по координатам
                HttpClient.DefaultRequestHeaders.Add("X-Yandex-Weather-Key", "f2307327-4cc9-4d25-ace8-5f548686944d");
                var yandexResponce = HttpClient.GetAsync($"https://api.weather.yandex.ru/v2/forecast?lat={lat}&lon={lon}");

                if (yandexResponce.Result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Парсим данные
                    var yandexContent = yandexResponce.Result.Content.ReadAsStringAsync().Result;
                    var contentJson = JObject.Parse(yandexContent);
                    var yandexCity = contentJson["geo_object"]["locality"]["name"];
                    var wheaterTemp = contentJson["fact"]["temp"];
                    var wheaterFeelsLike = contentJson["fact"]["feels_like"];

                    label.Text = $"Температура в городе: {yandexCity} равна {wheaterTemp}, ощущается как {wheaterFeelsLike}!";
                }
                else
                {
                    label.TextColor = Color.Red;
                    label.Text = "Не пришел ответ от YandexPogoda:(";
                }
            }
            catch
            {
                label.BackgroundColor = Color.Red;
                label.Text = "Юра, мы все... :(";
            }

        }
    }


}
