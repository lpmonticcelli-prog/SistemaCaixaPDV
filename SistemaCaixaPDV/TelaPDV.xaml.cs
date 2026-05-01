using System;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Data;

namespace SistemaCaixaPDV
{
    public partial class TelaPDV : Window
    {
        // Inicialização estrita para evitar CS8618
        public ObservableCollection<ItemCarrinho> carrinhoDeProdutos { get; set; } = new ObservableCollection<ItemCarrinho>();
        private readonly string ConnectionString = "Data Source=banco_pdv.sqlite;";

        private string _observacoesVenda = string.Empty;
        private string _enderecoEntrega = string.Empty;

        public TelaPDV()
        {
            InitializeComponent();
            gridCarrinho.ItemsSource = carrinhoDeProdutos;

            cbForma1.Items.Add("Dinheiro");
            cbForma1.Items.Add("PIX");
            cbForma1.Items.Add("Cartão de Crédito");
            cbForma1.Items.Add("Cartão de Débito");
            cbForma1.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDataVenda.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            CarregarClientes();
            txtCodigoBarra.Focus();
        }

        // ===============================================================
        // MÓDULO: CLIENTES E CRÉDITO
        // ===============================================================
        private void CarregarClientes()
        {
            var clientes = BancoDeDados.ListarClientes();
            cbCliente.ItemsSource = clientes;
        }

        private void btnAtualizarClientes_Click(object sender, RoutedEventArgs e)
        {
            CarregarClientes();
            txtCodigoBarra.Focus();
        }

