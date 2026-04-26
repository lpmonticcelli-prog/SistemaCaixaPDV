using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV
{
    public partial class TelaContasReceber : Window
    {
        private string connectionString = BancoDeDados.ConnectionString;
        private int idClienteFiltro = 0;
        private ContasReceberModel contaEmFoco = null;
        private int idContaEditando = 0;

        public TelaContasReceber()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CriarTabelaContasReceber();
            CarregarClientesCombo();
            dpVencimento.SelectedDate = DateTime.Now;
            CarregarDadosGrid();
        }

        // ==========================================
        // BANCO DE DADOS 
        // ==========================================
        private void CriarTabelaContasReceber()
        {
            using (var cx = new SqliteConnection(connectionString))
            {
                cx.Open();
                string sql = @"CREATE TABLE IF NOT EXISTS ContasReceber (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ClienteId INTEGER,
                                ClienteNome TEXT,
                                Descricao TEXT,
                                TipoDocumento TEXT, 
                                Valor NUMERIC,
                                DataVencimento TEXT,
                                Status TEXT,
                                DataPagamento TEXT,
                                FormaPagamento TEXT)";
                using (var cmd = new SqliteCommand(sql, cx)) { cmd.ExecuteNonQuery(); }

                // Blindagem: Adiciona a coluna TipoDocumento caso a tabela seja velha e não a tenha
                try { using (var cmd = new SqliteCommand("ALTER TABLE ContasReceber ADD COLUMN TipoDocumento TEXT", cx)) cmd.ExecuteNonQuery(); } catch { }
            }
        }

        private void CarregarClientesCombo()
        {
            try
            {
                var listaClientes = new List<dynamic>();
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    using (var cmdCheck = new SqliteCommand("CREATE TABLE IF NOT EXISTS Clientes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT)", cx)) { cmdCheck.ExecuteNonQuery(); }
                    using (var cmd = new SqliteCommand("SELECT Id, Nome FROM Clientes ORDER BY Nome", cx))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read()) { listaClientes.Add(new { Id = r.GetInt32(0), Nome = r.GetString(1) }); }
                        }
                    }
                }
                cbClienteForm.ItemsSource = listaClientes;
            }
            catch { }
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

            try
            {
                string cliNome = cbClienteForm.Text.Trim();
                int cliId = 0;
                if (cbClienteForm.SelectedValue != null) int.TryParse(cbClienteForm.SelectedValue.ToString(), out cliId);

                string desc = txtDescricao.Text.Trim();
                string tipoDoc = ((ComboBoxItem)cbTipoDoc.SelectedItem).Content.ToString();
                string dataVenc = dpVencimento.SelectedDate.Value.ToString("yyyy-MM-dd");

                decimal valorDec = 0;
                decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(), out valorDec);

                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    if (idContaEditando == 0) // INSERIR NOVO
                    {
                        string sql = "INSERT INTO ContasReceber (ClienteId, ClienteNome, Descricao, TipoDocumento, Valor, DataVencimento, Status) VALUES (@cid, @cnm, @d, @tdoc, @v, @dv, 'Pendente')";
                        using (var cmd = new SqliteCommand(sql, cx))
                        {
                            cmd.Parameters.AddWithValue("@cid", cliId);
                            cmd.Parameters.AddWithValue("@cnm", cliNome);
                            cmd.Parameters.AddWithValue("@d", desc);
                            cmd.Parameters.AddWithValue("@tdoc", tipoDoc);
                            cmd.Parameters.AddWithValue("@v", valorDec);
                            cmd.Parameters.AddWithValue("@dv", dataVenc);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Recebimento lançado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else // ATUALIZAR
                    {
                        string sql = "UPDATE ContasReceber SET ClienteNome=@cnm, Descricao=@d, TipoDocumento=@tdoc, Valor=@v, DataVencimento=@dv WHERE Id=@id";
                        using (var cmd = new SqliteCommand(sql, cx))
                        {
                            cmd.Parameters.AddWithValue("@cnm", cliNome);
                            cmd.Parameters.AddWithValue("@d", desc);
                            cmd.Parameters.AddWithValue("@tdoc", tipoDoc);
                            cmd.Parameters.AddWithValue("@v", valorDec);
                            cmd.Parameters.AddWithValue("@dv", dataVenc);
                            cmd.Parameters.AddWithValue("@id", idContaEditando);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Recebimento atualizado!", "Atualizado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                ResetarFormulario();
                CarregarDadosGrid();
            }
            catch (Exception ex) { MessageBox.Show("Erro ao salvar: " + ex.Message); }
        }

        // ==========================================
        // GRID: CARREGAR, EDITAR E EXCLUIR
        // ==========================================
        private void CarregarDadosGrid()
        {
            if (!this.IsLoaded) return;

            var lista = new List<ContasReceberModel>();
            decimal totalPendente = 0;
            string statusFiltro = cbFiltroStatus.SelectedItem != null ? ((ComboBoxItem)cbFiltroStatus.SelectedItem).Content.ToString() : "Pendente";

            using (var cx = new SqliteConnection(connectionString))
            {
                cx.Open();
                string sql = "SELECT * FROM ContasReceber WHERE 1=1";

                if (idClienteFiltro > 0) sql += " AND ClienteId = @cid";
                if (statusFiltro != "Todos") sql += " AND Status = @status";

                sql += " ORDER BY DataVencimento ASC";

                using (var cmd = new SqliteCommand(sql, cx))
                {
                    if (idClienteFiltro > 0) cmd.Parameters.AddWithValue("@cid", idClienteFiltro);
                    if (statusFiltro != "Todos") cmd.Parameters.AddWithValue("@status", statusFiltro);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal valor = r["Valor"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Valor"]);
                            string status = r["Status"].ToString();
                            if (status == "Pendente") totalPendente += valor;

                            string dataBanco = r["DataVencimento"].ToString();
                            string dataTela = dataBanco;
                            if (DateTime.TryParse(dataBanco, out DateTime dt)) dataTela = dt.ToString("dd/MM/yyyy");

                            // Trata coluna antiga que não tinha TipoDocumento
                            string tDoc = "Não Informado";
                            try { tDoc = r["TipoDocumento"] == DBNull.Value ? "Carnê" : r["TipoDocumento"].ToString(); } catch { }

                            lista.Add(new ContasReceberModel
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                ClienteNome = r["ClienteNome"].ToString(),
                                Descricao = r["Descricao"].ToString(),
                                TipoDocumento = tDoc,
                                Valor = valor,
                                ValorFormatado = valor.ToString("C"),
                                DataVencimentoFormatada = dataTela,
                                Status = status
                            });
                        }
                    }
                }
            }
            gridContas.ItemsSource = lista;
            txtTotalPendente.Text = $"Total Pendente: {totalPendente:C}";
        }

        private void btnEditarLinha_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int idSelecionado = Convert.ToInt32(button.Tag);
                try
                {
                    using (var cx = new SqliteConnection(connectionString))
                    {
                        cx.Open();
                        using (var cmd = new SqliteCommand("SELECT * FROM ContasReceber WHERE Id = @id", cx))
                        {
                            cmd.Parameters.AddWithValue("@id", idSelecionado);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    idContaEditando = idSelecionado;
                                    cbClienteForm.Text = r["ClienteNome"].ToString();
                                    txtDescricao.Text = r["Descricao"].ToString();
                                    txtValor.Text = Convert.ToDecimal(r["Valor"]).ToString("N2");

                                    string dataBanco = r["DataVencimento"].ToString();
                                    if (DateTime.TryParse(dataBanco, out DateTime dt)) dpVencimento.SelectedDate = dt;

                                    string docBanco = "Carnê/Promissória";
                                    try { docBanco = r["TipoDocumento"] == DBNull.Value ? "Carnê/Promissória" : r["TipoDocumento"].ToString(); } catch { }
                                    foreach (ComboBoxItem item in cbTipoDoc.Items) { if (item.Content.ToString() == docBanco) { cbTipoDoc.SelectedItem = item; break; } }

                                    txtTituloFormulario.Text = "✏️ EDITANDO RECEBIMENTO SELECIONADO";
                                    txtTituloFormulario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                                    btnSalvar.Content = "💾 Atualizar";
                                    btnSalvar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                                    btnCancelar.Visibility = Visibility.Visible;
                                    txtDescricao.Focus();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Erro ao carregar conta: " + ex.Message); }
            }
        }

        private void btnExcluirLinha_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int idParaExcluir = Convert.ToInt32(button.Tag);
                if (MessageBox.Show("Deseja realmente apagar este registro do histórico?", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var cx = new SqliteConnection(connectionString))
                        {
                            cx.Open();
                            using (var cmd = new SqliteCommand("DELETE FROM ContasReceber WHERE Id = @id", cx))
                            {
                                cmd.Parameters.AddWithValue("@id", idParaExcluir);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        if (idContaEditando == idParaExcluir) ResetarFormulario();
                        CarregarDadosGrid();
                    }
                    catch (Exception ex) { MessageBox.Show("Erro ao excluir: " + ex.Message); }
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
                try
                {
                    string forma = ((ComboBoxItem)cbFormaBaixa.SelectedItem).Content.ToString();
                    string hoje = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    using (var cx = new SqliteConnection(connectionString))
                    {
                        cx.Open();
                        string sql = "UPDATE ContasReceber SET Status='Pago', DataPagamento=@data, FormaPagamento=@forma WHERE Id=@id";
                        using (var cmd = new SqliteCommand(sql, cx))
                        {
                            cmd.Parameters.AddWithValue("@data", hoje);
                            cmd.Parameters.AddWithValue("@forma", forma);
                            cmd.Parameters.AddWithValue("@id", contaEmFoco.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Pagamento registrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    CarregarDadosGrid();
                    panelBaixa.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex) { MessageBox.Show("Erro ao baixar fatura: " + ex.Message); }
            }
        }
    }
}
