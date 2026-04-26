using System.Windows;

namespace SistemaCaixaPDV // <-- Verifique o nome do projeto
{
    public partial class GeradorChaves : Window
    {
        public GeradorChaves()
        {
            InitializeComponent();
        }

        private void btnGerar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigoCliente.Text))
            {
                MessageBox.Show("Você precisa colar o código da máquina do cliente primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // O gerador chama a SUA regra de segurança para gerar a chave matemática exata
            string chave = Seguranca.GerarChaveMestra(txtCodigoCliente.Text.Trim());

            txtChaveGerada.Text = chave;
        }
    }
}