        private void cbCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCliente.SelectedValue != null)
            {
                int idCliente = Convert.ToInt32(cbCliente.SelectedValue);
                var cliente = BancoDeDados.ObterClientePorId(idCliente);

                if (cliente != null)
                {
                    chkGerarCarne.IsEnabled = true;
                    txtLimiteCredito.Text = $"Limite: {cliente.CreditoLiberado:C}";
                    BancoDeDados.FiltrarContasReceber(idCliente, "Pendente", out decimal debito);
                    txtDebitoCliente.Text = debito.ToString("N2");
                }
            }
            else
            {
                chkGerarCarne.IsEnabled = false;
                chkGerarCarne.IsChecked = false;
                txtLimiteCredito.Text = "Limite: R$ 0,00";
                txtDebitoCliente.Text = "0,00";
            }
            txtCodigoBarra.Focus();
        }

        // ===============================================================
        // MÓDULO: MOTOR DE CÓDIGO DE BARRAS E BUSCA (F3)
        // ===============================================================
        private void txtCodigoBarra_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                ProcessarCodigoBarra(txtCodigoBarra.Text?.Trim() ?? "");
            }
        }

        private void ProcessarCodigoBarra(string codigoDigitado)
        {
            if (string.IsNullOrEmpty(codigoDigitado)) return;

            if (!decimal.TryParse(txtQtd.Text, out decimal quantidade) || quantidade <= 0)
                quantidade = 1;

            var produto = BancoDeDados.BuscarProduto(codigoDigitado);

            if (produto != null)
            {
                var itemExistente = carrinhoDeProdutos.FirstOrDefault(i => i.Codigo == produto.Codigo);

                if (itemExistente != null)
                {
                    itemExistente.Quantidade += quantidade;
                    gridCarrinho.Items.Refresh();
                }
                else
                {
                    carrinhoDeProdutos.Add(new ItemCarrinho
                    {
                        Codigo = produto.Codigo ?? "",
                        Descricao = string.IsNullOrWhiteSpace(produto.Descricao) ? "PRODUTO DIVERSO" : produto.Descricao,
                        Unidade = "UN",
                        Quantidade = quantidade,
                        PrecoUnitario = produto.Preco
                    });
                }

                txtNomeProduto.Text = produto.Descricao;
                txtPrecoUnitario.Text = produto.Preco.ToString("N2");

                AtualizarTotaisDaVenda();
            }
            else
            {
                MessageBox.Show("Produto não encontrado no banco de dados!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            txtCodigoBarra.Clear();
            txtQtd.Text = "1,00";
            txtCodigoBarra.Focus();
        }

        private void AbrirBuscaProduto()
        {
            Window busca = new Window()
            {
                Title = "Localizar Produto (F3)",
                Width = 700,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.WhiteSmoke
            };

            Grid grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            StackPanel panelTop = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            TextBlock lblBusca = new TextBlock { Text = "Digite o nome ou código:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 10, 0) };
            TextBox txtBusca = new TextBox { Width = 480, FontSize = 16, Padding = new Thickness(5) };

            panelTop.Children.Add(lblBusca);
            panelTop.Children.Add(txtBusca);
            grid.Children.Add(panelTop);
            Grid.SetRow(panelTop, 0);

            DataGrid dgResultados = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                FontSize = 14,
                RowHeight = 30,
                Background = Brushes.White,
                AlternatingRowBackground = Brushes.AliceBlue
            };

            dgResultados.Columns.Add(new DataGridTextColumn { Header = "Código", Binding = new Binding("Codigo"), Width = new DataGridLength(120) });
            dgResultados.Columns.Add(new DataGridTextColumn { Header = "Descrição", Binding = new Binding("Descricao"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            dgResultados.Columns.Add(new DataGridTextColumn { Header = "Preço", Binding = new Binding("Preco") { StringFormat = "C" }, Width = new DataGridLength(120) });

            grid.Children.Add(dgResultados);
            Grid.SetRow(dgResultados, 1);

            txtBusca.TextChanged += (s, ev) =>
            {
                if (txtBusca.Text.Length >= 2)
                    dgResultados.ItemsSource = BancoDeDados.BuscarProdutosPorNome(txtBusca.Text.Trim());
                else
                    dgResultados.ItemsSource = null;
            };

            txtBusca.PreviewKeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Down && dgResultados.Items.Count > 0)
                {
                    dgResultados.Focus();
                    dgResultados.SelectedIndex = 0;
                    ev.Handled = true;
                }
            };

            dgResultados.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter)
                {
                    if (dgResultados.SelectedItem is Produto p)
                    {
                        txtCodigoBarra.Text = p.Codigo;
                        busca.DialogResult = true;
                        busca.Close();
                    }
                    ev.Handled = true;
                }
            };

            dgResultados.MouseDoubleClick += (s, ev) =>
            {
                if (dgResultados.SelectedItem is Produto p)
                {
                    txtCodigoBarra.Text = p.Codigo;
                    busca.DialogResult = true;
                    busca.Close();
                }
            };

            busca.Content = grid;
            busca.Loaded += (s, ev) => txtBusca.Focus();

            if (busca.ShowDialog() == true && !string.IsNullOrEmpty(txtCodigoBarra.Text))
            {
                ProcessarCodigoBarra(txtCodigoBarra.Text);
            }
        }

        private void AtualizarTotaisDaVenda()
        {
            if (txtTotalLiquido == null || txtTotalBruto == null) return;

            decimal totalBruto = carrinhoDeProdutos.Sum(i => i.TotalBruto);

            decimal desconto = 0;
            if (decimal.TryParse(txtTotalDesconto.Text?.Replace("R$", "").Trim(), out decimal desc))
                desconto = desc;

            decimal totalLiquido = totalBruto - desconto;

            txtTotalLiquido.Text = totalLiquido.ToString("C");
            txtTotalBruto.Text = totalBruto.ToString("N2");
            txtTotalItens.Text = carrinhoDeProdutos.Count.ToString();

            btnFinalizar.IsEnabled = carrinhoDeProdutos.Count > 0;

            // Chama a lógica isolada em vez de forçar o evento passando null
            CalcularTrocoLogica();
        }

        private void CalcularTroco_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularTrocoLogica();
        }

        private void CalcularTrocoLogica()
        {
            if (txtTotalLiquido == null || txtValorPago1 == null || txtTroco == null || txtTotalPago == null) return;

            string valorVendaStr = (txtTotalLiquido.Text ?? "").Replace("R$", "").Trim();
            string valorPagoStr = (txtValorPago1.Text ?? "").Replace("R$", "").Trim();

            if (decimal.TryParse(valorVendaStr, out decimal totalVenda) &&
                decimal.TryParse(valorPagoStr, out decimal valorPago))
            {
                txtTotalPago.Text = valorPago.ToString("N2");
                decimal troco = valorPago - totalVenda;

                if (troco > 0)
                {
                    txtTroco.Text = troco.ToString("N2");
                    txtTroco.Foreground = Brushes.DarkGreen;
                }
                else
                {
                    txtTroco.Text = "0,00";
                    txtTroco.Foreground = Brushes.Black;
                }
            }
        }

        // ===============================================================
        // MÓDULO: TOP BAR E AÇÕES DO SISTEMA (ISOLADAS)
        // ===============================================================

        private void btnEndEntrega_Click(object sender, RoutedEventArgs e)
        {
            _enderecoEntrega = SolicitarInput("Endereço de Entrega:", _enderecoEntrega);
            txtCodigoBarra.Focus();
        }

        private void lblAbrirCliente_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try { new TelaClientes().ShowDialog(); CarregarClientes(); }
            catch { MessageBox.Show("Módulo TelaClientes indisponível.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private void lblObservacoes_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _observacoesVenda = SolicitarInput("Observações da Venda:", _observacoesVenda);
            txtCodigoBarra.Focus();
        }

        private void lblCapaCarne_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (cbCliente.SelectedValue == null)
            {
                MessageBox.Show("Selecione um cliente para imprimir o carnê.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ImprimirDocumentoPDV("CARNE");
        }

        private void lblConfiguracao_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try { new TelaConfiguracoes().ShowDialog(); }
            catch { MessageBox.Show("Módulo de Configuração Indisponível.", "Aviso"); }
        }

        private void lblAjuda_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("1. Bipe o produto para inserir.\n2. Para alterar Qtde, digite o número no campo Qtd antes de bipar.\n3. Pressione F4 para finalizar a venda.\n4. Para excluir (F11), é necessário autorização/senha.", "Ajuda PDV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void txtFechar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        // ===============================================================
        // MÓDULO: FECHAMENTO FINANCEIRO E TRANSAÇÕES (ACID)
        // ===============================================================

        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            EfetivarVenda();
        }

        private void EfetivarVenda()
        {
            if (carrinhoDeProdutos.Count == 0) return;

            string formaPagamento = cbForma1.Text ?? "Dinheiro";

            decimal.TryParse(txtTotalLiquido.Text?.Replace("R$", "").Trim(), out decimal totalLiquido);
            decimal.TryParse(txtTotalBruto.Text?.Replace("R$", "").Trim(), out decimal totalBruto);
            decimal.TryParse(txtTotalDesconto.Text?.Replace("R$", "").Trim(), out decimal desconto);

            string clienteNome = cbCliente.Text ?? "Consumidor Final";
            int clienteId = cbCliente.SelectedValue != null ? Convert.ToInt32(cbCliente.SelectedValue) : 0;
            string tipoVenda = chkOrcamento.IsChecked == true ? "Orçamento" : "Venda";

            if (chkGerarCarne.IsChecked == true && clienteId == 0)
            {
                MessageBox.Show("Para vendas a prazo (Carnê), é obrigatório selecionar um cliente cadastrado.", "Bloqueio", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbCliente.Focus();
                return;
            }

            try
            {
                long vendaId = 0;
                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    try { new SqliteCommand("ALTER TABLE Vendas ADD COLUMN Observacoes TEXT", conn).ExecuteNonQuery(); } catch { }

                    using (var transaction = conn.BeginTransaction())
                    {
                        string sqlVenda = @"INSERT INTO Vendas (TotalLiquido, FormaPagamento, DataHora, DataVenda, ClienteId, ClienteNome, Tipo, TotalBruto, TotalDesconto, Observacoes) 
                                            VALUES (@TotLiq, @Forma, @DataHora, @DataVenda, @CliId, @CliNome, @Tipo, @TotBru, @Desc, @Obs);
                                            SELECT last_insert_rowid();";

                        using (var cmd = new SqliteCommand(sqlVenda, conn))
                        {
                            cmd.Parameters.AddWithValue("@TotLiq", totalLiquido);
                            cmd.Parameters.AddWithValue("@Forma", formaPagamento);
                            cmd.Parameters.AddWithValue("@DataHora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@DataVenda", DateTime.Now.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@CliId", clienteId);
                            cmd.Parameters.AddWithValue("@CliNome", clienteNome);
                            cmd.Parameters.AddWithValue("@Tipo", tipoVenda);
                            cmd.Parameters.AddWithValue("@TotBru", totalBruto);
                            cmd.Parameters.AddWithValue("@Desc", desconto);
                            cmd.Parameters.AddWithValue("@Obs", _observacoesVenda + " " + _enderecoEntrega);
                            vendaId = (long)(cmd.ExecuteScalar() ?? 0);
                        }

                        foreach (var item in carrinhoDeProdutos)
                        {
                            string sqlItem = "INSERT INTO ItensVenda (VendaId, CodigoProduto, Descricao, Quantidade, PrecoUnitario, Total) VALUES (@VendaId, @Cod, @Desc, @Qtd, @Prc, @Tot)";
                            using (var cmdItem = new SqliteCommand(sqlItem, conn))
                            {
                                cmdItem.Parameters.AddWithValue("@VendaId", vendaId);
                                cmdItem.Parameters.AddWithValue("@Cod", item.Codigo);
                                cmdItem.Parameters.AddWithValue("@Desc", item.Descricao);
                                cmdItem.Parameters.AddWithValue("@Qtd", item.Quantidade);
                                cmdItem.Parameters.AddWithValue("@Prc", item.PrecoUnitario);
                                cmdItem.Parameters.AddWithValue("@Tot", item.TotalLiquido);
                                cmdItem.ExecuteNonQuery();
                            }

                            if (tipoVenda == "Venda")
                            {
                                string sqlEstoque = "UPDATE Produtos SET EstoqueAtual = EstoqueAtual - @Qtd WHERE Codigo = @Cod";
                                using (var cmdEstoque = new SqliteCommand(sqlEstoque, conn))
                                {
                                    cmdEstoque.Parameters.AddWithValue("@Qtd", item.Quantidade);
                                    cmdEstoque.Parameters.AddWithValue("@Cod", item.Codigo);
                                    cmdEstoque.ExecuteNonQuery();
                                }
                            }
                        }

                        if (tipoVenda == "Venda" && chkGerarCarne.IsChecked == true && clienteId > 0)
                        {
                            string sqlCarne = @"INSERT INTO ContasReceber (ClienteId, ClienteNome, Descricao, Valor, DataVencimento, Status, TipoDocumento) 
                                                VALUES (@CliId, @CliNome, @Desc, @Val, @Venc, 'Pendente', 'Carnê')";
                            using (var cmdCarne = new SqliteCommand(sqlCarne, conn))
                            {
                                cmdCarne.Parameters.AddWithValue("@CliId", clienteId);
                                cmdCarne.Parameters.AddWithValue("@CliNome", clienteNome);
                                cmdCarne.Parameters.AddWithValue("@Desc", $"Venda Nº {vendaId}");
                                cmdCarne.Parameters.AddWithValue("@Val", totalLiquido);
                                cmdCarne.Parameters.AddWithValue("@Venc", DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"));
                                cmdCarne.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                }

                txtNrVenda.Text = vendaId.ToString("D6");
                if (MessageBox.Show($"Venda {vendaId:D6} finalizada! Deseja imprimir o cupom?", "Sucesso", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    ImprimirDocumentoPDV("CUPOM");
                }

                IniciarNovaVenda();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha crítica na transação! Motivo: {ex.Message}", "Erro de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===============================================================
        // MÓDULO: ESTORNO, ALTERAÇÃO E BUSCA DE VENDAS
        // ===============================================================
        private void btnAlterarVenda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Para garantir o compliance contábil, utilize a função EXCLUIR VENDA para estornar e re-lance a venda corrigida.", "Bloqueio Operacional", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void btnExcluirVenda_Click(object sender, RoutedEventArgs e)
        {
            ExecutarEstornoVenda();
        }

        private void ExecutarEstornoVenda()
        {
            string idStr = SolicitarInput("Digite o Nº da Venda que deseja excluir/estornar:", "");
            if (long.TryParse(idStr, out long idVenda))
            {
                if (MessageBox.Show($"Atenção! Isso apagará a Venda Nº {idVenda} e devolverá os itens ao estoque. Confirma?", "Zona Crítica", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = new SqliteConnection(ConnectionString))
                        {
                            conn.Open();
                            using (var transaction = conn.BeginTransaction())
                            {
                                string tipoVenda = "";
                                using (var cmdChk = new SqliteCommand("SELECT Tipo FROM Vendas WHERE Id = @id", conn))
                                {
                                    cmdChk.Parameters.AddWithValue("@id", idVenda);
                                    var obj = cmdChk.ExecuteScalar();
                                    if (obj == null) { MessageBox.Show("Venda não encontrada.", "Aviso"); return; }
                                    tipoVenda = obj.ToString() ?? "";
                                }

                                if (tipoVenda == "Venda")
                                {
                                    using (var cmdItens = new SqliteCommand("SELECT CodigoProduto, Quantidade FROM ItensVenda WHERE VendaId = @id", conn))
                                    {
                                        cmdItens.Parameters.AddWithValue("@id", idVenda);
                                        using (var r = cmdItens.ExecuteReader())
                                        {
                                            while (r.Read())
                                            {
                                                using (var cmdEst = new SqliteCommand("UPDATE Produtos SET EstoqueAtual = EstoqueAtual + @q WHERE Codigo = @c", conn))
                                                {
                                                    cmdEst.Parameters.AddWithValue("@q", Convert.ToDecimal(r["Quantidade"]));
                                                    cmdEst.Parameters.AddWithValue("@c", r["CodigoProduto"].ToString());
                                                    cmdEst.ExecuteNonQuery();
                                                }
                                            }
                                        }
                                    }
                                }

                                new SqliteCommand($"DELETE FROM ItensVenda WHERE VendaId = {idVenda}", conn).ExecuteNonQuery();
                                new SqliteCommand($"DELETE FROM ContasReceber WHERE Descricao LIKE '%Venda Nº {idVenda}%'", conn).ExecuteNonQuery();
                                new SqliteCommand($"DELETE FROM Vendas WHERE Id = {idVenda}", conn).ExecuteNonQuery();

                                transaction.Commit();
                                MessageBox.Show($"Estorno da Venda {idVenda} realizado com sucesso. Estoque atualizado.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex) { MessageBox.Show($"Erro ao estornar: {ex.Message}", "Erro Crítico"); }
                }
            }
        }

        private void btnLocalizarVenda_Click(object sender, RoutedEventArgs e)
        {
            string idStr = SolicitarInput("Digite o Nº da Venda para reimprimir:", "");
            if (!string.IsNullOrEmpty(idStr))
            {
                txtNrVenda.Text = idStr;
                ImprimirDocumentoPDV("RECIBO_ANTIGO");
            }
        }

        // ===============================================================
        // MÓDULO DE IMPRESSÃO (NATIVO WPF)
        // ===============================================================
        private void btnImprimirNota_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Módulo de NF-e e NFC-e Sefaz acionado (Disparar ACBr/Webservice).", "Fiscal", MessageBoxButton.OK, MessageBoxImage.Information); }
        private void btnImprimir80mm_Click(object sender, RoutedEventArgs e) { ImprimirDocumentoPDV("CUPOM"); }

        private void ImprimirDocumentoPDV(string tipo)
        {
            PrintDialog printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument();
                doc.FontFamily = new FontFamily("Consolas");
                doc.PagePadding = new Thickness(5);
                doc.PageWidth = 280;

                if (tipo == "CARNE")
                {
                    doc.Blocks.Add(new Paragraph(new Run("CAPA DE CARNÊ - PAGAMENTO A PRAZO")) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                    doc.Blocks.Add(new Paragraph(new Run("------------------------------------------")));
                    doc.Blocks.Add(new Paragraph(new Run($"Cliente: {cbCliente.Text}\nEmissão: {DateTime.Now:dd/MM/yyyy}\nValor Total: {txtTotalLiquido.Text}")));
                    doc.Blocks.Add(new Paragraph(new Run("ASSINATURA DO CLIENTE:\n\n__________________________________________")) { Margin = new Thickness(0, 50, 0, 0) });
                }
                else
                {
                    doc.Blocks.Add(new Paragraph(new Run("CUPOM NÃO FISCAL")) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                    doc.Blocks.Add(new Paragraph(new Run($"Venda Nº: {txtNrVenda.Text}\nData: {DateTime.Now:dd/MM/yyyy HH:mm}\nCliente: {cbCliente.Text}\n------------------------------------------")));

                    Paragraph pItens = new Paragraph();
                    pItens.FontSize = 10;
                    foreach (var item in carrinhoDeProdutos)
                    {
                        pItens.Inlines.Add(new Run($"{item.Quantidade}x {item.Descricao}\n  {item.PrecoUnitario:C} -> {item.TotalLiquido:C}\n"));
                    }
                    doc.Blocks.Add(pItens);
                    doc.Blocks.Add(new Paragraph(new Run("------------------------------------------")));
                    doc.Blocks.Add(new Paragraph(new Run($"TOTAL: {txtTotalLiquido.Text}")) { FontSize = 16, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
                    if (!string.IsNullOrEmpty(_observacoesVenda)) doc.Blocks.Add(new Paragraph(new Run($"Obs: {_observacoesVenda}")));
                }

                IDocumentPaginatorSource idpSource = doc;
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Impressão PDV");
            }
        }

        // ===============================================================
        // UTILITÁRIOS GLOBAIS E INICIALIZAÇÃO
        // ===============================================================
        private void btnNovaVenda_Click(object sender, RoutedEventArgs e)
        {
            IniciarNovaVenda();
        }

        private void IniciarNovaVenda()
        {
            carrinhoDeProdutos.Clear();
            AtualizarTotaisDaVenda();

            txtNrVenda.Text = "";
            cbCliente.SelectedIndex = -1;
            chkOrcamento.IsChecked = false;
            chkVenda.IsChecked = true;
            chkGerarCarne.IsChecked = false;
            _observacoesVenda = "";
            _enderecoEntrega = "";

            txtNomeProduto.Clear();
            txtPrecoUnitario.Clear();
            txtValorPago1.Text = "R$ 0,00";
            txtTroco.Text = "0,00";
            txtQtd.Text = "1,00";

            txtCodigoBarra.Focus();
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
                MessageBox.Show("Selecione um cliente para autorizar venda a prazo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                chkGerarCarne.IsChecked = false;
                cbCliente.Focus();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape: this.Close(); break;
                case Key.F2: IniciarNovaVenda(); break;
                case Key.F3: AbrirBuscaProduto(); break;
                case Key.F4: if (btnFinalizar.IsEnabled) EfetivarVenda(); break;
                case Key.F5: btnAlterarVenda_Click(sender, e); break;
                case Key.F6: btnImprimirNota_Click(sender, e); break;
                case Key.F7: btnImprimir80mm_Click(sender, e); break;
                case Key.F11: ExecutarEstornoVenda(); break;
            }
        }

        private string SolicitarInput(string promptText, string defaultText)
        {
            Window prompt = new Window()
            {
                Width = 400,
                Height = 160,
                Title = "Sistema Caixa PDV",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.WhiteSmoke
            };

            StackPanel stack = new StackPanel() { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock() { Text = promptText, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });

            TextBox txtInput = new TextBox() { FontSize = 14, Padding = new Thickness(5), Text = defaultText };
            stack.Children.Add(txtInput);

            Button btnOk = new Button() { Content = "OK", Margin = new Thickness(0, 15, 0, 0), Height = 30, IsDefault = true };
            btnOk.Click += (s, ev) => { prompt.DialogResult = true; prompt.Close(); };
            stack.Children.Add(btnOk);

            prompt.Content = stack;
            return prompt.ShowDialog() == true ? txtInput.Text : defaultText;
        }
    }
}