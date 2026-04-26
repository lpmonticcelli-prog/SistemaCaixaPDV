using System.Windows;

namespace SistemaCaixaPDV
{
    public partial class TelaLicenca : Window
    {
        public TelaLicenca()
        {
            InitializeComponent();
            // Mostra o código único desta máquina para o cliente te enviar
            txtCodigoMaquina.Text = Seguranca.ObterCodigoMaquina();
        }

        private void btnAtivar_Click(object sender, RoutedEventArgs e)
        {
            if (Seguranca.ValidarLicenca(txtChave.Text))
            {
                BancoDeDados.SalvarLicenca(txtChave.Text.Trim().ToUpper());
                MessageBox.Show("Sistema ativado com sucesso! Obrigado.", "Licença Válida", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Libera o sistema
                this.Close();
            }
            else
            {
                MessageBox.Show("Chave de liberação inválida! Verifique o código digitado.", "Erro de Ativação", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
