using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SistemaCaixaPDV // <-- Verifique o nome do seu projeto
{
    public partial class TelaBuscaProduto : Window
    {
        // Esta variável guarda o produto que você escolher na lista
        public Produto ProdutoSelecionado { get; private set; }

        public TelaBuscaProduto()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Já joga o cursor de digitação na busca e lista tudo
            txtBusca.Focus();
            Pesquisar("");
        }

        private void txtBusca_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Pesquisa no banco a cada letra que você digita
            Pesquisar(txtBusca.Text);
        }

        private void Pesquisar(string termo)
        {
            gridProdutos.ItemsSource = BancoDeDados.BuscarProdutosPorNome(termo);
        }

        private void btnSelecionar_Click(object sender, RoutedEventArgs e)
        {
            ConfirmarSelecao();
        }

        private void gridProdutos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmarSelecao();
        }

        private void ConfirmarSelecao()
        {
            if (gridProdutos.SelectedItem != null)
            {
                // Pega o item clicado, avisa que deu tudo certo e fecha a tela
                ProdutoSelecionado = (Produto)gridProdutos.SelectedItem;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Selecione um produto na lista primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Atalhos de agilidade
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close(); // ESC fecha
            if (e.Key == Key.Enter) ConfirmarSelecao(); // ENTER confirma
            if (e.Key == Key.Down) gridProdutos.Focus(); // Seta pra baixo vai pra lista
        }
    }
}
