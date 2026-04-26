using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV // <-- Verifique se o nome do projeto está correto!
{
    public partial class TelaRelatorios : Window
    {
        private string connectionString = BancoDeDados.ConnectionString;

        public TelaRelatorios()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Começa com a lista de clientes por padrão
            GerarRelatorioClientes();
        }

        // ==========================================
        // BOTÕES DO MENU LATERAL
        // ==========================================
        private void btnRelClientes_Click(object sender, RoutedEventArgs e)
        {
            GerarRelatorioClientes();
        }

        private void btnRelProdutos_Click(object sender, RoutedEventArgs e)
        {
            GerarRelatorioProdutos();
        }

        private void btnRelVendas_Click(object sender, RoutedEventArgs e)
        {
            txtTituloRelatorio.Text = "Relatório de Vendas";
            panelFiltrosVendas.Visibility = Visibility.Visible;
            panelTotais.Visibility = Visibility.Visible;

            // Já dispara a busca de "Hoje" que é o padrão
            BuscarVendas();
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Função de exportar/imprimir grid será adicionada em breve!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ==========================================
        // LÓGICA DO FILTRO DE DATAS (VENDAS)
        // ==========================================
        private void cbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelCalendario == null) return; // Evita erro no carregamento da tela

            var selecionado = (ComboBoxItem)cbPeriodo.SelectedItem;
            string opcao = selecionado.Content.ToString();

            // Só mostra os DatePickers (Calendários) se escolher "Personalizado"
            if (opcao.Contains("Personalizado"))
            {
                panelCalendario.Visibility = Visibility.Visible;
                dpInicio.SelectedDate = DateTime.Today.AddDays(-30); // Padrão 30 dias atrás
                dpFim.SelectedDate = DateTime.Today;
            }
            else
            {
                panelCalendario.Visibility = Visibility.Collapsed;
            }
        }

        private void btnFiltrarVendas_Click(object sender, RoutedEventArgs e)
        {
            BuscarVendas();
        }

        // ==========================================
        // FUNÇÕES QUE PUXAM OS DADOS DO BANCO
        // ==========================================
        private void GerarRelatorioClientes()
        {
            txtTituloRelatorio.Text = "Lista Completa de Clientes";
            panelFiltrosVendas.Visibility = Visibility.Collapsed;
            panelTotais.Visibility = Visibility.Collapsed;

            var lista = new List<RelatorioClienteModel>();

            using (var cx = new SqliteConnection(connectionString))
            {
                cx.Open();
                using (var cmd = new SqliteCommand("SELECT Id, Nome, Cpf, Telefone, Cidade, Uf FROM Clientes ORDER BY Nome", cx))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        lista.Add(new RelatorioClienteModel
                        {
                            Codigo = r.GetInt32(0).ToString("D3"),
                            Nome = r.GetString(1),
                            CPF = r.IsDBNull(2) ? "" : r.GetString(2),
                            Celular = r.IsDBNull(3) ? "" : r.GetString(3),
                            Cidade = r.IsDBNull(4) ? "" : r.GetString(4),
                            UF = r.IsDBNull(5) ? "" : r.GetString(5)
                        });
                    }
                }
            }
            gridResultados.ItemsSource = lista;
        }

        private void GerarRelatorioProdutos()
        {
            txtTituloRelatorio.Text = "Lista de Produtos e Preços";
            panelFiltrosVendas.Visibility = Visibility.Collapsed;
            panelTotais.Visibility = Visibility.Collapsed;

            var lista = new List<RelatorioProdutoModel>();

            using (var cx = new SqliteConnection(connectionString))
            {
                cx.Open();
                using (var cmd = new SqliteCommand("SELECT Codigo, Descricao, Unidade, Preco, EstoqueAtual FROM Produtos ORDER BY Descricao", cx))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        lista.Add(new RelatorioProdutoModel
                        {
                            Codigo = r.GetString(0),
                            Descricao = r.GetString(1),
                            Unidade = r.IsDBNull(2) ? "UN" : r.GetString(2),
                            PrecoVenda = r.GetDecimal(3).ToString("C"),
                            Estoque = r.IsDBNull(4) ? "0" : r.GetDecimal(4).ToString("N2")
                        });
                    }
                }
            }
            gridResultados.ItemsSource = lista;
        }

        private void BuscarVendas()
        {
            string dataInicio = "";
            string dataFim = "";
            var selecionado = (ComboBoxItem)cbPeriodo.SelectedItem;
            string opcao = selecionado.Content.ToString();

            // Mágica das datas! (O banco salva em yyyy-MM-dd HH:mm:ss)
            DateTime hoje = DateTime.Today;

            if (opcao == "Hoje")
            {
                dataInicio = hoje.ToString("yyyy-MM-dd 00:00:00");
                dataFim = hoje.ToString("yyyy-MM-dd 23:59:59");
            }
            else if (opcao == "Ontem")
            {
                dataInicio = hoje.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                dataFim = hoje.AddDays(-1).ToString("yyyy-MM-dd 23:59:59");
            }
            else if (opcao == "Últimos 7 Dias")
            {
                dataInicio = hoje.AddDays(-7).ToString("yyyy-MM-dd 00:00:00");
                dataFim = hoje.ToString("yyyy-MM-dd 23:59:59");
            }
            else if (opcao == "Último Mês")
            {
                dataInicio = hoje.AddMonths(-1).ToString("yyyy-MM-dd 00:00:00");
                dataFim = hoje.ToString("yyyy-MM-dd 23:59:59");
            }
            else if (opcao.Contains("Personalizado"))
            {
                if (dpInicio.SelectedDate == null || dpFim.SelectedDate == null)
                {
                    MessageBox.Show("Por favor, selecione as datas de início e fim no calendário!", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                dataInicio = dpInicio.SelectedDate.Value.ToString("yyyy-MM-dd 00:00:00");
                dataFim = dpFim.SelectedDate.Value.ToString("yyyy-MM-dd 23:59:59");
            }

            // Agora busca no banco
            var lista = new List<RelatorioVendaModel>();
            decimal somaTotal = 0;

            using (var cx = new SqliteConnection(connectionString))
            {
                cx.Open();
                string sql = "SELECT Id, DataHora, FormaPagamento, TotalLiquido FROM Vendas WHERE DataHora >= @inicio AND DataHora <= @fim ORDER BY DataHora DESC";

                using (var cmd = new SqliteCommand(sql, cx))
                {
                    cmd.Parameters.AddWithValue("@inicio", dataInicio);
                    cmd.Parameters.AddWithValue("@fim", dataFim);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal valor = r.GetDecimal(3);
                            somaTotal += valor;

                            // Tenta converter a data para exibir mais bonitinho na tela
                            string dataBonita = r.GetString(1);
                            if (DateTime.TryParse(dataBonita, out DateTime dtParsed)) dataBonita = dtParsed.ToString("dd/MM/yyyy HH:mm");

                            lista.Add(new RelatorioVendaModel
                            {
                                NumVenda = r.GetInt32(0).ToString("D4"),
                                DataVenda = dataBonita,
                                FormaPagto = r.GetString(2),
                                ValorTotal = valor.ToString("C")
                            });
                        }
                    }
                }
            }

            gridResultados.ItemsSource = lista;
            txtTotalRelatorio.Text = somaTotal.ToString("C");
        }
    }
}
