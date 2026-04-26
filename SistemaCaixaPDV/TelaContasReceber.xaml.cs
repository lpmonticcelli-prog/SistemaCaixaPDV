using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaCaixaPDV
{
    public partial class TelaContasReceber : Window
    {
        private int idClienteFiltro = 0;
        private ContasReceberModel contaEmFoco = null;
        private int idContaEditando = 0;

        public TelaContasReceber()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarClientesCombo();
            dpVencimento.SelectedDate = DateTime.Now;
            CarregarDadosGrid();
        }

        private void CarregarClientesCombo()
        {
            // Utiliza o método já existente na camada de banco de dados
            cbClienteForm.ItemsSource = BancoDeDados.ListarClientes();
        }

        // ==========================================
        // SALVAR OU EDITAR (FORMULÁRIO DO TOPO)
        // ==========================================
        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Por favor, digite a descrição do recebimento.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescricao.Focus();
                return;
            }

            string cliNome = cbClienteForm.Text.Trim();
            int cliId = 0;
            if (cbClienteForm.SelectedValue != null) int.TryParse(cbClienteForm.SelectedValue.ToString(), out cliId);

            string desc = txtDescricao.Text.Trim();
            string tipoDoc = ((ComboBoxItem)cbTipoDoc.SelectedItem).Content.ToString();
            string dataVenc = dpVencimento.SelectedDate.Value.ToString("yyyy-MM-dd");

            decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(), out decimal valorDec);

            // Chama a camada do banco de dados centralizado
            BancoDeDados.SalvarContaReceber(idContaEditando, cliId, cliNome, desc, tipoDoc, valorDec, dataVenc);

            MessageBox.Show(idContaEditando == 0 ? "Recebimento lançado com sucesso!" : "Recebimento atualizado!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

            ResetarFormulario();
            CarregarDadosGrid();
        }

        // ==========================================
        // GRID: CARREGAR, EDITAR E EXCLUIR
        // ==========================================
        private void CarregarDadosGrid()
        {
            if (!this.IsLoaded) return;

            string statusFiltro = cbFiltroStatus.SelectedItem != null ? ((ComboBoxItem)cbFiltroStatus.SelectedItem).Content.ToString() : "Pendente";

            decimal totalPendente;
            gridContas.ItemsSource = BancoDeDados.FiltrarContasReceber(idClienteFiltro, statusFiltro, out totalPendente);
            txtTotalPendente.Text = $"Total Pendente: {totalPendente:C}";
        }

        private void btnEditarLinha_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int idSelecionado = Convert.ToInt32(button.Tag);
                var conta = BancoDeDados.ObterContaReceberPorId(idSelecionado);

                if (conta != null)
                {
                    idContaEditando = conta.Id;
                    cbClienteForm.Text = conta.ClienteNome;
                    txtDescricao.Text = conta.Descricao;
                    txtValor.Text = conta.Valor.ToString("N2");

                    if (DateTime.TryParse(conta.DataVencimentoFormatada, out DateTime dt))
                        dpVencimento.SelectedDate = dt;

                    foreach (ComboBoxItem item in cbTipoDoc.Items)
                    {
                        if (item.Content.ToString() == conta.TipoDocumento) { cbTipoDoc.SelectedItem = item; break; }
                    }

                    txtTituloFormulario.Text = "✏️ EDITANDO RECEBIMENTO SELECIONADO";
                    txtTituloFormulario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    btnSalvar.Content = "💾 Atualizar";
                    btnSalvar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    btnCancelar.Visibility = Visibility.Visible;
                    txtDescricao.Focus();
                }
            }
        }

        private void btnExcluirLinha_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int idParaExcluir = Convert.ToInt32(button.Tag);
                if (MessageBox.Show("Deseja realmente apagar este registro do histórico?", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    BancoDeDados.ExcluirContaReceber(idParaExcluir);

                    if (idContaEditando == idParaExcluir) ResetarFormulario();
                    CarregarDadosGrid();
                }
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ResetarFormulario();
        }

        private void ResetarFormulario()
        {
            idContaEditando = 0;
            txtDescricao.Clear();
            txtValor.Text = "0,00";
            cbClienteForm.Text = "";
            dpVencimento.SelectedDate = DateTime.Now;
            cbTipoDoc.SelectedIndex = 0;

            txtTituloFormulario.Text = "📝 LANÇAR NOVA CONTA A RECEBER";
            txtTituloFormulario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
            btnSalvar.Content = "💾 Gravar";
            btnSalvar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            btnCancelar.Visibility = Visibility.Collapsed;
        }

        // ==========================================
        // FILTROS E BAIXA DE PAGAMENTO
        // ==========================================
        private void btnBuscarCliente_Click(object sender, RoutedEventArgs e)
        {
            TelaBuscaCliente busca = new TelaBuscaCliente();
            if (busca.ShowDialog() == true && busca.ClienteSelecionado != null)
            {
                idClienteFiltro = busca.ClienteSelecionado.Id;
                lblClienteSelecionado.Text = busca.ClienteSelecionado.Nome;
                CarregarDadosGrid();
            }
        }

        private void btnLimparFiltro_Click(object sender, RoutedEventArgs e)
        {
            idClienteFiltro = 0;
            lblClienteSelecionado.Text = "Todos os Clientes";
            CarregarDadosGrid();
        }

        private void cbFiltroStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CarregarDadosGrid();
        }

        private void gridContas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            contaEmFoco = gridContas.SelectedItem as ContasReceberModel;

            if (contaEmFoco != null && contaEmFoco.Status == "Pendente")
            {
                panelBaixa.Visibility = Visibility.Visible;
                lblSelecione.Visibility = Visibility.Collapsed;
                txtValorRecebido.Text = contaEmFoco.Valor.ToString("N2");
            }
            else
            {
                panelBaixa.Visibility = Visibility.Collapsed;
                lblSelecione.Visibility = Visibility.Visible;
            }
        }

        private void btnFinalizarPagto_Click(object sender, RoutedEventArgs e)
        {
            if (contaEmFoco == null) return;

            if (MessageBox.Show($"Confirmar o recebimento de {txtValorRecebido.Text} referente a esta fatura?", "Receber", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string forma = ((ComboBoxItem)cbFormaBaixa.SelectedItem).Content.ToString();
                string hoje = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                BancoDeDados.BaixarContaReceber(contaEmFoco.Id, forma, hoje);

                MessageBox.Show("Pagamento registrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                CarregarDadosGrid();
                panelBaixa.Visibility = Visibility.Collapsed;
            }
        }
    }
}