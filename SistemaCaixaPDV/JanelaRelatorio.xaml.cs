using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaCaixaPDV
{
    public partial class JanelaRelatorio : Window
    {
        public JanelaRelatorio()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Puxa o nome da loja das configurações do sistema
            var config = BancoDeDados.ObterConfiguracoes();
            txtNomeLoja.Text = string.IsNullOrWhiteSpace(config.NomeLoja) ? "SISTEMA PDV" : config.NomeLoja.ToUpper();

            // 2. Registra o momento exato da emissão
            txtDataHora.Text = "Emitido em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // 3. Preenche a tabela chamando a função que nós já tínhamos criado no BancoDeDados.cs
            gridRelatorio.ItemsSource = BancoDeDados.ObterRelatorioProdutos();
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();

            // Abre a janela de seleção de impressora do Windows
            if (printDialog.ShowDialog() == true)
            {
                // Esconde o botão azul temporariamente para ele não sair impresso no papel
                btnImprimir.Visibility = Visibility.Hidden;

                // Manda o conteúdo visual da janela inteira para a impressora
                printDialog.PrintVisual(this.Content as Visual, "Relatório de Produtos - WV Systems");

                // Devolve o botão para a tela
                btnImprimir.Visibility = Visibility.Visible;
            }
        }
    }
}