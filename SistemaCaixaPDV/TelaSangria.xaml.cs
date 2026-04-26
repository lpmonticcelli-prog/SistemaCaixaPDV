using System;
using System.Windows;
using System.Windows.Input;

namespace SistemaCaixaPDV // <-- ATENÇÃO: Verifique o nome do projeto!
{
    public partial class TelaSangria : Window
    {
        public TelaSangria()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtValor.Focus();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            // Altera visualmente para ajudar o operador a não errar
            if (rbRetirada.IsChecked == true)
            {
                lblValor.Text = "Valor a ser RETIRADO do caixa (R$):";
                btnConfirmar.Content = "CONFIRMAR SANGRIA (ENTER)";
                btnConfirmar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)); // Vermelho
            }
            else
            {
                lblValor.Text = "Valor a ser INSERIDO no caixa (R$):";
                btnConfirmar.Content = "CONFIRMAR SUPRIMENTO (ENTER)";
                btnConfirmar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74)); // Verde
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close();
            else if (e.Key == Key.Enter) btnConfirmar_Click(null, null);
        }

        private void btnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtValor.Text) || !decimal.TryParse(txtValor.Text, out decimal valor) || valor <= 0)
            {
                MessageBox.Show("Por favor, digite um valor válido em dinheiro.", "Valor Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtValor.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivo.Text))
            {
                MessageBox.Show("Por favor, informe o motivo dessa movimentação!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMotivo.Focus();
                return;
            }

            string tipo = rbRetirada.IsChecked == true ? "Sangria" : "Suprimento";
            string motivo = txtMotivo.Text.Trim();

            try
            {
                // Grava no banco de dados!
                BancoDeDados.InserirMovimentacaoCaixa(tipo, valor, motivo);

                MessageBox.Show($"{tipo} registrada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar a movimentação: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
