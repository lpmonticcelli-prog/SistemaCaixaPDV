using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SistemaCaixaPDV
{
    public partial class TelaPDV : Window
    {
        // Lista que vai alimentar o DataGrid do carrinho
        public ObservableCollection<ItemCarrinho> carrinhoDeProdutos { get; set; } = new ObservableCollection<ItemCarrinho>();

        public TelaPDV()
        {
            InitializeComponent();
            gridCarrinho.ItemsSource = carrinhoDeProdutos;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Coloca a data atual
            txtDataVenda.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // Carrega os clientes logo ao abrir a tela
            CarregarClientes();
        }

        // ===============================================================
        // BUSCAR CLIENTES NO BANCO DE DADOS (BLINDADO)
        // ===============================================================
        private void CarregarClientes()
        {
            var clientes = BancoDeDados.ListarClientes();
            cbCliente.ItemsSource = clientes;

            if (clientes.Count == 0)
            {
                MessageBox.Show("A busca funcionou, mas a tabela 'Clientes' está VAZIA!\nVerifique se cadastrou o cliente no lugar certo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnAtualizarClientes_Click(object sender, RoutedEventArgs e)
        {
            CarregarClientes();
        }

        private void cbCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCliente.SelectedValue != null)
            {
                int idCliente = Convert.ToInt32(cbCliente.SelectedValue);

                // Utiliza o método blindado centralizado
                var cliente = BancoDeDados.ObterClientePorId(idCliente);

                if (cliente != null)
                {
                    // Quando precisar puxar o Limite de Crédito ou Débito, a lógica entra aqui
                    // Exemplo: txtLimiteCredito.Text = "Limite: R$ " + cliente.CreditoDisponivel;
                    chkGerarCarne.IsEnabled = true;
                }
            }
            else
            {
                chkGerarCarne.IsEnabled = false;
                txtLimiteCredito.Text = "Limite: R$ 0,00";
                txtDebitoCliente.Text = "0,00";
            }
        }

        // ===============================================================
        // EVENTOS DE TECLADO (ATALHOS F2, F4, ETC)
        // ===============================================================
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2: btnNovaVenda_Click(null, null); break;
                case Key.F4: btnFinalizar_Click(null, null); break;
                case Key.F5: btnAlterarVenda_Click(null, null); break;
                case Key.F6: btnImprimirNota_Click(null, null); break;
                case Key.F7: btnImprimir80mm_Click(null, null); break;
                case Key.F11: btnExcluirVenda_Click(null, null); break;
            }
        }

        // ===============================================================
        // EVENTOS DO LEITOR DE CÓDIGO DE BARRAS / BUSCA PRODUTO
        // ===============================================================
        private void txtCodigoBarra_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                string codigoDigitado = txtCodigoBarra.Text.Trim();
                if (string.IsNullOrEmpty(codigoDigitado)) return;

                // Consulta centralizada blindada
                var produto = BancoDeDados.BuscarProduto(codigoDigitado);

                if (produto != null)
                {
                    txtNomeProduto.Text = produto.Descricao;
                    txtPrecoUnitario.Text = produto.Preco.ToString("N2");

                    // TODO: Lógica para inserir o produto no carrinho
                    MessageBox.Show($"Produto {produto.Descricao} lido com sucesso!", "Leitor / Busca");
                }
                else
                {
                    MessageBox.Show("Produto não encontrado no sistema!", "Não Encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                txtCodigoBarra.Clear();
                txtQtd.Text = "1,00";
                txtCodigoBarra.Focus();
            }
        }

        private void AtualizarTotaisDaVenda()
        {
            decimal totalLiquido = carrinhoDeProdutos.Sum(i => i.TotalLiquido);
            decimal totalBruto = carrinhoDeProdutos.Sum(i => i.TotalBruto);
            int totalItens = carrinhoDeProdutos.Count;

            txtTotalLiquido.Text = totalLiquido.ToString("C");
            txtTotalBruto.Text = totalBruto.ToString("N2");
            txtTotalItens.Text = totalItens.ToString();

            btnFinalizar.IsEnabled = carrinhoDeProdutos.Count > 0;
        }

        // ===============================================================
        // EVENTOS DE CÁLCULO DE TROCO E CHECKBOXES
        // ===============================================================
        private void CalcularTroco_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtTotalLiquido == null || txtValorPago1 == null || txtTroco == null) return;

            string valorVendaStr = txtTotalLiquido.Text.Replace("R$", "").Trim();
            string valorPagoStr = txtValorPago1.Text.Replace("R$", "").Trim();

            if (decimal.TryParse(valorVendaStr, out decimal totalVenda) &&
                decimal.TryParse(valorPagoStr, out decimal valorPago))
            {
                decimal troco = valorPago - totalVenda;
                if (troco > 0)
                {
                    txtTroco.Text = troco.ToString("N2");
                    txtTroco.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    txtTroco.Text = "0,00";
                    txtTroco.Foreground = System.Windows.Media.Brushes.Black;
                }
            }
        }

        private void chkTipoVenda_Click(object sender, RoutedEventArgs e)
        {
            if (sender == chkVenda && chkVenda.IsChecked == true) chkOrcamento.IsChecked = false;
            else if (sender == chkOrcamento && chkOrcamento.IsChecked == true) chkVenda.IsChecked = false;
        }

        private void chkGerarCarne_Click(object sender, RoutedEventArgs e)
        {
            if (chkGerarCarne.IsChecked == true && cbCliente.SelectedValue == null)
            {
                MessageBox.Show("Selecione um cliente para gerar um carnê (Venda a Prazo).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                chkGerarCarne.IsChecked = false;
            }
        }

        // ===============================================================
        // EVENTOS DE BOTÕES DE TOPO / JANELA
        // ===============================================================
        private void txtFechar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void lblAbrirCliente_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Abrir tela de Cadastro de Cliente...");
        }

        // ===============================================================
        // EVENTOS DOS BOTÕES DE AÇÃO DO MENU (F2, F4, ETC)
        // ===============================================================
        private void btnNovaVenda_Click(object sender, RoutedEventArgs e)
        {
            carrinhoDeProdutos.Clear();
            AtualizarTotaisDaVenda();
            txtNrVenda.Text = "A Gerar...";
            cbCliente.SelectedIndex = -1;
            txtValorPago1.Text = "0,00";
            txtTroco.Text = "0,00";
            txtCodigoBarra.Focus();
        }

        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            if (carrinhoDeProdutos.Count == 0) return;
            MessageBox.Show("Venda Finalizada com Sucesso!", "Finalizar Venda", MessageBoxButton.OK, MessageBoxImage.Information);
            btnNovaVenda_Click(null, null);
        }

        private void btnAlterarVenda_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Tela de Alteração..."); }
        private void btnExcluirVenda_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Tela de Exclusão..."); }
        private void btnLocalizarVenda_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Tela de Pesquisa..."); }
        private void btnImprimirNota_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Imprimindo A4 / NF-e..."); }
        private void btnImprimir80mm_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Imprimindo Recibo 80mm..."); }
    }
}