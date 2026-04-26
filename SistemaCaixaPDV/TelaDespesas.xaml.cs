using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV // <-- Verifique o nome do seu projeto!
{
    public partial class TelaDespesas : Window
    {
        private string connectionString = BancoDeDados.ConnectionString;

        // Memória para saber se estamos editando ou criando uma nova conta
        private int idDespesaEditando = 0;

        public TelaDespesas()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CriarTabelaSeNaoExistir();

            dpFiltroInicio.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpFiltroFim.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

            dpDataConta.SelectedDate = DateTime.Now;

            CarregarDespesas();
        }

        private void CriarTabelaSeNaoExistir()
        {
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sql = "CREATE TABLE IF NOT EXISTS Despesas (Id INTEGER PRIMARY KEY AUTOINCREMENT, Descricao TEXT, Categoria TEXT, Valor NUMERIC, DataVencimento TEXT, Status TEXT)";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao preparar tabela de despesas: " + ex.Message);
            }
        }

        // ==========================================
        // SALVAR (NOVA) OU ATUALIZAR (EDIÇÃO)
        // ==========================================
        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Por favor, digite a descrição da despesa.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescricao.Focus();
                return;
            }

            if (cbCategoria.SelectedItem == null)
            {
                MessageBox.Show("Selecione uma categoria!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string desc = txtDescricao.Text.Trim();
                string categ = ((ComboBoxItem)cbCategoria.SelectedItem).Content.ToString();
                string status = ((ComboBoxItem)cbStatus.SelectedItem).Content.ToString();
                string dataVencimento = dpDataConta.SelectedDate.Value.ToString("yyyy-MM-dd");

                string textoValor = txtValor.Text.Replace("R$", "").Trim();
                decimal valorDec = 0;
                decimal.TryParse(textoValor, out valorDec);

                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();

                    if (idDespesaEditando == 0) // MODO: INSERIR NOVA
                    {
                        string sql = "INSERT INTO Despesas (Descricao, Categoria, Valor, DataVencimento, Status) VALUES (@d, @c, @v, @data, @s)";
                        using (var cmd = new SqliteCommand(sql, cx))
                        {
                            cmd.Parameters.AddWithValue("@d", desc);
                            cmd.Parameters.AddWithValue("@c", categ);
                            cmd.Parameters.AddWithValue("@v", valorDec);
                            cmd.Parameters.AddWithValue("@data", dataVencimento);
                            cmd.Parameters.AddWithValue("@s", status);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Despesa lançada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else // MODO: ATUALIZAR EXISTENTE
                    {
                        string sql = "UPDATE Despesas SET Descricao=@d, Categoria=@c, Valor=@v, DataVencimento=@data, Status=@s WHERE Id=@id";
                        using (var cmd = new SqliteCommand(sql, cx))
                        {
                            cmd.Parameters.AddWithValue("@d", desc);
                            cmd.Parameters.AddWithValue("@c", categ);
                            cmd.Parameters.AddWithValue("@v", valorDec);
                            cmd.Parameters.AddWithValue("@data", dataVencimento);
                            cmd.Parameters.AddWithValue("@s", status);
                            cmd.Parameters.AddWithValue("@id", idDespesaEditando);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Despesa atualizada com sucesso!", "Atualizado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                ResetarFormulario();
                CarregarDespesas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // EDITAR LINHA ESPECÍFICA (AÇÃO DA TABELA)
        // ==========================================
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
                        using (var cmd = new SqliteCommand("SELECT * FROM Despesas WHERE Id = @id", cx))
                        {
                            cmd.Parameters.AddWithValue("@id", idSelecionado);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    // Sobe os dados para o formulário
                                    idDespesaEditando = idSelecionado;
                                    txtDescricao.Text = r["Descricao"].ToString();
                                    txtValor.Text = Convert.ToDecimal(r["Valor"]).ToString("N2");

                                    string dataBanco = r["DataVencimento"].ToString();
                                    if (DateTime.TryParse(dataBanco, out DateTime dt)) dpDataConta.SelectedDate = dt;

                                    // Localiza a categoria correta no ComboBox
                                    string catBanco = r["Categoria"].ToString();
                                    foreach (ComboBoxItem item in cbCategoria.Items)
                                    {
                                        if (item.Content.ToString() == catBanco) { cbCategoria.SelectedItem = item; break; }
                                    }

                                    // Localiza o status correto no ComboBox
                                    string statusBanco = r["Status"].ToString();
                                    foreach (ComboBoxItem item in cbStatus.Items)
                                    {
                                        if (item.Content.ToString() == statusBanco) { cbStatus.SelectedItem = item; break; }
                                    }

                                    // Muda o visual da tela para "Modo Edição"
                                    txtTituloFormulario.Text = "✏️ EDITANDO DESPESA SELECIONADA";
                                    txtTituloFormulario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Amarelo

                                    btnSalvar.Content = "💾 Atualizar";
                                    btnSalvar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                                    btnCancelar.Visibility = Visibility.Visible;

                                    txtDescricao.Focus();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao carregar despesa: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==========================================
        // CANCELAR A EDIÇÃO E LIMPAR TELA
        // ==========================================
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ResetarFormulario();
        }

        private void ResetarFormulario()
        {
            idDespesaEditando = 0;
            txtDescricao.Clear();
            txtValor.Text = "0,00";
            dpDataConta.SelectedDate = DateTime.Now;
            cbStatus.SelectedIndex = 0; // Pendente

            // Volta o visual da tela ao normal
            txtTituloFormulario.Text = "📝 LANÇAR NOVA DESPESA";
            txtTituloFormulario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A")); // Azul

            btnSalvar.Content = "💾 Gravar";
            btnSalvar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Verde
            btnCancelar.Visibility = Visibility.Collapsed;

            txtDescricao.Focus();
        }

        // ==========================================
        // FILTRAR, CARREGAR E EXCLUIR
        // ==========================================
        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            CarregarDespesas();
        }

        private void CarregarDespesas()
        {
            if (dpFiltroInicio.SelectedDate == null || dpFiltroFim.SelectedDate == null || cbFiltroStatus.SelectedItem == null)
                return;

            string dataIni = dpFiltroInicio.SelectedDate.Value.ToString("yyyy-MM-dd");
            string dataFim = dpFiltroFim.SelectedDate.Value.ToString("yyyy-MM-dd");
            string statusFiltro = ((ComboBoxItem)cbFiltroStatus.SelectedItem).Content.ToString();

            var lista = new List<DespesaModel>();
            decimal totalFiltro = 0;

            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sql = "SELECT * FROM Despesas WHERE DataVencimento >= @ini AND DataVencimento <= @fim";

                    if (statusFiltro != "Todas") sql += " AND Status = @status";

                    sql += " ORDER BY DataVencimento ASC";

                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@ini", dataIni);
                        cmd.Parameters.AddWithValue("@fim", dataFim);
                        if (statusFiltro != "Todas") cmd.Parameters.AddWithValue("@status", statusFiltro);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                decimal valor = r["Valor"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Valor"]);
                                totalFiltro += valor;

                                string dataBanco = r["DataVencimento"] == DBNull.Value ? "" : r["DataVencimento"].ToString();
                                string dataExibicao = dataBanco;
                                if (DateTime.TryParse(dataBanco, out DateTime dt)) dataExibicao = dt.ToString("dd/MM/yyyy");

                                lista.Add(new DespesaModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    Descricao = r["Descricao"].ToString(),
                                    Categoria = r["Categoria"].ToString(),
                                    ValorReais = valor.ToString("C"),
                                    DataFormatada = dataExibicao,
                                    Status = r["Status"].ToString()
                                });
                            }
                        }
                    }
                }

                gridDespesas.ItemsSource = lista;
                txtTotalDespesas.Text = totalFiltro.ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Falha ao buscar as despesas: " + ex.Message, "Erro de Leitura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExcluirLinha_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                int idParaExcluir = Convert.ToInt32(button.Tag);

                if (MessageBox.Show("Deseja realmente apagar esta despesa do histórico?", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var cx = new SqliteConnection(connectionString))
                        {
                            cx.Open();
                            using (var cmd = new SqliteCommand("DELETE FROM Despesas WHERE Id = @id", cx))
                            {
                                cmd.Parameters.AddWithValue("@id", idParaExcluir);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // Se você apagou a conta que estava editando agora, limpa o formulário também
                        if (idDespesaEditando == idParaExcluir) ResetarFormulario();

                        CarregarDespesas();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao excluir: " + ex.Message);
                    }
                }
            }
        }
    }
}
