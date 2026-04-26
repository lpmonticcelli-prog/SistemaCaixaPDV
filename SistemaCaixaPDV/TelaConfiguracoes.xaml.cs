using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.Data.Sqlite;
using System.Windows.Input; // Para detectar a tecla Enter
using System.Net.Http;      // Para conectar na internet (API)
using System.Threading.Tasks; // Para não travar a tela enquanto busca
using System.Text.RegularExpressions; // Para extrair os dados do ViaCEP
using System.Security.Cryptography.X509Certificates; // <--- NOVA BIBLIOTECA PARA TESTAR O CERTIFICADO!

namespace SistemaCaixaPDV
{
    public partial class TelaConfiguracoes : Window
    {
        private string connectionString = BancoDeDados.ConnectionString;

        private string caminhoFundoAtual = "";
        private string caminhoLogoAtual = "";
        private string caminhoBannerAtual = "";
        private string caminhoCertificadoAtual = "";

        public TelaConfiguracoes()
        {
            InitializeComponent();
            CarregarConfiguracoes();
        }

        private void CarregarConfiguracoes()
        {
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();

                    string sqlCria = "CREATE TABLE IF NOT EXISTS Configuracoes (Id INTEGER PRIMARY KEY AUTOINCREMENT, NomeLoja TEXT)";
                    using (var cmdCria = new SqliteCommand(sqlCria, cx)) { cmdCria.ExecuteNonQuery(); }

                    string[] colunasNecessarias = {
                        "Cnpj TEXT", "Ie TEXT", "Telefone TEXT", "Endereco TEXT",
                        "Rua TEXT", "Numero TEXT", "Bairro TEXT", "Cep TEXT", "Cidade TEXT", "Uf TEXT",
                        "CaminhoFundo TEXT", "CaminhoLogo TEXT", "CaminhoBanner TEXT",
                        "TipoImpressora TEXT", "SenhaGerente TEXT", "Cabecalho TEXT", "Rodape TEXT",
                        "AceitaPix INTEGER", "ChavePix TEXT", "AceitaCredito INTEGER", "AceitaDebito INTEGER",
                        "CaminhoCertificado TEXT", "SenhaCertificado TEXT",
                        "CodigoIbge TEXT", "IdCsc TEXT", "CscSefaz TEXT"
                    };

                    foreach (string coluna in colunasNecessarias)
                    {
                        try { using (var cmdAdd = new SqliteCommand($"ALTER TABLE Configuracoes ADD COLUMN {coluna}", cx)) cmdAdd.ExecuteNonQuery(); } catch { }
                    }

                    using (var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM Configuracoes", cx))
                    {
                        long count = (long)cmdCheck.ExecuteScalar();
                        if (count == 0)
                        {
                            using (var cmdInsert = new SqliteCommand("INSERT INTO Configuracoes (NomeLoja) VALUES ('Restaurante Kit Sabor')", cx)) cmdInsert.ExecuteNonQuery();
                        }
                    }

                    using (var cmdLer = new SqliteCommand("SELECT * FROM Configuracoes LIMIT 1", cx))
                    {
                        using (var r = cmdLer.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtNomeLoja.Text = LerBancoSeguro(r, "NomeLoja");
                                txtCnpj.Text = LerBancoSeguro(r, "Cnpj");
                                txtIe.Text = LerBancoSeguro(r, "Ie");
                                txtTelefone.Text = LerBancoSeguro(r, "Telefone");

                                txtRua.Text = LerBancoSeguro(r, "Rua");
                                txtNumero.Text = LerBancoSeguro(r, "Numero");
                                txtBairro.Text = LerBancoSeguro(r, "Bairro");
                                txtCep.Text = LerBancoSeguro(r, "Cep");
                                txtCidade.Text = LerBancoSeguro(r, "Cidade");

                                string ufSalva = LerBancoSeguro(r, "Uf");
                                if (!string.IsNullOrEmpty(ufSalva))
                                {
                                    foreach (System.Windows.Controls.ComboBoxItem item in cbUf.Items)
                                    {
                                        if (item.Content.ToString() == ufSalva) { cbUf.SelectedItem = item; break; }
                                    }
                                }

                                string ibge = LerBancoSeguro(r, "CodigoIbge");
                                txtCodigoIbge.Text = string.IsNullOrEmpty(ibge) ? "3522604" : ibge;

                                string idCsc = LerBancoSeguro(r, "IdCsc");
                                txtIdCsc.Text = string.IsNullOrEmpty(idCsc) ? "001" : idCsc;
                                txtCscSefaz.Text = LerBancoSeguro(r, "CscSefaz");

                                txtSenhaGerente.Password = LerBancoSeguro(r, "SenhaGerente");
                                txtCabecalho.Text = LerBancoSeguro(r, "Cabecalho");
                                txtRodape.Text = LerBancoSeguro(r, "Rodape");
                                txtChavePix.Text = LerBancoSeguro(r, "ChavePix");

                                caminhoCertificadoAtual = LerBancoSeguro(r, "CaminhoCertificado");
                                txtCaminhoCertificado.Text = caminhoCertificadoAtual;
                                txtSenhaCertificado.Password = LerBancoSeguro(r, "SenhaCertificado");

                                chkPix.IsChecked = (LerBancoSeguro(r, "AceitaPix") == "1");
                                chkCredito.IsChecked = (LerBancoSeguro(r, "AceitaCredito") == "1");
                                chkDebito.IsChecked = (LerBancoSeguro(r, "AceitaDebito") == "1");

                                string tipoImp = LerBancoSeguro(r, "TipoImpressora");
                                if (string.IsNullOrEmpty(tipoImp)) tipoImp = "58mm";
                                foreach (System.Windows.Controls.ComboBoxItem item in cbTipoImpressora.Items)
                                {
                                    if (item.Content.ToString() == tipoImp) { cbTipoImpressora.SelectedItem = item; break; }
                                }

                                caminhoFundoAtual = LerBancoSeguro(r, "CaminhoFundo");
                                txtCaminhoFundo.Text = caminhoFundoAtual;

                                caminhoLogoAtual = LerBancoSeguro(r, "CaminhoLogo");
                                ExibirImagem(caminhoLogoAtual, imgLogo);

                                caminhoBannerAtual = LerBancoSeguro(r, "CaminhoBanner");
                                ExibirImagem(caminhoBannerAtual, imgBannerPDV);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao carregar: " + ex.Message); }
        }

        private string LerBancoSeguro(SqliteDataReader r, string coluna) { try { return r[coluna] == DBNull.Value ? "" : r[coluna].ToString(); } catch { return ""; } }
        private void ExibirImagem(string caminho, System.Windows.Controls.Image controleImagem) { if (!string.IsNullOrEmpty(caminho) && File.Exists(caminho)) { try { controleImagem.Source = new BitmapImage(new Uri(caminho)); } catch { controleImagem.Source = null; } } else { controleImagem.Source = null; } }

        private void btnFundo_Click(object sender, RoutedEventArgs e) { OpenFileDialog dlg = new OpenFileDialog { Filter = "Imagens|*.jpg;*.jpeg;*.png" }; if (dlg.ShowDialog() == true) { caminhoFundoAtual = dlg.FileName; txtCaminhoFundo.Text = caminhoFundoAtual; } }
        private void btnRemoverFundo_Click(object sender, RoutedEventArgs e) { caminhoFundoAtual = ""; txtCaminhoFundo.Text = ""; }
        private void btnLogo_Click(object sender, RoutedEventArgs e) { OpenFileDialog dlg = new OpenFileDialog { Filter = "Imagens|*.png;*.jpg" }; if (dlg.ShowDialog() == true) { caminhoLogoAtual = dlg.FileName; ExibirImagem(caminhoLogoAtual, imgLogo); } }
        private void btnBanner_Click(object sender, RoutedEventArgs e) { OpenFileDialog dlg = new OpenFileDialog { Title = "Selecione o Banner", Filter = "Imagens|*.jpg;*.jpeg;*.png" }; if (dlg.ShowDialog() == true) { caminhoBannerAtual = dlg.FileName; ExibirImagem(caminhoBannerAtual, imgBannerPDV); } }
        private void btnRemoverBanner_Click(object sender, RoutedEventArgs e) { caminhoBannerAtual = ""; imgBannerPDV.Source = null; }
        private void btnCertificado_Click(object sender, RoutedEventArgs e) { OpenFileDialog dlg = new OpenFileDialog { Title = "Selecione o Certificado A1", Filter = "Certificado Digital (*.pfx;*.p12)|*.pfx;*.p12" }; if (dlg.ShowDialog() == true) { caminhoCertificadoAtual = dlg.FileName; txtCaminhoCertificado.Text = caminhoCertificadoAtual; } }

        // ==========================================
        // TESTE LOCAL DE CERTIFICADO (RESOLVIDO)
        // ==========================================
        private void btnTestarCertificado_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtCaminhoCertificado.Text)) { MessageBox.Show("Selecione o arquivo primeiro!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                // O ecrã de configurações testa o certificado por conta própria agora!
                var certificado = new X509Certificate2(txtCaminhoCertificado.Text, txtSenhaCertificado.Password);

                MessageBox.Show($"Certificado validado com sucesso!\n\nTitular: {certificado.FriendlyName}\nValidade: {certificado.NotAfter:dd/MM/yyyy}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao ler o certificado. Verifique se o arquivo está correto e se a senha é válida.\n\nDetalhes do Erro: {ex.Message}", "Erro de Certificado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var cx = new SqliteConnection(connectionString))
                {
                    cx.Open();
                    string sqlUpdate = @"UPDATE Configuracoes SET 
                                        NomeLoja = @nome, Cnpj = @cnpj, Ie = @ie, Telefone = @tel, 
                                        Rua = @rua, Numero = @num, Bairro = @bairro, Cep = @cep, Cidade = @cid, Uf = @uf,
                                        CaminhoFundo = @fundo, CaminhoLogo = @logo, CaminhoBanner = @banner,
                                        TipoImpressora = @imp, SenhaGerente = @senha, Cabecalho = @cab, Rodape = @rod,
                                        AceitaPix = @pix, ChavePix = @chavepix, AceitaCredito = @cred, AceitaDebito = @deb,
                                        CaminhoCertificado = @cert, SenhaCertificado = @senhacert,
                                        CodigoIbge = @ibge, IdCsc = @idcsc, CscSefaz = @csc";

                    using (var cmd = new SqliteCommand(sqlUpdate, cx))
                    {
                        cmd.Parameters.AddWithValue("@nome", txtNomeLoja.Text.Trim());
                        cmd.Parameters.AddWithValue("@cnpj", txtCnpj.Text.Trim());
                        cmd.Parameters.AddWithValue("@ie", txtIe.Text.Trim());
                        cmd.Parameters.AddWithValue("@tel", txtTelefone.Text.Trim());

                        cmd.Parameters.AddWithValue("@rua", txtRua.Text.Trim());
                        cmd.Parameters.AddWithValue("@num", txtNumero.Text.Trim());
                        cmd.Parameters.AddWithValue("@bairro", txtBairro.Text.Trim());
                        cmd.Parameters.AddWithValue("@cep", txtCep.Text.Trim());
                        cmd.Parameters.AddWithValue("@cid", txtCidade.Text.Trim());

                        string ufSelecionada = cbUf.SelectedItem != null ? ((System.Windows.Controls.ComboBoxItem)cbUf.SelectedItem).Content.ToString() : "SP";
                        cmd.Parameters.AddWithValue("@uf", ufSelecionada);

                        cmd.Parameters.AddWithValue("@fundo", caminhoFundoAtual);
                        cmd.Parameters.AddWithValue("@logo", caminhoLogoAtual);
                        cmd.Parameters.AddWithValue("@banner", caminhoBannerAtual);

                        string imp = cbTipoImpressora.SelectedItem != null ? ((System.Windows.Controls.ComboBoxItem)cbTipoImpressora.SelectedItem).Content.ToString() : "58mm";
                        cmd.Parameters.AddWithValue("@imp", imp);

                        cmd.Parameters.AddWithValue("@senha", txtSenhaGerente.Password);
                        cmd.Parameters.AddWithValue("@cab", txtCabecalho.Text);
                        cmd.Parameters.AddWithValue("@rod", txtRodape.Text);
                        cmd.Parameters.AddWithValue("@chavepix", txtChavePix.Text.Trim());
                        cmd.Parameters.AddWithValue("@pix", chkPix.IsChecked == true ? 1 : 0);
                        cmd.Parameters.AddWithValue("@cred", chkCredito.IsChecked == true ? 1 : 0);
                        cmd.Parameters.AddWithValue("@deb", chkDebito.IsChecked == true ? 1 : 0);
                        cmd.Parameters.AddWithValue("@cert", caminhoCertificadoAtual);
                        cmd.Parameters.AddWithValue("@senhacert", txtSenhaCertificado.Password);

                        cmd.Parameters.AddWithValue("@ibge", txtCodigoIbge.Text.Trim());
                        cmd.Parameters.AddWithValue("@idcsc", txtIdCsc.Text.Trim());
                        cmd.Parameters.AddWithValue("@csc", txtCscSefaz.Text.Trim());

                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Configurações salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Erro ao salvar: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // ==========================================
        // MÁGICA DO VIA CEP (BUSCA AUTOMÁTICA)
        // ==========================================
        private async void txtCep_LostFocus(object sender, RoutedEventArgs e)
        {
            await IniciarBuscaCep();
        }

        private async void txtCep_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await IniciarBuscaCep();
            }
        }

        private async Task IniciarBuscaCep()
        {
            string cepLimpo = txtCep.Text.Replace("-", "").Replace(".", "").Trim();
            if (cepLimpo.Length == 8)
            {
                await BuscarCepNaInternet(cepLimpo);
            }
        }

        private async Task BuscarCepNaInternet(string cep)
        {
            try
            {
                txtRua.Text = "Buscando...";
                txtCidade.Text = "";
                txtBairro.Text = "";

                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://viacep.com.br/ws/{cep}/json/";
                    string jsonDaInternet = await client.GetStringAsync(url);

                    if (jsonDaInternet.Contains("\"erro\": true") || jsonDaInternet.Contains("\"erro\":\"true\""))
                    {
                        MessageBox.Show("CEP não encontrado na base dos Correios!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtRua.Text = "";
                        return;
                    }

                    // Extraindo os dados do texto JSON usando Regex (Bem mais leve e não trava o C#)
                    txtRua.Text = ExtrairValorJson(jsonDaInternet, "logradouro");
                    txtBairro.Text = ExtrairValorJson(jsonDaInternet, "bairro");
                    txtCidade.Text = ExtrairValorJson(jsonDaInternet, "localidade");
                    txtCodigoIbge.Text = ExtrairValorJson(jsonDaInternet, "ibge");

                    string ufViaCep = ExtrairValorJson(jsonDaInternet, "uf");

                    // Procura o Estado na ComboBox e seleciona sozinho
                    foreach (System.Windows.Controls.ComboBoxItem item in cbUf.Items)
                    {
                        if (item.Content.ToString() == ufViaCep)
                        {
                            cbUf.SelectedItem = item;
                            break;
                        }
                    }

                    // Joga o cursor piscando direto pro campo do "Número"
                    txtNumero.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao buscar o CEP. Sem conexão com a internet?\n\nDetalhes: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                txtRua.Text = "";
            }
        }

        private string ExtrairValorJson(string textoJson, string chave)
        {
            // O robô que acha exatamente a palavra que queremos no meio da maçaroca do JSON
            string padrao = $"\"{chave}\":\\s*\"([^\"]+)\"";
            var match = Regex.Match(textoJson, padrao);
            return match.Success ? match.Groups[1].Value : "";
        }
    }
}
