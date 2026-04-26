using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SistemaCaixaPDV // <-- ATENÇÃO: Verifique o nome do projeto!
{
    public partial class TelaPagamento : Window
    {
        public string FormaPagamentoSelecionada { get; private set; } = "Dinheiro";

        public TelaPagamento(decimal total)
        {
            InitializeComponent();
            txtTotalPagar.Text = total.ToString("C");

            var config = BancoDeDados.ObterConfiguracoes();
            btnPix.IsEnabled = config.AceitaPix;
            btnCredito.IsEnabled = config.AceitaCredito;
            btnDebito.IsEnabled = config.AceitaDebito;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Se o painel PIX estiver aberto, o ESC apenas volta para os botões
                if (panelPix.Visibility == Visibility.Visible)
                {
                    btnVoltarPix_Click(null, null);
                }
                else
                {
                    this.DialogResult = false;
                    this.Close();
                }
            }

            // Só aceita os atalhos F1-F4 se o painel do PIX NÃO estiver aberto
            if (panelPix.Visibility == Visibility.Collapsed)
            {
                if (e.Key == Key.F1 || e.Key == Key.D1 || e.Key == Key.NumPad1)
                    ConfirmarPagamento("Dinheiro");
                else if ((e.Key == Key.F2 || e.Key == Key.D2 || e.Key == Key.NumPad2) && btnPix.IsEnabled)
                    btnPix_Click(null, null); // Chama a função que abre o painel do QR Code
                else if ((e.Key == Key.F3 || e.Key == Key.D3 || e.Key == Key.NumPad3) && btnCredito.IsEnabled)
                    ConfirmarPagamento("Cartão de Crédito");
                else if ((e.Key == Key.F4 || e.Key == Key.D4 || e.Key == Key.NumPad4) && btnDebito.IsEnabled)
                    ConfirmarPagamento("Cartão de Débito");
            }
        }

        // ==========================================
        // CLIQUES DOS BOTÕES NORMAIS
        // ==========================================
        private void btnDinheiro_Click(object sender, RoutedEventArgs e) => ConfirmarPagamento("Dinheiro");
        private void btnCredito_Click(object sender, RoutedEventArgs e) => ConfirmarPagamento("Cartão de Crédito");
        private void btnDebito_Click(object sender, RoutedEventArgs e) => ConfirmarPagamento("Cartão de Débito");

        // ==========================================
        // FLUXO DO PIX (QR CODE)
        // ==========================================
        private void btnPix_Click(object sender, RoutedEventArgs e)
        {
            var config = BancoDeDados.ObterConfiguracoes();

            if (string.IsNullOrWhiteSpace(config.ChavePix))
            {
                MessageBox.Show("Você não cadastrou nenhuma Chave PIX nas Configurações!\n\nVá em Configurações > Meios de Pagamento para cadastrar.", "Chave Ausente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Esconde os botões e mostra o painel do QR Code
            gridBotoes.Visibility = Visibility.Collapsed;
            panelPix.Visibility = Visibility.Visible;

            txtChavePixTela.Text = $"Chave: {config.ChavePix}";

            try
            {
                // Gera o QR Code usando a API do Google/QRServer (Requer Internet na máquina do caixa)
                string urlQrCode = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(config.ChavePix)}";
                imgQrCode.Source = new BitmapImage(new Uri(urlQrCode));
            }
            catch
            {
                MessageBox.Show("Sem conexão com a internet para gerar a imagem do QR Code. Peça para o cliente digitar a chave manualmente!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnVoltarPix_Click(object sender, RoutedEventArgs e)
        {
            // Volta para a tela de botões
            panelPix.Visibility = Visibility.Collapsed;
            gridBotoes.Visibility = Visibility.Visible;
        }

        private void btnConfirmarPix_Click(object sender, RoutedEventArgs e)
        {
            // O operador checou o celular e viu que o dinheiro caiu. Agora confirma!
            ConfirmarPagamento("PIX");
        }

        // ==========================================
        // FINALIZAÇÃO
        // ==========================================
        private void ConfirmarPagamento(string forma)
        {
            FormaPagamentoSelecionada = forma;
            this.DialogResult = true;
            this.Close();
        }
    }
}
