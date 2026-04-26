using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; // Permite o mapeamento dinâmico de colunas

namespace SistemaCaixaPDV
{
    public partial class TelaRelatorios : Window
    {
        public TelaRelatorios()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

            BuscarVendas();
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("A funcionalidade de exportação e impressão de grelhas em ficheiros (PDF/Excel) será acoplada na próxima fase.", "Exportação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ==========================================
        // LÓGICA DO FILTRO DE DATAS (VENDAS)
        // ==========================================
        private void cbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelCalendario == null) return;

            var selecionado = (ComboBoxItem)cbPeriodo.SelectedItem;
            string opcao = selecionado.Content.ToString();

            if (opcao.Contains("Personalizado"))
            {
                panelCalendario.Visibility = Visibility.Visible;
                dpInicio.SelectedDate = DateTime.Today.AddDays(-30);
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
        // MOTOR DE GERAÇÃO DINÂMICA DE COLUNAS
        // ==========================================
        private void ConfigurarColunasClientes()
        {
            gridResultados.Columns.Clear();
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Código", Binding = new Binding("Codigo"), Width = 70 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Nome do Cliente", Binding = new Binding("Nome"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "CPF/CNPJ", Binding = new Binding("CPF"), Width = 140 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Celular", Binding = new Binding("Celular"), Width = 130 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Cidade", Binding = new Binding("Cidade"), Width = 150 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "UF", Binding = new Binding("UF"), Width = 50 });
        }

        private void ConfigurarColunasProdutos()
        {
            gridResultados.Columns.Clear();
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Cód / Barras", Binding = new Binding("Codigo"), Width = 120 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Descrição do Produto", Binding = new Binding("Descricao"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Unidade", Binding = new Binding("Unidade"), Width = 70 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Preço Venda", Binding = new Binding("PrecoVenda"), Width = 130 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Estoque", Binding = new Binding("Estoque"), Width = 100 });
        }

        private void ConfigurarColunasVendas()
        {
            gridResultados.Columns.Clear();
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Nº Venda", Binding = new Binding("NumVenda"), Width = 100 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Data e Hora", Binding = new Binding("DataVenda"), Width = 160 });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Forma de Pagto", Binding = new Binding("FormaPagto"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            gridResultados.Columns.Add(new DataGridTextColumn { Header = "Valor Total", Binding = new Binding("ValorTotal"), Width = 140 });
        }

        // ==========================================
        // CARREGAMENTO DOS DADOS NO DATA GRID
        // ==========================================
        private void GerarRelatorioClientes()
        {
            txtTituloRelatorio.Text = "Lista Completa de Clientes";
            panelFiltrosVendas.Visibility = Visibility.Collapsed;
            panelTotais.Visibility = Visibility.Collapsed;

            ConfigurarColunasClientes();
            gridResultados.ItemsSource = BancoDeDados.ObterRelatorioClientes();
        }

        private void GerarRelatorioProdutos()
        {
            txtTituloRelatorio.Text = "Lista de Produtos e Preços";
            panelFiltrosVendas.Visibility = Visibility.Collapsed;
            panelTotais.Visibility = Visibility.Collapsed;

            ConfigurarColunasProdutos();
            gridResultados.ItemsSource = BancoDeDados.ObterRelatorioProdutos();
        }

        private void BuscarVendas()
        {
            string dataInicio = "";
            string dataFim = "";
            var selecionado = (ComboBoxItem)cbPeriodo.SelectedItem;
            string opcao = selecionado.Content.ToString();

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

            ConfigurarColunasVendas();
            decimal somaTotal;
            gridResultados.ItemsSource = BancoDeDados.ObterRelatorioVendas(dataInicio, dataFim, out somaTotal);
            txtTotalRelatorio.Text = somaTotal.ToString("C");
        }
    }
}