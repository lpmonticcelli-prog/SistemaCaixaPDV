using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SistemaCaixaPDV
{
    public partial class TelaOrdemServico : Window
    {
        public ObservableCollection<ItemOS> ItensDaOS { get; set; }

        public TelaOrdemServico()
        {
            InitializeComponent();

            // Inicializa a lista vazia para a tabela (DataGrid)
            ItensDaOS = new ObservableCollection<ItemOS>();
            gridItensOS.ItemsSource = ItensDaOS;

            // Preenche as datas automaticamente com o dia de hoje
            dpDataEntrada.SelectedDate = DateTime.Now;
            dpDataSaida.SelectedDate = DateTime.Now.AddDays(3); // Previsão de entrega 3 dias
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Atalhos de teclado baseados na imagem
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            // Adicionaremos o resto dos atalhos (F2, F4, etc) na próxima fase
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtBuscaProdutoOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Aqui faremos a busca no banco de dados SQLite depois
                MessageBox.Show("Função de buscar produto/serviço no banco de dados será ativada a seguir!", "Em construção", MessageBoxButton.OK, MessageBoxImage.Information);
                txtBuscaProdutoOS.Clear();
            }
        }
    }
}
