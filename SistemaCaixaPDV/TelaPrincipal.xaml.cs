using System;
using System.Windows;
using System.Windows.Threading;

namespace SistemaCaixaPDV
{
    public partial class TelaPrincipal : Window
    {
        private DispatcherTimer timer;

        public TelaPrincipal()
        {
            InitializeComponent();
            BancoDeDados.InicializarBanco(); // <--- INICIALIZAÇÃO SEGURA ADICIONADA AQUI!
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Inicia o relógio do Menu Principal
            timer = new DispatcherTimer();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            // Escreve a data atual formatada (Ex: Segunda-feira, 01 de Janeiro de 2026)
            txtData.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy");
            Timer_Tick(null, null);

            // Tenta puxar o nome da loja do banco de dados para mostrar no topo
            try
            {
                var config = BancoDeDados.ObterConfiguracoes();
                if (config != null && !string.IsNullOrEmpty(config.NomeLoja))
                {
                    txtNomeLoja.Text = config.NomeLoja;
                }
            }
            catch { }
        }

        private void Timer_Tick(object sender, EventArgs e)
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
        // BOTÕES EM CONSTRUÇÃO (Para não dar erro)
        // ==========================================

        private void btnPdv_Click(object sender, RoutedEventArgs e)
        {
            TelaPDV config = new TelaPDV();
            config.ShowDialog();
        }

        private void btnEmissorNFe_Click(object sender, RoutedEventArgs e)
        {
            TelaEmissorNFe config = new TelaEmissorNFe();
            config.ShowDialog();
        }

        private void btnProdutos_Click(object sender, RoutedEventArgs e)
        {
            TelaCadastroProduto config = new TelaCadastroProduto();
            config.ShowDialog();
        }

        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            TelaClientes config = new TelaClientes();
            config.ShowDialog();
        }

        private void btnRelatorios_Click(object sender, RoutedEventArgs e)
        {
            TelaRelatorios config = new TelaRelatorios();
            config.ShowDialog();
        }

        private void btnCaixa_Click(object sender, RoutedEventArgs e)
        {
            TelaFechamentoCaixa config = new TelaFechamentoCaixa();
            config.ShowDialog();
        }

        private void btnDespesas_Click(object sender, RoutedEventArgs e)
        {
            TelaDespesas config = new TelaDespesas();
            config.ShowDialog();
        }

        private void btnReceber_Click(object sender, RoutedEventArgs e)
        {
            TelaContasReceber config = new TelaContasReceber();
            config.ShowDialog();
        }
    }
}