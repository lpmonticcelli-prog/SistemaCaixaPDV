using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;

namespace SistemaCaixaPDV
{
    public partial class TelaClientes : Window
    {
        private string connectionString = BancoDeDados.ConnectionString;
        private int idClienteAtual = 0;
        private string caminhoFotoAtual = "";

        public TelaClientes()
        {
            InitializeComponent();
            ConfigurarBancoDeDados();
            txtDataCadastro.Text = DateTime.Now.ToString("dd/MM/yyyy");
            btnNovo_Click(null, null);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ConfigurarBancoDeDados()
        {
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sqlCriaVale = @"CREATE TABLE IF NOT EXISTS HistoricoValeCompras (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                                            ClienteId INTEGER, 
                                            DataHora TEXT, 
                                            Tipo TEXT, 
                                            Valor NUMERIC, 
                                            Motivo TEXT
                                          )";
                    using (var cmdVale = new SqliteCommand(sqlCriaVale, cx)) { cmdVale.ExecuteNonQuery(); }
                }
            }
            catch { }
        }

        // ==========================================
        // BOTÕES SUPERIORES E AÇÕES
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

            if (txtCreditoDisponivel != null) txtCreditoDisponivel.Text = "0,00";
            if (txtCreditoUtilizado != null) txtCreditoUtilizado.Text = "0,00";

            caminhoFotoAtual = "";
            if (imgFotoCliente != null) imgFotoCliente.Source = null;
            if (panelSemFoto != null) panelSemFoto.Visibility = Visibility.Visible;
            if (gridHistoricoVendas != null) gridHistoricoVendas.ItemsSource = null;
            if (gridContasReceber != null) gridContasReceber.ItemsSource = null;
            if (gridHistoricoVale != null) gridHistoricoVale.ItemsSource = null;
            if (txtSaldoVale != null) txtSaldoVale.Text = "R$ 0,00";
            if (txtValorVale != null) txtValorVale.Clear();
            if (txtMotivoVale != null) txtMotivoVale.Clear();

            if (txtNome != null) txtNome.Focus();
        }

        private void btnAlterar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Localize o cliente na lupa 🔍 primeiro para alterar!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnGravar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Preencha pelo menos o Nome!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNumero.Text)) txtNumero.Text = "SN";
            if (string.IsNullOrWhiteSpace(cbUf.Text)) cbUf.Text = "SP";

            decimal creditoParaLiberar = ConverterTextoParaDecimal(txtCreditoDisponivel.Text);

            ClienteCompleto cliente = new ClienteCompleto
            {
                Id = idClienteAtual,
                Nome = txtNome.Text.Trim(),
                CpfCnpj = txtCpfCnpj.Text.Trim(),
                Celular = txtCelular.Text.Trim(),
                TelefoneFixo = txtTelefoneFixo.Text.Trim(),
                Tipo = cbTipo.Text,
                Rg = txtRg.Text.Trim(),
                Endereco = txtEndereco.Text.Trim(),
                Numero = txtNumero.Text.Trim(),
                Complemento = txtComplemento.Text.Trim(),
                Bairro = txtBairro.Text.Trim(),
                Cidade = txtCidade.Text.Trim(),
                Uf = cbUf.Text,
                Cep = txtCep.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Observacoes = txtObservacoes.Text.Trim(),
                DataCadastro = txtDataCadastro.Text,
                CreditoLiberado = creditoParaLiberar
            };

            BancoDeDados.SalvarCliente(cliente);

            MessageBox.Show("Cliente gravado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            btnNovo_Click(null, null);
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
                BancoDeDados.ExcluirCliente(idClienteAtual);
                btnNovo_Click(null, null);
            }
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e) { this.Close(); }

        // ==========================================
        // FOTOGRAFIA DO CLIENTE
        // ==========================================
        private void btnFoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Imagens|*.bmp;*.jpg;*.png" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string pastaSegura = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagens", "Clientes");
                    if (!Directory.Exists(pastaSegura)) Directory.CreateDirectory(pastaSegura);

                    string extensao = Path.GetExtension(dlg.FileName);
                    string nomeArquivoSeguro = Guid.NewGuid().ToString() + extensao;
                    string caminhoDestinoFinal = Path.Combine(pastaSegura, nomeArquivoSeguro);

                    File.Copy(dlg.FileName, caminhoDestinoFinal, true);
                    caminhoFotoAtual = caminhoDestinoFinal;

                    imgFotoCliente.Source = new BitmapImage(new Uri(caminhoDestinoFinal));
                    panelSemFoto.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao arquivar a foto: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==========================================
        // MÁGICA DO VIA CEP
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

        // ==========================================
        // CÁLCULO DE CRÉDITO E SALDO
        // ==========================================
        private void AtualizarSaldo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (txtCreditoDisponivel == null || txtCreditoUtilizado == null || txtSaldoDisponivel == null) return;

            decimal creditoLiberado = ConverterTextoParaDecimal(txtCreditoDisponivel.Text);
            decimal creditoUtilizado = ConverterTextoParaDecimal(txtCreditoUtilizado.Text);

            decimal saldo = creditoLiberado - creditoUtilizado;

            txtSaldoDisponivel.Text = saldo.ToString("N2");
            if (saldo < 0) txtSaldoDisponivel.Foreground = System.Windows.Media.Brushes.Red;
            else txtSaldoDisponivel.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E3A8A"));
        }

        private decimal ConverterTextoParaDecimal(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            texto = texto.Replace("R$", "").Trim();
            if (decimal.TryParse(texto, out decimal valor)) return valor;
            return 0;
        }

        // ==========================================
        // CARREGAMENTO DE CLIENTE E ABAS HISTÓRICO
        // ==========================================
        public void CarregarClienteNaTela(int id)
        {
            var cliente = BancoDeDados.ObterClientePorId(id);
            if (cliente != null)
            {
                idClienteAtual = cliente.Id;
                txtCodigo.Text = cliente.Id.ToString();
                txtNome.Text = cliente.Nome;
                txtCpfCnpj.Text = cliente.CpfCnpj;
                txtCelular.Text = cliente.Celular;
                txtTelefoneFixo.Text = cliente.TelefoneFixo;
                cbTipo.Text = cliente.Tipo;
                txtRg.Text = cliente.Rg;
                txtEndereco.Text = cliente.Endereco;
                txtNumero.Text = cliente.Numero;
                txtComplemento.Text = cliente.Complemento;
                txtBairro.Text = cliente.Bairro;
                txtCidade.Text = cliente.Cidade;
                cbUf.Text = cliente.Uf;

                txtCep.TextChanged -= txtCep_TextChanged;
                txtCep.Text = cliente.Cep;
                txtCep.TextChanged += txtCep_TextChanged;

                txtEmail.Text = cliente.Email;
                txtObservacoes.Text = cliente.Observacoes;
                txtDataCadastro.Text = cliente.DataCadastro;

                txtCreditoDisponivel.TextChanged -= AtualizarSaldo_TextChanged;
                txtCreditoDisponivel.Text = cliente.CreditoLiberado.ToString("N2");
                txtCreditoDisponivel.TextChanged += AtualizarSaldo_TextChanged;

                CarregarHistoricoEContas(cliente.Id, cliente.Nome);
                CarregarHistoricoValeCompras(cliente.Id);
            }
        }

        private void CarregarHistoricoEContas(int idCliente, string nomeCliente)
        {
            try
            {
                decimal totalPendente = 0;
                gridContasReceber.ItemsSource = BancoDeDados.FiltrarContasReceber(idCliente, "Todos", out totalPendente);
                txtCreditoUtilizado.Text = totalPendente.ToString("N2");

                var listaVendas = new List<RelatorioVendaModel>();
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sql = "SELECT Id, DataHora, FormaPagamento, TotalLiquido FROM Vendas WHERE ClienteId = @id OR ClienteNome = @nome ORDER BY Id DESC";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@id", idCliente);
                        cmd.Parameters.AddWithValue("@nome", nomeCliente);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string dataFormatada = r["DataHora"].ToString();
                                if (DateTime.TryParse(dataFormatada, out DateTime dt)) dataFormatada = dt.ToString("dd/MM/yyyy HH:mm");

                                listaVendas.Add(new RelatorioVendaModel
                                {
                                    NumVenda = r["Id"].ToString().PadLeft(4, '0'),
                                    DataVenda = dataFormatada,
                                    FormaPagto = r["FormaPagamento"].ToString(),
                                    ValorTotal = Convert.ToDecimal(r["TotalLiquido"]).ToString("C")
                                });
                            }
                        }
                    }
                }
                gridHistoricoVendas.ItemsSource = listaVendas;
            }
            catch { }
        }

        // ==========================================
        // GESTÃO DA ABA VALE COMPRAS E TROCAS
        // ==========================================
        private void CarregarHistoricoValeCompras(int idCliente)
        {
            var lista = new List<ValeCompraModel>();
            decimal saldoFinal = 0;

            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sql = "SELECT * FROM HistoricoValeCompras WHERE ClienteId = @id ORDER BY Id DESC";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@id", idCliente);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string tipo = r["Tipo"].ToString();
                                decimal valor = Convert.ToDecimal(r["Valor"]);

                                if (tipo == "Entrada") saldoFinal += valor;
                                else saldoFinal -= valor;

                                string dataFormatada = r["DataHora"].ToString();
                                if (DateTime.TryParse(dataFormatada, out DateTime dt)) dataFormatada = dt.ToString("dd/MM/yyyy HH:mm");

                                lista.Add(new ValeCompraModel
                                {
                                    DataFormatada = dataFormatada,
                                    Tipo = tipo == "Entrada" ? "🟢 Crédito" : "🔴 Saída",
                                    ValorFormatado = valor.ToString("C"),
                                    Motivo = r["Motivo"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            gridHistoricoVale.ItemsSource = lista;
            txtSaldoVale.Text = saldoFinal.ToString("C");
            if (saldoFinal < 0) txtSaldoVale.Foreground = System.Windows.Media.Brushes.Red;
            else txtSaldoVale.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#15803D"));
        }

        private void btnGerarCredito_Click(object sender, RoutedEventArgs e) { RegistrarVale("Entrada"); }
        private void btnBaixarCredito_Click(object sender, RoutedEventArgs e) { RegistrarVale("Saída"); }

        private void RegistrarVale(string tipoMovimento)
        {
            if (idClienteAtual == 0)
            {
                MessageBox.Show("Você precisa localizar e selecionar um cliente primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal valor = ConverterTextoParaDecimal(txtValorVale.Text);
            if (valor <= 0)
            {
                MessageBox.Show("Digite um valor válido maior que zero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivoVale.Text))
            {
                MessageBox.Show("Por favor, preencha o Motivo da movimentação (ex: Devolução de blusa).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMotivoVale.Focus();
                return;
            }

            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sql = "INSERT INTO HistoricoValeCompras (ClienteId, DataHora, Tipo, Valor, Motivo) VALUES (@cid, @data, @tipo, @v, @m)";
                    using (var cmd = new SqliteCommand(sql, cx))
                    {
                        cmd.Parameters.AddWithValue("@cid", idClienteAtual);
                        cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@tipo", tipoMovimento);
                        cmd.Parameters.AddWithValue("@v", valor);
                        cmd.Parameters.AddWithValue("@m", txtMotivoVale.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Vale Compras ({tipoMovimento}) registrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                txtValorVale.Clear();
                txtMotivoVale.Clear();
                CarregarHistoricoValeCompras(idClienteAtual);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar Vale Compras: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Modelo de Dados Exclusivo para o Grid do Vale Compras
    public class ValeCompraModel
    {
        public string DataFormatada { get; set; }
        public string Tipo { get; set; }
        public string ValorFormatado { get; set; }
        public string Motivo { get; set; }
    }
}