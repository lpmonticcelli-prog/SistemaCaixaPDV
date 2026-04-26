using System.Windows;
using System.Windows.Media;

namespace SistemaCaixaPDV
{
    public partial class JanelaEtiqueta : Window
    {
        // Ao abrir a janela, ela recebe os dados do produto que estava na tela principal
        public JanelaEtiqueta(string descricao, string preco, string codigo)
        {
            InitializeComponent();

            var config = BancoDeDados.ObterConfiguracoes();
            lblNomeLoja.Text = string.IsNullOrWhiteSpace(config.NomeLoja) ? "WV SYSTEMS" : config.NomeLoja.ToUpper();

            lblDescricao.Text = descricao;
            lblPreco.Text = "R$ " + preco;
            lblCodigo.Text = "CÓD: " + codigo;
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.PrintDialog pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() == true)
            {
                // Imprime APENAS a borda branca (a etiqueta em si), ignorando o fundo cinza e o botão
                pd.PrintVisual(areaEtiqueta, "Etiqueta de Produto");
                this.Close();
            }
        }
    }
}