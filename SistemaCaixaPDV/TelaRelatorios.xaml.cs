using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

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

        // ==========================================
        // MOTOR DE EXPORTAÇÃO E IMPRESSÃO (NATIVO E REFLECTION)
        // ==========================================
        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (gridResultados.Items.Count == 0)
            {
                MessageBox.Show("Não há dados carregados para exportar.", "Bloqueio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult escolha = MessageBox.Show("Deseja exportar para o EXCEL (CSV)?\n\n[SIM] - Exportar para Excel\n[NÃO] - Imprimir em Papel/PDF\n[CANCELAR] - Abortar", "Motor de Exportação", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (escolha == MessageBoxResult.Yes)
            {
                ExportarParaCsv();
            }
            else if (escolha == MessageBoxResult.No)
            {
                ImprimirDocumento();
            }
        }

        private void ExportarParaCsv()
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "Relatorio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    DefaultExt = ".csv",
                    Filter = "Arquivo Excel CSV (.csv)|*.csv"
                };

                if (dlg.ShowDialog() == true)
                {
                    // Encoding.UTF8 com BOM garante que o Excel pt-BR leia acentos corretamente
                    using (StreamWriter sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                    {
                        // Extração dinâmica de Cabeçalhos
                        var headers = gridResultados.Columns.Select(c => c.Header.ToString());
                        sw.WriteLine(string.Join(";", headers));

                        // Extração via Reflection
                        foreach (var item in gridResultados.Items)
                        {
                            var values = new List<string>();
                            foreach (var column in gridResultados.Columns)
                            {
                                if (column is DataGridTextColumn textColumn && textColumn.Binding is Binding binding)
                                {
                                    PropertyInfo propInfo = item.GetType().GetProperty(binding.Path.Path);
                                    string value = propInfo?.GetValue(item, null)?.ToString() ?? "";
                                    values.Add($"\"{value}\""); // Aspas duplas previnem quebras por ponto-e-vírgula no texto
                                }
                            }
                            sw.WriteLine(string.Join(";", values));
                        }

                        // Anexa totais no fim do arquivo, se estiver filtrando vendas
                        if (panelTotais.Visibility == Visibility.Visible)
                        {
                            sw.WriteLine("");
                            sw.WriteLine($";;;TOTAL DO PERÍODO:;\"{txtTotalRelatorio.Text}\"");
                        }
                    }

                    MessageBox.Show("Relatório gerado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Abre o arquivo no programa padrão do Windows (geralmente o Excel)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gravar arquivo de exportação: {ex.Message}", "Falha", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImprimirDocumento()
        {
            try
            {
                PrintDialog printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    FlowDocument doc = new FlowDocument
                    {
                        PagePadding = new Thickness(30),
                        FontFamily = new FontFamily("Segoe UI")
                    };

                    // Cabeçalho do Documento
                    doc.Blocks.Add(new Paragraph(new Run(txtTituloRelatorio.Text)) { FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                    doc.Blocks.Add(new Paragraph(new Run($"Emissão: {DateTime.Now:dd/MM/yyyy HH:mm}")) { FontSize = 12, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 0, 0, 20), Foreground = Brushes.Gray });

                    // Criação da Tabela Nativa
                    Table table = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };

                    foreach (var col in gridResultados.Columns)
                        table.Columns.Add(new TableColumn());

                    TableRowGroup headerGroup = new TableRowGroup();
                    TableRow headerRow = new TableRow { Background = Brushes.LightGray, FontWeight = FontWeights.Bold };

                    foreach (var col in gridResultados.Columns)
                    {
                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(col.Header.ToString())) { Padding = new Thickness(5) }) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1) });
                    }
                    headerGroup.Rows.Add(headerRow);
                    table.RowGroups.Add(headerGroup);

                    // Preenchimento de Dados
                    TableRowGroup dataGroup = new TableRowGroup();
                    foreach (var item in gridResultados.Items)
                    {
                        TableRow row = new TableRow();
                        foreach (var column in gridResultados.Columns)
                        {
                            string cellValue = "";
                            if (column is DataGridTextColumn textColumn && textColumn.Binding is Binding binding)
                            {
                                PropertyInfo propInfo = item.GetType().GetProperty(binding.Path.Path);
                                cellValue = propInfo?.GetValue(item, null)?.ToString() ?? "";
                            }
                            row.Cells.Add(new TableCell(new Paragraph(new Run(cellValue)) { Padding = new Thickness(5), FontSize = 12 }) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1) });
                        }
                        dataGroup.Rows.Add(row);
                    }
                    table.RowGroups.Add(dataGroup);
                    doc.Blocks.Add(table);

                    // Adiciona o bloco de Total se estiver visível
                    if (panelTotais.Visibility == Visibility.Visible)
                    {
                        doc.Blocks.Add(new Paragraph(new Run($"TOTAL DO PERÍODO: {txtTotalRelatorio.Text}")) { FontSize = 16, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 20, 0, 0) });
                    }

                    // Envia para o Spooler
                    IDocumentPaginatorSource idpSource = doc;
                    printDlg.PrintDocument(idpSource.DocumentPaginator, "Impressão de Relatório - PDV");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha no Spooler de impressão: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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