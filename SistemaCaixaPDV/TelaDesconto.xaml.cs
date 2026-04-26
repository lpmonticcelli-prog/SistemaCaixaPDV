using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SistemaCaixaPDV // <-- ATENÇÃO: Verifique o nome do projeto!
{
    public partial class TelaDesconto : Window
    {
        private decimal TotalVendaOriginal;

        // Esta variável guarda o valor final em Reais para o PDV usar
        public decimal ValorDescontoReais { get; private set; } = 0;

        public TelaDesconto(decimal total)
        {
            InitializeComponent();
            TotalVendaOriginal = total;
            txtTotalOriginal.Text = TotalVendaOriginal.ToString("C");

            txtPorcentagem.Focus();
        }

        private void txtPorcentagem_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtPorcentagem.Text))
                {
                    ValorDescontoReais = 0;
                    txtValorDesconto.Text = "- R$ 0,00";
                    return;
                }

                decimal porcentagem = decimal.Parse(txtPorcentagem.Text.Replace("%", "").Trim());

                // Trava a porcentagem para não passar de 100%
                if (porcentagem > 100) porcentagem = 100;

                ValorDescontoReais = TotalVendaOriginal * (porcentagem / 100m);
                txtValorDesconto.Text = "- " + ValorDescontoReais.ToString("C");
            }
            catch
            {
                // Ignora enquanto o usuário digita
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
            else if (e.Key == Key.Enter)
            {
                btnConfirmar_Click(null, null);
            }
        }

        private void btnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (ValorDescontoReais > 0)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Digite uma porcentagem válida para o desconto!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPorcentagem.Focus();
            }
        }
    }
}
