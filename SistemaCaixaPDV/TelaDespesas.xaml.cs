using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaCaixaPDV
{
    public partial class TelaDespesas : Window
    {
        private int idDespesaEditando = 0;

        public TelaDespesas()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dpFiltroInicio.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpFiltroFim.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            dpDataConta.SelectedDate = DateTime.Now;

            CarregarDespesas();
        }

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

            string desc = txtDescricao.Text.Trim();
            string categ = ((ComboBoxItem)cbCategoria.SelectedItem).Content.ToString();
            string status = ((ComboBoxItem)cbStatus.SelectedItem).Content.ToString();
            string dataVencimento = dpDataConta.SelectedDate.Value.ToString("yyyy-MM-dd");

            decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(), out decimal valorDec);

            // Chama a camada do banco de dados centralizado
            BancoDeDados.SalvarDespesa(idDespesaEditando, desc, categ, valorDec, dataVencimento, status);

            MessageBox.Show(idDespesaEditando == 0 ? "Despesa lançada com sucesso!" : "Despesa atualizada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

            ResetarFormulario();
            CarregarDespesas();
        }

        private void btnEditarLinha_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int idSelecionado = Convert.ToInt32(button.Tag);
                var despesa = BancoDeDados.ObterDespesaPorId(idSelecionado);

                if (despesa != null)
                {
                    idDespesaEditando = despesa.Id;
                    txtDescricao.Text = despesa.Descricao;
                    txtValor.Text = despesa.ValorReais;

                    if (DateTime.TryParse(despesa.DataFormatada, out DateTime dt))
                        dpDataConta.SelectedDate = dt;

                    foreach (ComboBoxItem item in cbCategoria.Items)
                    {
                        if (item.Content.ToString() == despesa.Categoria) { cbCategoria.SelectedItem = item; break; }
                    }

                    foreach (ComboBoxItem item in cbStatus.Items)
                    {
                        if (item.Content.ToString() == despesa.Status) { cbStatus.SelectedItem = item; break; }
                    }

                    txtTituloFormulario.Text = "✏️ EDITANDO DESPESA SELECIONADA";
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

                if (MessageBox.Show("Deseja realmente apagar esta despesa do histórico?", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    BancoDeDados.ExcluirDespesa(idParaExcluir);

                    if (idDespesaEditando == idParaExcluir) ResetarFormulario();
                    CarregarDespesas();
                }
            }
        }

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
            cbStatus.SelectedIndex = 0;

            txtTituloFormulario.Text = "📝 LANÇAR NOVA DESPESA";
            txtTituloFormulario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A"));
            btnSalvar.Content = "💾 Gravar";
            btnSalvar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            btnCancelar.Visibility = Visibility.Collapsed;
            txtDescricao.Focus();
        }

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

            decimal totalFiltro;
            gridDespesas.ItemsSource = BancoDeDados.FiltrarDespesas(dataIni, dataFim, statusFiltro, out totalFiltro);
            txtTotalDespesas.Text = totalFiltro.ToString("C");
        }
    }
}