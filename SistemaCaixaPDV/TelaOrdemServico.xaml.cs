using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Diagnostics;

namespace SistemaCaixaPDV
{
    public partial class TelaOrdemServico : Window
    {
        public ObservableCollection<ItemOrdemServicoGrid> ItensDaOS { get; set; }
        private readonly string ConnectionString = "Data Source=banco_pdv.sqlite;";

        public TelaOrdemServico()
        {
            ItensDaOS = new ObservableCollection<ItemOrdemServicoGrid>();
            InitializeComponent();
            gridItensOS.ItemsSource = ItensDaOS;
            ResetarTela();
            CarregarClientesCombo();
        }

        private void CarregarClientesCombo()
        {
            try
            {
                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand("SELECT Nome FROM Clientes ORDER BY Nome", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        cbCliente.Items.Clear();
                        while (reader.Read())
                        {
                            cbCliente.Items.Add(reader["Nome"].ToString());
                        }
                    }
                }
            }
            catch { /* Ignora se a tabela não existir ainda na inicialização */ }
        }

        private void ResetarTela()
        {
            txtNumOS.Text = "0";
            cbCliente.Text = "";
            cbResponsavel.SelectedIndex = -1;
            cbStatus.SelectedIndex = 0;

            dpDataEntrada.SelectedDate = DateTime.Now;
            dpDataSaida.SelectedDate = DateTime.Now.AddDays(3);

            txtProblema.Clear();
            txtLaudo.Clear();
            txtObservacoes.Clear();
            txtObsInterna.Clear();

            txtImei.Clear();
            txtEquipamento.Clear();
            txtMarcaModelo.Clear();

            chkCarregador.IsChecked = false;
            chkCaboUsb.IsChecked = false;
            chkCapaPelicula.IsChecked = false;
            chkBateria.IsChecked = false;
            chkCaixa.IsChecked = false;

            chkEquipamentoLiga.IsChecked = false;
            chkTelaIntacta.IsChecked = false;
            chkConectorCarga.IsChecked = false;
            chkSenhasFornecidas.IsChecked = false;
            chkGarantiaVigente.IsChecked = false;

            ItensDaOS.Clear();
            txtDesconto.Text = "0,00";
            CalcularTotais();

            txtBuscaProdutoOS.Focus();
        }

        private void CalcularTotais()
        {
            if (ItensDaOS == null || txtDesconto == null || txtTotalBruto == null || txtTotalLiquido == null || txtTotalItens == null || txtTotalPago == null || txtTroco == null) return;

            decimal totalBruto = ItensDaOS.Sum(i => i.Subtotal);

            if (!decimal.TryParse(txtDesconto.Text.Replace("R$", "").Trim(), out decimal desconto))
                desconto = 0;

            decimal totalLiquido = totalBruto - desconto;
            if (totalLiquido < 0) totalLiquido = 0;

            txtTotalBruto.Text = totalBruto.ToString("C");
            txtTotalLiquido.Text = totalLiquido.ToString("C");
            txtTotalItens.Text = ItensDaOS.Count.ToString();

            txtTotalPago.Text = "R$ 0,00";
            txtTroco.Text = "R$ 0,00";
        }

