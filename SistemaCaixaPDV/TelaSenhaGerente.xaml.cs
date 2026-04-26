using System.Windows;
using System.Windows.Input;

namespace SistemaCaixaPDV // <-- ATENÇÃO: Verifique o nome do seu projeto!
{
    public partial class TelaSenhaGerente : Window
    {
        // Esta variável vai dizer ao PDV se o gerente acertou a senha ou não
        public bool Autorizado { get; private set; } = false;
        private string senhaCorreta = "";

        public TelaSenhaGerente()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Vai buscar a senha atual do gerente ao banco de dados
            var config = BancoDeDados.ObterConfiguracoes();
            senhaCorreta = config.SenhaGerente ?? "";

            txtSenha.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                btnCancelar_Click(null, null);
            }
            else if (e.Key == Key.Enter)
            {
                btnAutorizar_Click(null, null);
            }
        }

        private void btnAutorizar_Click(object sender, RoutedEventArgs e)
        {
            // Se a senha estiver correta (ou se não houver senha configurada)
            if (txtSenha.Password == senhaCorreta || string.IsNullOrWhiteSpace(senhaCorreta))
            {
                Autorizado = true;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Senha incorreta! Operação não autorizada.", "Erro de Autorização", MessageBoxButton.OK, MessageBoxImage.Error);
                txtSenha.Clear();
                txtSenha.Focus();
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Autorizado = false;
            this.DialogResult = false;
            this.Close();
        }
    }
}
