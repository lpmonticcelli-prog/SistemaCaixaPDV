using System;
using System.Windows;

namespace SistemaCaixaPDV // <-- ATENÇÃO: Verifique o nome do projeto!
{
    public partial class TelaFechamentoCaixa : Window
    {
        public TelaFechamentoCaixa()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarResumoDoDia();
        }

        private void CarregarResumoDoDia()
        {
            // Pega a data de hoje para filtrar as vendas
            string hoje = DateTime.Now.ToString("yyyy-MM-dd");
            txtDataFechamento.Text = "Movimentação do dia: " + DateTime.Now.ToString("dd/MM/yyyy");

            // 1. Soma das Vendas por Categoria
            decimal vendaDinheiro = BancoDeDados.CalcularTotalVendasPorPagamento("Dinheiro", hoje);
            decimal vendaPix = BancoDeDados.CalcularTotalVendasPorPagamento("PIX", hoje);
            decimal vendaCartao = BancoDeDados.CalcularTotalVendasPorPagamento("Cartão de Crédito", hoje) +
                                  BancoDeDados.CalcularTotalVendasPorPagamento("Cartão de Débito", hoje);

            decimal totalVendido = vendaDinheiro + vendaPix + vendaCartao;

            // 2. Soma das Movimentações Extras (Gaveta)
            decimal suprimento = BancoDeDados.CalcularTotalMovimentacao("Suprimento", hoje);
            decimal sangria = BancoDeDados.CalcularTotalMovimentacao("Sangria", hoje);

            // 3. O Faturamento Real da Gaveta Física (Dinheiro de Venda + Troco - Retiradas)
            decimal saldoGaveta = vendaDinheiro + suprimento - sangria;

            // 4. Joga os valores na tela com formatação de Moeda (R$)
            txtVendaDinheiro.Text = vendaDinheiro.ToString("C");
            txtVendaPix.Text = vendaPix.ToString("C");
            txtVendaCartao.Text = vendaCartao.ToString("C");
            txtTotalVendido.Text = totalVendido.ToString("C");

            txtSuprimento.Text = suprimento.ToString("C");
            txtSangria.Text = sangria.ToString("C");

            txtSaldoGaveta.Text = saldoGaveta.ToString("C");
        }

        private void btnEncerrarTurno_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Atenção: Encerrar o turno irá zerar o caixa atual para o próximo operador.\n\nO dinheiro físico na gaveta confere com o valor em verde mostrado na tela?", "Encerrar Turno", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                // Pede a senha do gerente para permitir o zeramento do caixa!
                TelaSenhaGerente telaSenha = new TelaSenhaGerente();
                telaSenha.Owner = this;
                telaSenha.ShowDialog();

                if (telaSenha.Autorizado)
                {
                    BancoDeDados.LimparDadosDoTurno();
                    MessageBox.Show("Turno encerrado com sucesso! O caixa foi zerado para a próxima operação.", "Fechamento Concluído", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
        }

        private void btnVoltar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
