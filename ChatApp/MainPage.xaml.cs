using Microsoft.Maui.Controls;

namespace ChatApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnEnterChatClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            if (!string.IsNullOrEmpty(username))
            {
                // Переход на экран чата
                await Navigation.PushAsync(new ChatPage(username));
            }
            else
            {
                await DisplayAlert("Ошибка", "Введите имя пользователя", "OK");
            }
        }
    }
}
