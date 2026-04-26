using System;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace SistemaCaixaPDV
{
    public partial class TelaClientes : Window
    {
        private string connectionString = BancoDeDados.ConnectionString;
        private int idClienteAtual = 0;

        public TelaClientes()
        {
            InitializeComponent();

            // =========================================================
            // A MÁGICA: Agora o banco é forçado a criar logo no arranque!
            // =========================================================
            ConfigurarBancoDeDados();

            txtDataCadastro.Text = DateTime.Now.ToString("dd/MM/yyyy");
            btnNovo_Click(null, null);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Já não precisamos disto aqui porque o construtor acima já resolveu tudo!
        }

        // =====================================================================
        // MOTOR NOVO: CRIA A TABELA "Clientes" COMPLETA E IMPECÁVEL
        // =====================================================================
        private void ConfigurarBancoDeDados()
        {
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sqlCria = @"CREATE TABLE IF NOT EXISTS Clientes (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                                        Nome TEXT, 
                                        CpfCnpj TEXT,
                                        Numero TEXT,
                                        Endereco TEXT,
                                        Complemento TEXT,
                                        Bairro TEXT,
                                        Cidade TEXT,
                                        Uf TEXT,
                                        Cep TEXT,
                                        DataCadastro TEXT,
                                        Celular TEXT,
                                        TelefoneFixo TEXT,
                                        Tipo TEXT,
                                        Rg TEXT,
                                        Email TEXT,
                                        Observacoes TEXT
                                      )";
                    using (var cmd = new SqliteCommand(sqlCria, cx))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao criar a tabela nova: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // BOTÕES SUPERIORES
        // ==========================================
        private void btnNovo_Click(object sender, RoutedEventArgs e)
        {
            idClienteAtual = 0;
            if (txtCodigo != null) txtCodigo.Text = "NOVO";
            if (txtNome != null) txtNome.Clear();
            if (txtCpfCnpj != null) txtCpfCnpj.Clear();
            if (txtCelular != null) txtCelular.Clear();
            if (txtTelefoneFixo != null) txtTelefoneFixo.Clear();
            if (txtRg != null) txtRg.Clear();
            if (txtEmissor != null) txtEmissor.Clear();
            if (txtInscricao != null) txtInscricao.Clear();
            if (txtCep != null) txtCep.Clear();
            if (txtEndereco != null) txtEndereco.Clear();
            if (txtNumero != null) txtNumero.Clear();
            if (txtComplemento != null) txtComplemento.Clear();
            if (txtBairro != null) txtBairro.Clear();
            if (txtCidade != null) txtCidade.Clear();
            if (cbUf != null) cbUf.Text = "SP";
            if (txtNaturalidade != null) txtNaturalidade.Clear();
            if (txtDtNasc != null) txtDtNasc.Clear();
            if (txtEmail != null) txtEmail.Clear();
            if (txtPai != null) txtPai.Clear();
            if (txtMae != null) txtMae.Clear();
            if (txtObservacoes != null) txtObservacoes.Clear();
            if (txtDataCadastro != null) txtDataCadastro.Text = DateTime.Now.ToString("dd/MM/yyyy");
        }

        private void btnAlterar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Localize o cliente na lupa 🔍 primeiro para alterar!");
        }

        private void btnGravar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Preencha pelo menos o Nome!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Armadura para a NF-e
            if (string.IsNullOrWhiteSpace(txtNumero.Text)) txtNumero.Text = "SN";
            if (string.IsNullOrWhiteSpace(cbUf.Text)) cbUf.Text = "SP";

            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    SqliteCommand cmd;

                    if (idClienteAtual == 0)
                    {
                        // GRAVANDO NA TABELA NOVA!
                        string sql = @"INSERT INTO Clientes (Nome, CpfCnpj, Celular, TelefoneFixo, Tipo, Rg, Endereco, Numero, Complemento, Bairro, Cidade, Uf, Cep, Email, Observacoes, DataCadastro) 
                                       VALUES (@nome, @cpf, @celular, @fixo, @tipo, @rg, @endereco, @numero, @complemento, @bairro, @cidade, @uf, @cep, @email, @obs, @data)";
                        cmd = new SqliteCommand(sql, cx);
                    }
                    else
                    {
                        string sql = @"UPDATE Clientes SET Nome=@nome, CpfCnpj=@cpf, Celular=@celular, TelefoneFixo=@fixo, Tipo=@tipo, Rg=@rg, 
                                       Endereco=@endereco, Numero=@numero, Complemento=@complemento, Bairro=@bairro, Cidade=@cidade, Uf=@uf, 
                                       Cep=@cep, Email=@email, Observacoes=@obs WHERE Id=@id";
                        cmd = new SqliteCommand(sql, cx);
                        cmd.Parameters.AddWithValue("@id", idClienteAtual);
                    }

                    cmd.Parameters.AddWithValue("@nome", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@cpf", txtCpfCnpj.Text.Trim());
                    cmd.Parameters.AddWithValue("@celular", txtCelular.Text.Trim());
                    cmd.Parameters.AddWithValue("@fixo", txtTelefoneFixo.Text.Trim());
                    cmd.Parameters.AddWithValue("@tipo", cbTipo.Text);
                    cmd.Parameters.AddWithValue("@rg", txtRg.Text.Trim());
                    cmd.Parameters.AddWithValue("@endereco", txtEndereco.Text.Trim());
                    cmd.Parameters.AddWithValue("@numero", txtNumero.Text.Trim());
                    cmd.Parameters.AddWithValue("@complemento", txtComplemento.Text.Trim());
                    cmd.Parameters.AddWithValue("@bairro", txtBairro.Text.Trim());
                    cmd.Parameters.AddWithValue("@cidade", txtCidade.Text.Trim());
                    cmd.Parameters.AddWithValue("@uf", cbUf.Text);
                    cmd.Parameters.AddWithValue("@cep", txtCep.Text.Trim());
                    cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@obs", txtObservacoes.Text.Trim());
                    cmd.Parameters.AddWithValue("@data", txtDataCadastro.Text);

                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Cliente gravado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                btnNovo_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gravar: " + ex.Message, "Erro SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLocalizar_Click(object sender, RoutedEventArgs e)
        {
            TelaBuscaCliente telaBusca = new TelaBuscaCliente();
            telaBusca.ShowDialog();

            if (telaBusca.IdSelecionado > 0) CarregarClienteNaTela(telaBusca.IdSelecionado);
        }

        private void btnExcluir_Click(object sender, RoutedEventArgs e)
        {
            if (idClienteAtual == 0) { MessageBox.Show("Localize um cliente primeiro."); return; }
            if (MessageBox.Show("Excluir cliente?", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    using (var cmd = new SqliteCommand("DELETE FROM Clientes WHERE Id = @id", cx))
                    {
                        cmd.Parameters.AddWithValue("@id", idClienteAtual);
                        cmd.ExecuteNonQuery();
                    }
                }
                btnNovo_Click(null, null);
            }
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e) { this.Close(); }

        // ==========================================
        // EVENTOS EXTRAS
        // ==========================================
        private async void txtCep_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtCep == null) return;
            string cepLimpo = txtCep.Text.Replace("-", "").Replace(".", "").Trim();
            if (cepLimpo.Length == 8)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        string json = await client.GetStringAsync($"https://viacep.com.br/ws/{cepLimpo}/json/");
                        if (!json.Contains("\"erro\": true"))
                        {
                            txtEndereco.Text = ExtrairValorJson(json, "logradouro");
                            txtBairro.Text = ExtrairValorJson(json, "bairro");
                            txtCidade.Text = ExtrairValorJson(json, "localidade");
                            cbUf.Text = ExtrairValorJson(json, "uf");
                            txtNumero.Focus();
                        }
                    }
                }
                catch { }
            }
        }

        private string ExtrairValorJson(string json, string chave)
        {
            var match = Regex.Match(json, $"\"{chave}\":\\s*\"(.*?)\"");
            return match.Success ? match.Groups[1].Value : "";
        }

        private void cbBloqueio_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void AtualizarSaldo_TextChanged(object sender, TextChangedEventArgs e) { }

        public void CarregarClienteNaTela(int id)
        {
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    // BUSCANDO DA TABELA NOVA!
                    using (var cmd = new SqliteCommand("SELECT * FROM Clientes WHERE Id = @id", cx))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                idClienteAtual = Convert.ToInt32(r["Id"]);
                                txtCodigo.Text = idClienteAtual.ToString();
                                txtNome.Text = r["Nome"].ToString();
                                txtCpfCnpj.Text = r["CpfCnpj"].ToString();
                                txtCelular.Text = r["Celular"].ToString();
                                txtTelefoneFixo.Text = r["TelefoneFixo"].ToString();
                                cbTipo.Text = r["Tipo"].ToString();
                                txtRg.Text = r["Rg"].ToString();
                                txtEndereco.Text = r["Endereco"].ToString();
                                txtNumero.Text = r["Numero"].ToString();
                                txtComplemento.Text = r["Complemento"].ToString();
                                txtBairro.Text = r["Bairro"].ToString();
                                txtCidade.Text = r["Cidade"].ToString();
                                cbUf.Text = r["Uf"].ToString();

                                txtCep.TextChanged -= txtCep_TextChanged;
                                txtCep.Text = r["Cep"].ToString();
                                txtCep.TextChanged += txtCep_TextChanged;

                                txtEmail.Text = r["Email"].ToString();
                                txtObservacoes.Text = r["Observacoes"].ToString();
                                txtDataCadastro.Text = r["DataCadastro"].ToString();
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}