        private void gridItensOS_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => CalcularTotais()), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void txtDesconto_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularTotais();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape: this.Close(); break;
                case Key.F2: btnNovaOS_Click(sender, e); break;
                case Key.F4: btnSalvarOS_Click(sender, e); break;
                case Key.F6: btnImprimirA4_Click(sender, e); break;
                case Key.F9: btnLocOS_Click(sender, e); break;
            }
        }

        private void txtBuscaProdutoOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string termo = txtBuscaProdutoOS.Text.Trim();
                if (string.IsNullOrEmpty(termo)) return;

                BuscarProduto(termo);
                txtBuscaProdutoOS.Clear();
                txtBuscaProdutoOS.Focus();
            }
        }

        private void BuscarProduto(string termo)
        {
            try
            {
                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT Codigo, Descricao, Unidade, Preco as ValorVenda 
                                     FROM Produtos 
                                     WHERE Codigo = @Termo OR Descricao LIKE @TermoLike 
                                     LIMIT 1";

                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Termo", termo);
                        cmd.Parameters.AddWithValue("@TermoLike", $"%{termo}%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var item = new ItemOrdemServicoGrid
                                {
                                    Codigo = reader["Codigo"].ToString() ?? string.Empty,
                                    Descricao = reader["Descricao"].ToString() ?? string.Empty,
                                    Unidade = reader["Unidade"].ToString() ?? string.Empty,
                                    Quantidade = 1,
                                    ValorUnitario = reader["ValorVenda"] != DBNull.Value ? Convert.ToDecimal(reader["ValorVenda"]) : 0
                                };

                                item.PropertyChanged += (s, ev) => CalcularTotais();
                                ItensDaOS.Add(item);
                                CalcularTotais();
                            }
                            else
                            {
                                MessageBox.Show("Produto ou Serviço não encontrado.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================================================
        // IMPLEMENTAÇÕES DEFINITIVAS DA TOOLBAR
        // =========================================================================================

        private void btnNovaOS_Click(object sender, RoutedEventArgs e)
        {
            if (ItensDaOS.Count > 0 && MessageBox.Show("Deseja descartar a OS atual e iniciar uma nova?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }
            ResetarTela();
        }

        private void btnSalvarOS_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbCliente.Text))
            {
                MessageBox.Show("Informe o nome do Cliente antes de salvar a OS.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbCliente.Focus();
                return;
            }

            try
            {
                long osId = long.Parse(txtNumOS.Text);
                bool isUpdate = osId > 0;

                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        string queryOS = isUpdate ?
                            @"UPDATE OrdemServico SET DataEntrada=@DataIn, DataSaida=@DataOut, Status=@Status, Problema=@Prob, Laudo=@Laudo, Imei=@Imei, Equipamento=@Equip, MarcaModelo=@Marca, Total=@Total WHERE Id=@Id;" :
                            @"INSERT INTO OrdemServico (DataEntrada, DataSaida, Status, Problema, Laudo, Imei, Equipamento, MarcaModelo, Total) VALUES (@DataIn, @DataOut, @Status, @Prob, @Laudo, @Imei, @Equip, @Marca, @Total); SELECT last_insert_rowid();";

                        using (var cmd = new SqliteCommand(queryOS, conn))
                        {
                            if (isUpdate) cmd.Parameters.AddWithValue("@Id", osId);
                            cmd.Parameters.AddWithValue("@DataIn", dpDataEntrada.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@DataOut", dpDataSaida.SelectedDate?.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@Status", cbStatus.Text);
                            cmd.Parameters.AddWithValue("@Prob", txtProblema.Text);
                            cmd.Parameters.AddWithValue("@Laudo", txtLaudo.Text);
                            cmd.Parameters.AddWithValue("@Imei", txtImei.Text);
                            cmd.Parameters.AddWithValue("@Equip", txtEquipamento.Text);
                            cmd.Parameters.AddWithValue("@Marca", txtMarcaModelo.Text);

                            decimal totalLiquido = decimal.Parse(txtTotalLiquido.Text.Replace("R$", "").Trim());
                            cmd.Parameters.AddWithValue("@Total", totalLiquido);

                            if (isUpdate) cmd.ExecuteNonQuery();
                            else osId = (long)cmd.ExecuteScalar();
                        }

                        // Se for update, deleta os itens antigos para re-inserir os novos (Evita lógica complexa de Diff)
                        if (isUpdate)
                        {
                            using (var cmdDel = new SqliteCommand("DELETE FROM OrdemServicoItens WHERE OS_ID = @OSID", conn))
                            {
                                cmdDel.Parameters.AddWithValue("@OSID", osId);
                                cmdDel.ExecuteNonQuery();
                            }
                        }

                        foreach (var item in ItensDaOS)
                        {
                            string queryItem = @"INSERT INTO OrdemServicoItens (OS_ID, CodigoProduto, Quantidade, ValorUnitario, Subtotal) VALUES (@OSID, @Cod, @Qtde, @ValUn, @Subt)";
                            using (var cmdItem = new SqliteCommand(queryItem, conn))
                            {
                                cmdItem.Parameters.AddWithValue("@OSID", osId);
                                cmdItem.Parameters.AddWithValue("@Cod", item.Codigo);
                                cmdItem.Parameters.AddWithValue("@Qtde", item.Quantidade);
                                cmdItem.Parameters.AddWithValue("@ValUn", item.ValorUnitario);
                                cmdItem.Parameters.AddWithValue("@Subt", item.Subtotal);
                                cmdItem.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        txtNumOS.Text = osId.ToString();
                        MessageBox.Show($"OS nº {osId} {(isUpdate ? "atualizada" : "salva")} com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro crítico ao salvar OS: {ex.Message}", "Falha de Banco de Dados", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLocOS_Click(object sender, RoutedEventArgs e)
        {
            // Cria um InputDialog dinâmico 100% nativo WPF
            Window prompt = new Window()
            {
                Width = 300,
                Height = 150,
                Title = "Localizar OS",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.WhiteSmoke
            };

            StackPanel stack = new StackPanel() { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock() { Text = "Digite o Nº da OS para buscar:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });

            TextBox txtInputId = new TextBox() { FontSize = 16, Padding = new Thickness(5) };
            stack.Children.Add(txtInputId);

            Button btnBuscar = new Button() { Content = "Buscar OS", Margin = new Thickness(0, 15, 0, 0), Height = 30, IsDefault = true };
            btnBuscar.Click += (s, ev) => {
                if (long.TryParse(txtInputId.Text, out long idOS)) { CarregarOS(idOS); prompt.Close(); }
                else { MessageBox.Show("Por favor, digite um número válido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning); }
            };
            stack.Children.Add(btnBuscar);
            prompt.Content = stack;
            prompt.ShowDialog();
        }

        private void CarregarOS(long osId)
        {
            try
            {
                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand("SELECT * FROM OrdemServico WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", osId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ResetarTela();
                                txtNumOS.Text = reader["Id"].ToString();
                                cbStatus.Text = reader["Status"].ToString();
                                txtProblema.Text = reader["Problema"].ToString();
                                txtLaudo.Text = reader["Laudo"].ToString();
                                txtImei.Text = reader["Imei"].ToString();
                                txtEquipamento.Text = reader["Equipamento"].ToString();
                                txtMarcaModelo.Text = reader["MarcaModelo"].ToString();

                                if (DateTime.TryParse(reader["DataEntrada"].ToString(), out DateTime dtIn)) dpDataEntrada.SelectedDate = dtIn;
                                if (DateTime.TryParse(reader["DataSaida"].ToString(), out DateTime dtOut)) dpDataSaida.SelectedDate = dtOut;
                            }
                            else
                            {
                                MessageBox.Show("Ordem de Serviço não encontrada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    using (var cmdItens = new SqliteCommand(@"SELECT i.*, p.Descricao as ProdDesc, p.Unidade FROM OrdemServicoItens i LEFT JOIN Produtos p ON i.CodigoProduto = p.Codigo WHERE i.OS_ID = @Id", conn))
                    {
                        cmdItens.Parameters.AddWithValue("@Id", osId);
                        using (var readerItens = cmdItens.ExecuteReader())
                        {
                            ItensDaOS.Clear();
                            while (readerItens.Read())
                            {
                                var item = new ItemOrdemServicoGrid
                                {
                                    Codigo = readerItens["CodigoProduto"].ToString() ?? "",
                                    Descricao = readerItens["ProdDesc"].ToString() ?? "Produto Avulso/Serviço",
                                    Unidade = readerItens["Unidade"].ToString() ?? "UN",
                                    Quantidade = Convert.ToDecimal(readerItens["Quantidade"]),
                                    ValorUnitario = Convert.ToDecimal(readerItens["ValorUnitario"])
                                };
                                item.PropertyChanged += (s, ev) => CalcularTotais();
                                ItensDaOS.Add(item);
                            }
                        }
                    }
                    CalcularTotais();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar OS: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnImprimirA4_Click(object sender, RoutedEventArgs e)
        {
            ImprimirDocumento(false);
        }

        private void btnImprimir58_Click(object sender, RoutedEventArgs e)
        {
            ImprimirDocumento(true);
        }

        private void ImprimirDocumento(bool is58mm)
        {
            if (txtNumOS.Text == "0")
            {
                MessageBox.Show("Salve a OS antes de imprimir.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PrintDialog printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument();
                doc.FontFamily = new FontFamily(is58mm ? "Consolas" : "Arial");
                doc.PagePadding = new Thickness(is58mm ? 5 : 40);
                if (is58mm) doc.PageWidth = 220;
                else doc.ColumnWidth = 800;

                // Cabeçalho
                doc.Blocks.Add(new Paragraph(new Run("ORDEM DE SERVIÇO Nº " + txtNumOS.Text)) { FontSize = is58mm ? 12 : 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                doc.Blocks.Add(new Paragraph(new Run("----------------------------------------")) { TextAlignment = TextAlignment.Center });

                // Dados Principais
                Paragraph pDados = new Paragraph();
                pDados.FontSize = is58mm ? 10 : 14;
                pDados.Inlines.Add(new Run($"Cliente: {cbCliente.Text}\n"));
                pDados.Inlines.Add(new Run($"Entrada: {dpDataEntrada.SelectedDate?.ToString("dd/MM/yyyy")}  Previsão: {dpDataSaida.SelectedDate?.ToString("dd/MM/yyyy")}\n"));
                pDados.Inlines.Add(new Run($"Status:  {cbStatus.Text}\n"));
                pDados.Inlines.Add(new Run($"Equip:   {txtEquipamento.Text} {txtMarcaModelo.Text}\n"));
                pDados.Inlines.Add(new Run($"Problema Relatado:\n{txtProblema.Text}\n"));
                doc.Blocks.Add(pDados);
                doc.Blocks.Add(new Paragraph(new Run("----------------------------------------")) { TextAlignment = TextAlignment.Center });

                // Itens
                Paragraph pItens = new Paragraph(new Run("ITENS E SERVIÇOS:\n")) { FontWeight = FontWeights.Bold, FontSize = is58mm ? 10 : 14 };
                foreach (var item in ItensDaOS)
                {
                    pItens.Inlines.Add(new Run($"{item.Quantidade}x {item.Descricao}\n  Un: {item.ValorUnitario:C} -> {item.Subtotal:C}\n") { FontWeight = FontWeights.Normal });
                }
                doc.Blocks.Add(pItens);
                doc.Blocks.Add(new Paragraph(new Run("----------------------------------------")) { TextAlignment = TextAlignment.Center });

                // Totais
                Paragraph pTotal = new Paragraph(new Run($"TOTAL: {txtTotalLiquido.Text}")) { FontSize = is58mm ? 12 : 18, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right };
                doc.Blocks.Add(pTotal);

                // Executa Impressão Nativa
                IDocumentPaginatorSource idpSource = doc;
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Impressão de OS");
            }
        }

        private void btnWhatsApp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbCliente.Text))
            {
                MessageBox.Show("Preencha o nome do cliente para enviar mensagem.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string status = cbStatus.Text;
            string total = txtTotalLiquido.Text;
            string numOs = txtNumOS.Text == "0" ? "[NÃO SALVA]" : txtNumOS.Text;
            string equip = string.IsNullOrWhiteSpace(txtEquipamento.Text) ? "aparelho" : txtEquipamento.Text;

            string mensagem = $"Olá {cbCliente.Text}, informamos que a *Ordem de Serviço Nº {numOs}* referente ao seu *{equip}* encontra-se com o status: *{status}*.\n\nValor Total: *{total}*.\n\nQualquer dúvida, estamos à disposição!";

            string urlWhatsApp = $"https://api.whatsapp.com/send?text={Uri.EscapeDataString(mensagem)}";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = urlWhatsApp,
                    UseShellExecute = true // Abre no navegador padrão ou no app do Windows nativamente
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao abrir o link do WhatsApp: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ItemOrdemServicoGrid : INotifyPropertyChanged
    {
        private decimal _quantidade;
        private decimal _valorUnitario;

        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Unidade { get; set; } = string.Empty;

        public decimal Quantidade
        {
            get => _quantidade;
            set
            {
                if (_quantidade != value)
                {
                    _quantidade = value;
                    OnPropertyChanged(nameof(Quantidade));
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        public decimal ValorUnitario
        {
            get => _valorUnitario;
            set
            {
                if (_valorUnitario != value)
                {
                    _valorUnitario = value;
                    OnPropertyChanged(nameof(ValorUnitario));
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        public decimal Subtotal => Quantidade * ValorUnitario;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}