using System;
using System.Windows;
using System.Windows.Threading;

namespace SistemaCaixaPDV
{
    public partial class TelaPrincipal : Window
    {
        // BLINDAGEM: Instanciação direta em readonly evita aviso CS8618 (Campo não anulável)
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public TelaPrincipal()
        {
            InitializeComponent();
            BancoDeDados.InicializarBanco(); // Garante a integridade do SQLite antes de qualquer tela abrir
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Inicia o relógio do Menu Principal (remoção da instanciação duplicada)
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Escreve a data atual formatada (Ex: Segunda-feira, 01 de Janeiro de 2026)
            txtData.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy");

            // Primeira chamada extraída para método seguro (Evita injetar null, null em eventos)
            AtualizarRelogio();

            // Puxa o nome da loja do banco de dados para mostrar no topo
            try
            {
                var config = BancoDeDados.ObterConfiguracoes();
                if (config != null && !string.IsNullOrEmpty(config.NomeLoja))
                {
                    txtNomeLoja.Text = config.NomeLoja;
                }
            }
            catch { /* Suprime falhas visuais não críticas no carregamento */ }
        }

        // Assinatura do delegate adaptada para aceitar remetentes anuláveis (CS8622)
        private void Timer_Tick(object? sender, EventArgs e)
        {
            AtualizarRelogio();
        }

        private void AtualizarRelogio()
        {
            txtRelogio.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        // ==========================================
        // CLIQUES DOS BOTÕES DO MENU
        // ==========================================

        private void btnOrdemServico_Click(object sender, RoutedEventArgs e)
        {
            TelaOrdemServico telaOS = new TelaOrdemServico();
            telaOS.ShowDialog();
        }

        private void btnPdvRapido_Click(object sender, RoutedEventArgs e)
        {
            // O seu PDV Rápido (Frente de Caixa) está no MainWindow
            MainWindow pdv = new MainWindow();
            pdv.ShowDialog();
        }

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            TelaConfiguracoes config = new TelaConfiguracoes();
            config.ShowDialog();
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Deseja realmente sair do sistema?", "Encerrar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        // ==========================================
        // MÓDULOS DO SISTEMA
        // ==========================================

        private void btnPdv_Click(object sender, RoutedEventArgs e)
        {
            TelaPDV tela = new TelaPDV();
            tela.ShowDialog();
        }

        private void btnEmissorNFe_Click(object sender, RoutedEventArgs e)
        {
            TelaEmissorNFe tela = new TelaEmissorNFe();
            tela.ShowDialog();
        }

        private void btnProdutos_Click(object sender, RoutedEventArgs e)
        {
            TelaCadastroProduto tela = new TelaCadastroProduto();
            tela.ShowDialog();
        }

        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            TelaClientes tela = new TelaClientes();
            tela.ShowDialog();
        }

        private void btnRelatorios_Click(object sender, RoutedEventArgs e)
        {
            TelaRelatorios tela = new TelaRelatorios();
            tela.ShowDialog();
        }

        private void btnCaixa_Click(object sender, RoutedEventArgs e)
        {
            TelaFechamentoCaixa tela = new TelaFechamentoCaixa();
            tela.ShowDialog();
        }

        private void btnDespesas_Click(object sender, RoutedEventArgs e)
        {
            TelaDespesas tela = new TelaDespesas();
            tela.ShowDialog();
        }

        private void btnReceber_Click(object sender, RoutedEventArgs e)
        {
            TelaContasReceber tela = new TelaContasReceber();
            tela.ShowDialog();
        }
    }
}