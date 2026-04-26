using System;
using System.Windows;
using System.Windows.Input;

namespace SistemaCaixaPDV // <-- Verifique o nome do projeto
{
    public partial class TelaOS : Window
    {
        public TelaOS()
        {
            InitializeComponent();
            PrepararNovaOS();

            // Tenta carregar os clientes registados na lista
            CarregarClientes();
        }

        private void PrepararNovaOS()
        {
            // Limpa os campos e coloca a data de hoje
            txtNumOS.Text = "";
            txtDataEntrada.Text = DateTime.Now.ToString("dd/MM/yyyy");
            cbCliente.Text = "";
            txtProduto.Text = "";
            txtProblema.Text = "";
            txtLaudo.Text = "";
            txtObservacoes.Text = "";
            txtObsInternas.Text = "";
            txtTotalLiquido.Text = "0,00";

            cbStatus.SelectedIndex = 0; // "Aberto"
            cbCliente.Focus();
        }

        private void CarregarClientes()
        {
            try
            {
                var clientes = BancoDeDados.ListarClientes();
                foreach (var cli in clientes)
                {
                    cbCliente.Items.Add(cli.Nome);
                }
            }
            catch { } // Ignora erros se a lista estiver vazia
        }

        // Reconhece as teclas de atalho
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F4)
            {
                btnSalvar_Click(null, null);
            }
            else if (e.Key == Key.F2)
            {
                PrepararNovaOS();
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void btnNovaOS_Click(object sender, RoutedEventArgs e)
        {
            PrepararNovaOS();
        }

        // Função principal: Guardar a OS na Base de Dados
        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(cbCliente.Text))
            {
                MessageBox.Show("Informe o Cliente antes de guardar a OS.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbCliente.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtProblema.Text))
            {
                MessageBox.Show("Informe o Problema relatado.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtProblema.Focus();
                return;
            }

            // Tenta converter o valor (caso o utilizador digite algo inválido)
            decimal total = 0;
            decimal.TryParse(txtTotalLiquido.Text.Replace("R$", "").Trim(), out total);

            try
            {
                // Guarda e recebe o número gerado da OS
                long numOS = BancoDeDados.InserirOS(
                    cbCliente.Text,
                    cbResponsavel.Text,
                    txtDataEntrada.Text,
                    cbStatus.Text,
                    txtProduto.Text,
                    txtProblema.Text,
                    txtLaudo.Text,
                    txtObservacoes.Text,
                    total
                );

                // Mostra o número no ecrã
                txtNumOS.Text = numOS.ToString();

                MessageBox.Show($"Ordem de Serviço Nº {numOS} guardada com sucesso!", "OS Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao guardar a OS: " + ex.Message, "Erro SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Sair_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
